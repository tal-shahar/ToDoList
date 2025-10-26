using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibreries.Contracts;
using SharedLibreries.Constants;
using System.Text;
using System.Text.Json;

namespace SharedLibreries.RabbitMQ
{
    public interface IMessageHandler<TRequest, TResponse>
        where TRequest : IRequest
        where TResponse : IResponse, new()
    {
        Task<TResponse> HandleAsync(TRequest request);
    }

    public interface IRabbitMqRpcServer
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }

    public class RabbitMqRpcServer : BackgroundService, IRabbitMqRpcServer
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqRpcServer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, Type> _messageTypes = new();
        private bool _disposed = false;

        public RabbitMqRpcServer(IConfiguration configuration, ILogger<RabbitMqRpcServer> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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

            SetupQueues();
            RegisterMessageTypes();

            _logger.LogInformation("RabbitMQ RPC Server initialized");
        }

        private void SetupQueues()
        {
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

            _channel.QueueDeclare(QueueNames.UserQueue, true, false, false, queueArgs);
            _channel.QueueDeclare(QueueNames.ItemQueue, true, false, false, queueArgs);

            // Setup QoS for fair message distribution
            _channel.BasicQos(0, 1, false);
        }

        private void RegisterMessageTypes()
        {
            // User operations
            _messageTypes[OperationTypes.CreateUser] = typeof(Contracts.CreateUserRequest);
            _messageTypes[OperationTypes.GetUser] = typeof(Contracts.GetUserRequest);
            _messageTypes[OperationTypes.GetAllUsers] = typeof(Contracts.GetAllUsersRequest);
            _messageTypes[OperationTypes.UpdateUser] = typeof(Contracts.UpdateUserRequest);
            _messageTypes[OperationTypes.DeleteUser] = typeof(Contracts.DeleteUserRequest);

            // Item operations
            _messageTypes[OperationTypes.CreateItem] = typeof(Contracts.CreateItemRequest);
            _messageTypes[OperationTypes.GetItem] = typeof(Contracts.GetItemRequest);
            _messageTypes[OperationTypes.GetAllItems] = typeof(Contracts.GetAllItemsRequest);
            _messageTypes[OperationTypes.GetUserItems] = typeof(Contracts.GetUserItemsRequest);
            _messageTypes[OperationTypes.UpdateItem] = typeof(Contracts.UpdateItemRequest);
            _messageTypes[OperationTypes.DeleteItem] = typeof(Contracts.DeleteItemRequest);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Start consuming from User queue
            var userConsumer = new EventingBasicConsumer(_channel);
            userConsumer.Received += async (model, ea) => await ProcessMessageAsync(ea, QueueNames.UserQueue);
            _channel.BasicConsume(QueueNames.UserQueue, false, userConsumer);

            // Start consuming from Item queue
            var itemConsumer = new EventingBasicConsumer(_channel);
            itemConsumer.Received += async (model, ea) => await ProcessMessageAsync(ea, QueueNames.ItemQueue);
            _channel.BasicConsume(QueueNames.ItemQueue, false, itemConsumer);

            _logger.LogInformation("RabbitMQ RPC Server started consuming messages");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, string queueName)
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
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                var request = JsonSerializer.Deserialize(messageJson, messageType, _jsonOptions) as IRequest;
                if (request == null)
                {
                    _logger.LogError("Failed to deserialize message for operation {OperationType}", operationType);
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
                
                // Send error response
                var errorResponse = new Contracts.CreateUserResponse // Use any response type as base
                {
                    CorrelationId = correlationId,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };

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

        private async Task<IResponse> ProcessRequestAsync(IRequest request, string operationType)
        {
            using var scope = _serviceProvider.CreateScope();
            
            var operationHandlers = new Dictionary<string, Func<Task<IResponse>>>
            {
                [OperationTypes.CreateUser] = async () => await ProcessUserRequestAsync<Contracts.CreateUserRequest, Contracts.CreateUserResponse>(request, scope),
                [OperationTypes.GetUser] = async () => await ProcessUserRequestAsync<Contracts.GetUserRequest, Contracts.GetUserResponse>(request, scope),
                [OperationTypes.GetAllUsers] = async () => await ProcessUserRequestAsync<Contracts.GetAllUsersRequest, Contracts.GetAllUsersResponse>(request, scope),
                [OperationTypes.UpdateUser] = async () => await ProcessUserRequestAsync<Contracts.UpdateUserRequest, Contracts.UpdateUserResponse>(request, scope),
                [OperationTypes.DeleteUser] = async () => await ProcessUserRequestAsync<Contracts.DeleteUserRequest, Contracts.DeleteUserResponse>(request, scope),
                
                [OperationTypes.CreateItem] = async () => await ProcessItemRequestAsync<Contracts.CreateItemRequest, Contracts.CreateItemResponse>(request, scope),
                [OperationTypes.GetItem] = async () => await ProcessItemRequestAsync<Contracts.GetItemRequest, Contracts.GetItemResponse>(request, scope),
                [OperationTypes.GetAllItems] = async () => await ProcessItemRequestAsync<Contracts.GetAllItemsRequest, Contracts.GetAllItemsResponse>(request, scope),
                [OperationTypes.GetUserItems] = async () => await ProcessItemRequestAsync<Contracts.GetUserItemsRequest, Contracts.GetUserItemsResponse>(request, scope),
                [OperationTypes.UpdateItem] = async () => await ProcessItemRequestAsync<Contracts.UpdateItemRequest, Contracts.UpdateItemResponse>(request, scope),
                [OperationTypes.DeleteItem] = async () => await ProcessItemRequestAsync<Contracts.DeleteItemRequest, Contracts.DeleteItemResponse>(request, scope)
            };

            if (operationHandlers.TryGetValue(operationType, out var handler))
            {
                return await handler();
            }

            throw new NotSupportedException($"Operation type {operationType} is not supported");
        }

        private async Task<TResponse> ProcessUserRequestAsync<TRequest, TResponse>(IRequest request, IServiceScope scope)
            where TRequest : IRequest
            where TResponse : IResponse, new()
        {
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TRequest, TResponse>>();
            return await handler.HandleAsync((TRequest)request);
        }

        private async Task<TResponse> ProcessItemRequestAsync<TRequest, TResponse>(IRequest request, IServiceScope scope)
            where TRequest : IRequest
            where TResponse : IResponse, new()
        {
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TRequest, TResponse>>();
            return await handler.HandleAsync((TRequest)request);
        }

        public override void Dispose()
        {
            if (_disposed) return;

            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();

            _disposed = true;
            _logger.LogInformation("RabbitMQ RPC Server disposed");
        }
    }

    public static class RabbitMqRpcServerExtensions
    {
        public static IServiceCollection AddRabbitMqRpcServer(this IServiceCollection services)
        {
            services.AddSingleton<IRabbitMqRpcServer, RabbitMqRpcServer>();
            services.AddHostedService<RabbitMqRpcServer>();
            return services;
        }
    }
}
