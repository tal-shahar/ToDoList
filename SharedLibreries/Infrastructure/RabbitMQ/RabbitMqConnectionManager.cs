using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SharedLibreries.Infrastructure.Resilience;

namespace SharedLibreries.Infrastructure.RabbitMQ
{
    public interface IRabbitMqConnectionManager : IDisposable
    {
        Task<IConnection> GetConnectionAsync();
        Task<IModel> GetChannelAsync();
        bool IsConnected { get; }
        event EventHandler<ConnectionEventArgs>? ConnectionLost;
        event EventHandler<ConnectionEventArgs>? ConnectionRestored;
    }

    public class RabbitMqConnectionManager : IRabbitMqConnectionManager
    {
        private readonly ConnectionFactory _factory;
        private readonly ILogger<RabbitMqConnectionManager> _logger;
        private readonly ICircuitBreaker _circuitBreaker;
        private readonly IRetryPolicy _retryPolicy;
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private readonly SemaphoreSlim _channelSemaphore = new(10, 10);

        private IConnection? _connection;
        private bool _disposed = false;

        public bool IsConnected => _connection?.IsOpen == true;

        public event EventHandler<ConnectionEventArgs>? ConnectionLost;
        public event EventHandler<ConnectionEventArgs>? ConnectionRestored;

        public RabbitMqConnectionManager(
            IConfiguration configuration,
            ILogger<RabbitMqConnectionManager> logger,
            ICircuitBreaker circuitBreaker,
            IRetryPolicy retryPolicy)
        {
            _logger = logger;
            _circuitBreaker = circuitBreaker;
            _retryPolicy = retryPolicy;

            _factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                TopologyRecoveryEnabled = true
            };
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMqConnectionManager));

            await _connectionSemaphore.WaitAsync();
            try
            {
                if (_connection?.IsOpen == true)
                    return _connection!;

                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    return await _retryPolicy.ExecuteAsync(async () =>
                    {
                        _logger.LogInformation("Creating new RabbitMQ connection");
                        _connection?.Dispose();
                        _connection = _factory.CreateConnection();
                        _connection.ConnectionShutdown += OnConnectionShutdown;
                        
                        ConnectionRestored?.Invoke(this, new ConnectionEventArgs());
                        _logger.LogInformation("RabbitMQ connection established");
                        
                        return _connection;
                    });
                });
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task<IModel> GetChannelAsync()
        {
            await _channelSemaphore.WaitAsync();
            try
            {
                var connection = await GetConnectionAsync();
                return connection.CreateModel();
            }
            finally
            {
                _channelSemaphore.Release();
            }
        }

        private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            _logger.LogWarning("RabbitMQ connection lost: {Reason}", e.ReplyText);
            ConnectionLost?.Invoke(this, new ConnectionEventArgs());
        }

        public void Dispose()
        {
            if (_disposed) return;

            _connection?.Close();
            _connection?.Dispose();
            _connectionSemaphore?.Dispose();
            _channelSemaphore?.Dispose();

            _disposed = true;
            _logger.LogInformation("RabbitMQ connection manager disposed");
            
            GC.SuppressFinalize(this);
        }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }

    public static class RabbitMqConnectionManagerExtensions
    {
        public static IServiceCollection AddRabbitMqConnectionManager(this IServiceCollection services)
        {
            services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
            services.AddSingleton<IRetryPolicy, RetryPolicy>();
            services.AddSingleton<IRabbitMqConnectionManager, RabbitMqConnectionManager>();
            return services;
        }
    }
}
