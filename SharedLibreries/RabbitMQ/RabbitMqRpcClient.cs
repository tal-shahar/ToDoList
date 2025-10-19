using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibreries.Contracts;
using SharedLibreries.Constants;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SharedLibreries.RabbitMQ
{
    public interface IRabbitMqService
    {
        Task<TResponse> SendRpcRequestAsync<TRequest, TResponse>(TRequest request, string queueName, string operationType)
            where TRequest : IRequest
            where TResponse : IResponse, new();
    }

    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<IResponse>> _pendingRequests = new();
        private readonly Timer _cleanupTimer;
        private bool _disposed = false;

        public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? RabbitMQConfig.HostName,
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? RabbitMQConfig.Port.ToString()),
                VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? RabbitMQConfig.VirtualHost,
                UserName = configuration["RabbitMQ:Username"] ?? RabbitMQConfig.Username,
                Password = configuration["RabbitMQ:Password"] ?? RabbitMQConfig.Password,
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(30)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Setup Dead Letter Exchange
            SetupDeadLetterExchange();

            // Setup cleanup timer for orphaned requests
            _cleanupTimer = new Timer(CleanupOrphanedRequests, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _logger.LogInformation("RabbitMQ RPC Client initialized");
        }

        private void SetupDeadLetterExchange()
        {
            _channel.ExchangeDeclare(QueueNames.DeadLetterExchange, ExchangeType.Direct, true);
            _channel.QueueDeclare(QueueNames.DeadLetterQueue, true, false, false);
            _channel.QueueBind(QueueNames.DeadLetterQueue, QueueNames.DeadLetterExchange, "");
        }

        public async Task<TResponse> SendRpcRequestAsync<TRequest, TResponse>(TRequest request, string queueName, string operationType)
            where TRequest : IRequest
            where TResponse : IResponse, new()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMqService));

            var correlationId = request.CorrelationId;
            var replyQueueName = $"reply.{correlationId}";

            try
            {
                // Declare temporary reply queue with same arguments as server
                var queueArgs = new Dictionary<string, object>
                {
                    { "x-message-ttl", 300000 } // 5 minutes TTL
                };
                var replyQueue = _channel.QueueDeclare(replyQueueName, false, true, true, queueArgs);

                // Setup consumer for reply
                var tcs = new TaskCompletionSource<IResponse>();
                _pendingRequests[correlationId] = tcs;

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var responseJson = Encoding.UTF8.GetString(body);
                            var response = JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
                            
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

                var consumerTag = _channel.BasicConsume(replyQueueName, true, consumer);

                // Declare main queue with DLX and TTL (same as server)
                _channel.QueueDeclare(queueName, true, false, false, new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", QueueNames.DeadLetterExchange },
                    { "x-message-ttl", 300000 } // 5 minutes TTL
                });

                // Publish request
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
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                // Cleanup
                _pendingRequests.TryRemove(correlationId, out _);
                try
                {
                    _channel.QueueDelete(replyQueueName);
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

            _cleanupTimer?.Dispose();
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();

            _disposed = true;
            _logger.LogInformation("RabbitMQ RPC Client disposed");
        }
    }

    public static class RabbitMqServiceExtensions
    {
        public static IServiceCollection AddRabbitMqService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IRabbitMqService, RabbitMqService>();
            return services;
        }
    }
}
