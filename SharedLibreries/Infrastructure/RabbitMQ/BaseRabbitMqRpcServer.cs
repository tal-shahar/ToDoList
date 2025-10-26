using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibreries.Contracts;
using SharedLibreries.Constants;
using SharedLibreries.RabbitMQ;
using System.Text;
using System.Text.Json;

namespace SharedLibreries.Infrastructure.RabbitMQ
{
    public abstract class BaseRabbitMqRpcServer : BackgroundService, IRabbitMqRpcServer, IDisposable
    {
        protected readonly IRabbitMqConnectionManager _connectionManager;
        protected readonly ILogger _logger;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly JsonSerializerOptions _jsonOptions;
        protected readonly Dictionary<string, Type> _messageTypes = new();
        
        private IModel? _channel;
        private bool _disposed = false;

        protected BaseRabbitMqRpcServer(
            IRabbitMqConnectionManager connectionManager,
            ILogger logger,
            IServiceProvider serviceProvider)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            RegisterMessageTypes();
            SetupConnectionEvents();
        }

        protected abstract void RegisterMessageTypes();
        protected abstract string[] GetQueueNames();
        protected abstract Task<IResponse> ProcessRequestAsync(IRequest request, string operationType);

        private void SetupConnectionEvents()
        {
            if (_connectionManager != null)
            {
                _connectionManager.ConnectionLost += OnConnectionLost;
                _connectionManager.ConnectionRestored += OnConnectionRestored;
            }
        }

        private async void OnConnectionLost(object? sender, ConnectionEventArgs e)
        {
            _logger.LogWarning("RabbitMQ connection lost, attempting to reconnect");
            await ReconnectAsync();
        }

        private async void OnConnectionRestored(object? sender, ConnectionEventArgs e)
        {
            _logger.LogInformation("RabbitMQ connection restored");
            await ReconnectAsync();
        }

        private async Task ReconnectAsync()
        {
            try
            {
                _channel?.Close();
                _channel?.Dispose();
                _channel = await _connectionManager.GetChannelAsync();
                SetupQueues();
                StartConsuming();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect to RabbitMQ");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _channel = await _connectionManager.GetChannelAsync();
                SetupQueues();
                StartConsuming();

                _logger.LogInformation("RabbitMQ RPC Server started consuming messages");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RabbitMQ RPC Server execution");
                throw;
            }
        }

        private void SetupQueues()
        {
            if (_channel == null)
            {
                _logger.LogError("Cannot setup queues - channel is null");
                return;
            }

            // Setup Dead Letter Exchange
            _channel.ExchangeDeclare(QueueNames.DeadLetterExchange, ExchangeType.Direct, true);
            _channel.QueueDeclare(QueueNames.DeadLetterQueue, true, false, false);
            _channel.QueueBind(QueueNames.DeadLetterQueue, QueueNames.DeadLetterExchange, "");

            // Setup main queues with DLX
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", QueueNames.DeadLetterExchange },
                { "x-message-ttl", 300000 } // 5 minutes TTL
            };

            foreach (var queueName in GetQueueNames())
            {
                _channel.QueueDeclare(queueName, true, false, false, queueArgs);
            }

            // Setup QoS for fair message distribution
            _channel.BasicQos(0, 1, false);
        }

        private void StartConsuming()
        {
            if (_channel == null)
            {
                _logger.LogError("Cannot start consuming - channel is null");
                return;
            }

            foreach (var queueName in GetQueueNames())
            {
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) => await ProcessMessageAsync(ea, queueName);
                _channel.BasicConsume(queueName, false, consumer);
            }
        }

        protected async Task ProcessMessageAsync(BasicDeliverEventArgs ea, string queueName)
        {
            var correlationId = ea.BasicProperties.CorrelationId;
            var operationType = ea.BasicProperties.Type;

            try
            {
                _logger.LogDebug("Processing message {OperationType} with correlation ID {CorrelationId}", operationType, correlationId);

                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);

                if (!_messageTypes.TryGetValue(operationType, out var messageType))
                {
                    _logger.LogWarning("Unknown operation type: {OperationType}", operationType);
                    if (_channel != null)
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                var request = JsonSerializer.Deserialize(messageJson, messageType, _jsonOptions) as IRequest;
                if (request == null || _channel == null)
                {
                    _logger.LogError("Failed to deserialize message for operation {OperationType}", operationType);
                    if (_channel != null)
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                // Process the message using appropriate handler
                var response = await ProcessRequestAsync(request, operationType);

                // Send response back
                var responseJson = JsonSerializer.Serialize(response, response.GetType(), _jsonOptions);
                var responseBody = Encoding.UTF8.GetBytes(responseJson);

                var properties = _channel.CreateBasicProperties();
                properties.CorrelationId = correlationId;
                properties.Type = operationType;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish("", ea.BasicProperties.ReplyTo, properties, responseBody);
                _channel.BasicAck(ea.DeliveryTag, false);

                _logger.LogDebug("Processed message {OperationType} with correlation ID {CorrelationId}", operationType, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {OperationType} with correlation ID {CorrelationId}", operationType, correlationId);
                
                if (_channel == null)
                {
                    _logger.LogError("Cannot send error response - channel is null");
                    return;
                }

                // Send error response
                var errorResponse = CreateErrorResponse(correlationId, ex.Message);
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                var errorBody = Encoding.UTF8.GetBytes(errorJson);

                var properties = _channel.CreateBasicProperties();
                properties.CorrelationId = correlationId;
                properties.Type = operationType;

                try
                {
                    _channel.BasicPublish("", ea.BasicProperties.ReplyTo, properties, errorBody);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception publishEx)
                {
                    _logger.LogError(publishEx, "Failed to send error response for correlation ID {CorrelationId}", correlationId);
                    _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue for retry
                }
            }
        }

        protected abstract IResponse CreateErrorResponse(string correlationId, string errorMessage);

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping RabbitMQ RPC Server");
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_connectionManager != null)
                {
                    _connectionManager.ConnectionLost -= OnConnectionLost;
                    _connectionManager.ConnectionRestored -= OnConnectionRestored;
                }
                
                _channel?.Close();
                _channel?.Dispose();
                
                _disposed = true;
                _logger.LogInformation("RabbitMQ RPC Server disposed");
            }
        }
    }
}
