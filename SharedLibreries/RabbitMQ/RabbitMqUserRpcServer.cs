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
    public class RabbitMqUserRpcServer : BackgroundService, IRabbitMqRpcServer, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqUserRpcServer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, Type> _messageTypes = new();
        private bool _disposed = false;

        public RabbitMqUserRpcServer(IConfiguration configuration, ILogger<RabbitMqUserRpcServer> logger, IServiceProvider serviceProvider)
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

            _logger.LogInformation("RabbitMQ User RPC Server initialized");
        }

        private void SetupQueues()
        {
            // Setup Dead Letter Exchange
            _channel.ExchangeDeclare(QueueNames.DeadLetterExchange, ExchangeType.Direct, true);
            _channel.QueueDeclare(QueueNames.DeadLetterQueue, true, false, false);
            _channel.QueueBind(QueueNames.DeadLetterQueue, QueueNames.DeadLetterExchange, "");

            // Setup User queue with DLX
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", QueueNames.DeadLetterExchange },
                { "x-message-ttl", 300000 } // 5 minutes TTL
            };

            _channel.QueueDeclare(QueueNames.UserQueue, true, false, false, queueArgs);

            // Setup QoS for fair message distribution
            _channel.BasicQos(0, 1, false);
        }

        private void RegisterMessageTypes()
        {
            // User operations only
            _messageTypes[OperationTypes.CreateUser] = typeof(Contracts.CreateUserRequest);
            _messageTypes[OperationTypes.GetUser] = typeof(Contracts.GetUserRequest);
            _messageTypes[OperationTypes.GetAllUsers] = typeof(Contracts.GetAllUsersRequest);
            _messageTypes[OperationTypes.UpdateUser] = typeof(Contracts.UpdateUserRequest);
            _messageTypes[OperationTypes.DeleteUser] = typeof(Contracts.DeleteUserRequest);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Start consuming from User queue only
            var userConsumer = new EventingBasicConsumer(_channel);
            userConsumer.Received += async (model, ea) => await ProcessMessageAsync(ea, QueueNames.UserQueue);
            _channel.BasicConsume(QueueNames.UserQueue, false, userConsumer);

            _logger.LogInformation("RabbitMQ User RPC Server started consuming messages from User queue");

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
                _logger.LogDebug("Processing user message {OperationType} with correlation ID {CorrelationId}", operationType, correlationId);

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

                var response = await HandleMessageAsync(operationType, request);
                var responseJson = JsonSerializer.Serialize(response, response.GetType(), _jsonOptions);
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                var replyProperties = _channel.CreateBasicProperties();
                replyProperties.CorrelationId = correlationId;
                replyProperties.Type = operationType;

                _channel.BasicPublish("", ea.BasicProperties.ReplyTo, replyProperties, responseBytes);
                _channel.BasicAck(ea.DeliveryTag, false);

                _logger.LogDebug("Successfully processed message {OperationType} with correlation ID {CorrelationId}", operationType, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {OperationType} with correlation ID {CorrelationId}", operationType, correlationId);

                try
                {
                    var errorResponse = new { IsSuccess = false, ErrorMessage = ex.Message };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    var errorBytes = Encoding.UTF8.GetBytes(errorJson);

                    var replyProperties = _channel.CreateBasicProperties();
                    replyProperties.CorrelationId = correlationId;
                    replyProperties.Type = operationType;

                    _channel.BasicPublish("", ea.BasicProperties.ReplyTo, replyProperties, errorBytes);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception replyEx)
                {
                    _logger.LogError(replyEx, "Failed to send error response for message {OperationType}", operationType);
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            }
        }

        private async Task<object> HandleMessageAsync(string operationType, IRequest request)
        {
            using var scope = _serviceProvider.CreateScope();

            return operationType switch
            {
                OperationTypes.CreateUser => await scope.ServiceProvider
                    .GetRequiredService<IMessageHandler<CreateUserRequest, CreateUserResponse>>()
                    .HandleAsync((CreateUserRequest)request),
                OperationTypes.GetUser => await scope.ServiceProvider
                    .GetRequiredService<IMessageHandler<GetUserRequest, GetUserResponse>>()
                    .HandleAsync((GetUserRequest)request),
                OperationTypes.GetAllUsers => await scope.ServiceProvider
                    .GetRequiredService<IMessageHandler<GetAllUsersRequest, GetAllUsersResponse>>()
                    .HandleAsync((GetAllUsersRequest)request),
                OperationTypes.UpdateUser => await scope.ServiceProvider
                    .GetRequiredService<IMessageHandler<UpdateUserRequest, UpdateUserResponse>>()
                    .HandleAsync((UpdateUserRequest)request),
                OperationTypes.DeleteUser => await scope.ServiceProvider
                    .GetRequiredService<IMessageHandler<DeleteUserRequest, DeleteUserResponse>>()
                    .HandleAsync((DeleteUserRequest)request),
                _ => throw new NotSupportedException($"Operation type {operationType} is not supported")
            };
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping RabbitMQ User RPC Server");
            await base.StopAsync(cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }

    public static class RabbitMqUserRpcServerExtensions
    {
        public static IServiceCollection AddRabbitMqUserRpcServer(this IServiceCollection services)
        {
            services.AddSingleton<IRabbitMqRpcServer, RabbitMqUserRpcServer>();
            services.AddHostedService<RabbitMqUserRpcServer>();
            return services;
        }
    }
}
