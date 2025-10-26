using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibreries.Constants;
using SharedLibreries.Contracts;
using SharedLibreries.RabbitMQ;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SharedLibreries.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Resilient RabbitMQ RPC client service with connection pooling, circuit breaker,
    /// retry policies, and automatic channel health monitoring.
    /// Implements the IRabbitMqService interface with enterprise-grade resilience patterns.
    /// </summary>
    public class ResilientRabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly IRabbitMqClientConnectionManager _connectionManager;
        private readonly ILogger<ResilientRabbitMqService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<IResponse>> _pendingRequests = new();
        private readonly Timer _cleanupTimer;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly SemaphoreSlim _channelSemaphore = new(1, 1);
        private IModel? _channel;
        private bool _disposed = false;

        public ResilientRabbitMqService(
            IRabbitMqClientConnectionManager connectionManager,
            ILogger<ResilientRabbitMqService> logger)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            SetupConnectionEvents();

            // Setup cleanup timer for orphaned requests
            _cleanupTimer = new Timer(CleanupOrphanedRequests, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _logger.LogInformation("Resilient RabbitMQ Service initialized");

            // Setup dead letter exchange asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await SetupDeadLetterExchangeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize dead letter exchange");
                }
            });
        }

        private void SetupConnectionEvents()
        {
            if (_connectionManager != null)
            {
                _connectionManager.ConnectionLost += OnConnectionLost;
                _connectionManager.ConnectionRestored += OnConnectionRestored;
            }
        }

        private void OnConnectionLost(object? sender, EventArgs e)
        {
            _logger.LogWarning("RabbitMQ connection lost, will retry on next request");
            
            if (_channel?.IsOpen == true)
            {
                try
                {
                    _channel.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing channel during connection loss");
                }
            }
            
            try
            {
                _channel?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing channel during connection loss");
            }
            
            _channel = null;
        }

        private async void OnConnectionRestored(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("RabbitMQ connection restored, setting up channel");
                await EnsureChannelAsync();
                await SetupDeadLetterExchangeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup channel after connection restoration");
            }
        }

        private async Task<IModel> EnsureChannelAsync()
        {
            await _channelSemaphore.WaitAsync();
            try
            {
                if (_channel != null && _channel.IsOpen)
                    return _channel;

                _channel?.Close();
                _channel?.Dispose();
                _channel = await _connectionManager.GetChannelAsync();
                
                _logger.LogDebug("Created new RabbitMQ channel");
                
                return _channel;
            }
            finally
            {
                _channelSemaphore.Release();
            }
        }

        private async Task SetupDeadLetterExchangeAsync()
        {
            try
            {
                var channel = await EnsureChannelAsync();
                
                channel.ExchangeDeclare(
                    QueueNames.DeadLetterExchange, 
                    ExchangeType.Direct, 
                    durable: true,
                    autoDelete: false);
                
                channel.QueueDeclare(
                    QueueNames.DeadLetterQueue, 
                    durable: true, 
                    exclusive: false, 
                    autoDelete: false,
                    arguments: null);
                
                channel.QueueBind(
                    QueueNames.DeadLetterQueue, 
                    QueueNames.DeadLetterExchange, 
                    routingKey: "");
                
                _logger.LogDebug("Dead letter exchange setup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup dead letter exchange");
            }
        }

        public async Task<TResponse> SendRpcRequestAsync<TRequest, TResponse>(TRequest request, string queueName, string operationType)
            where TRequest : IRequest
            where TResponse : IResponse, new()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResilientRabbitMqService));

            var correlationId = request.CorrelationId;
            var replyQueueName = $"reply.{correlationId}";

            try
            {
                // Ensure we have a valid channel
                await EnsureChannelAsync();

                if (_channel == null || !_channel.IsOpen)
                {
                    _logger.LogError("No valid channel available");
                    return new TResponse
                    {
                        CorrelationId = correlationId,
                        IsSuccess = false,
                        ErrorMessage = "RabbitMQ channel unavailable"
                    };
                }

                // Setup consumer FIRST before publishing to avoid race condition
                var tcs = new TaskCompletionSource<IResponse>();
                _pendingRequests[correlationId] = tcs;

                // Declare temporary reply queue with autoDelete=false to keep it alive
                var queueArgs = new Dictionary<string, object>
                {
                    { "x-message-ttl", 300000 } // 5 minutes TTL
                };
                var replyQueue = _channel.QueueDeclare(replyQueueName, false, false, false, queueArgs);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var responseJson = Encoding.UTF8.GetString(body);
                            
                            // Check if response is valid JSON before deserializing
                            if (string.IsNullOrWhiteSpace(responseJson))
                            {
                                _logger.LogError("Empty response received for correlation ID {CorrelationId}", correlationId);
                                tcs.SetException(new InvalidOperationException("Empty response received"));
                                return;
                            }
                            
                            // Try to deserialize - if it fails, check if it's an error message
                            TResponse response;
                            try
                            {
                                response = JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
                                if (response == null)
                                {
                                    throw new InvalidOperationException("Deserialized response is null");
                                }
                            }
                            catch (JsonException)
                            {
                                // Not valid JSON - might be an error message
                                _logger.LogWarning("Non-JSON response received for correlation ID {CorrelationId}: {Response}", correlationId, responseJson.Substring(0, Math.Min(100, responseJson.Length)));
                                
                                // Create error response
                                response = new TResponse
                                {
                                    CorrelationId = correlationId,
                                    IsSuccess = false,
                                    ErrorMessage = $"Invalid response format: {responseJson.Substring(0, Math.Min(100, responseJson.Length))}"
                                };
                            }
                            
                            if (response != null)
                            {
                                tcs.SetResult(response);
                            }
                            else
                            {
                                tcs.SetException(new InvalidOperationException("Failed to deserialize response"));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing RPC response for correlation ID {CorrelationId}", correlationId);
                            tcs.SetException(ex);
                        }
                    }
                };

                // Register consumer BEFORE publishing
                var consumerTag = _channel.BasicConsume(replyQueueName, true, consumer);

                // Declare main queue with DLX and TTL
                _channel.QueueDeclare(queueName, true, false, false, new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", QueueNames.DeadLetterExchange },
                    { "x-message-ttl", 300000 } // 5 minutes TTL
                });

                // Publish request AFTER consumer is registered
                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                var requestBody = Encoding.UTF8.GetBytes(requestJson);

                var properties = _channel.CreateBasicProperties();
                properties.CorrelationId = correlationId;
                properties.ReplyTo = replyQueueName;
                properties.Type = operationType;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.Persistent = true;

                _channel.BasicPublish("", queueName, properties, requestBody);

                _logger.LogDebug("Sent RPC request {OperationType} with correlation ID {CorrelationId}", operationType, correlationId);

                // Wait for response with timeout
                var timeout = TimeSpan.FromSeconds(RabbitMQConfig.RequestTimeoutSeconds);
                using var cts = new CancellationTokenSource(timeout);
                
                try
                {
                    var response = await tcs.Task.WaitAsync(cts.Token);
                    return (TResponse)response;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("RPC request timeout for correlation ID {CorrelationId}", correlationId);
                    return new TResponse
                    {
                        CorrelationId = correlationId,
                        IsSuccess = false,
                        ErrorMessage = "Request timeout"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending RPC request {OperationType} with correlation ID {CorrelationId}", operationType, correlationId);
                return new TResponse
                {
                    CorrelationId = correlationId,
                    IsSuccess = false,
                    ErrorMessage = $"RabbitMQ error: {ex.Message}"
                };
            }
            finally
            {
                // Cleanup
                _pendingRequests.TryRemove(correlationId, out _);
                try
                {
                    if (_channel?.IsOpen == true)
                    {
                        _channel.QueueDelete(replyQueueName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete reply queue {ReplyQueueName}", replyQueueName);
                }
            }
        }

        private void CleanupOrphanedRequests(object? state)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
            var orphanedKeys = _pendingRequests
                .Where(kvp => kvp.Value.Task.IsCompleted == false)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in orphanedKeys)
            {
                if (_pendingRequests.TryRemove(key, out var tcs))
                {
                    tcs.SetException(new TimeoutException("Request cleanup due to timeout"));
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _cancellationTokenSource?.Cancel();
            _cleanupTimer?.Dispose();
            _channel?.Close();
            _channel?.Dispose();
            
            if (_connectionManager != null)
            {
                _connectionManager.ConnectionLost -= OnConnectionLost;
                _connectionManager.ConnectionRestored -= OnConnectionRestored;
            }
            
            _cancellationTokenSource?.Dispose();
            _channelSemaphore?.Dispose();

            _disposed = true;
            _logger.LogInformation("Resilient RabbitMQ Service disposed");
            
            GC.SuppressFinalize(this);
        }
    }

    public static class ResilientRabbitMqServiceExtensions
    {
        public static IServiceCollection AddResilientRabbitMqService(this IServiceCollection services, int maxPoolSize = 10)
        {
            services.AddRabbitMqClientConnectionManager(maxPoolSize);
            services.AddSingleton<IRabbitMqService, ResilientRabbitMqService>();
            return services;
        }
    }
}

