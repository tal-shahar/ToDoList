using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SharedLibreries.Infrastructure.Resilience;

namespace SharedLibreries.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Manages a connection pool for RabbitMQ client connections with automatic health monitoring,
    /// dead connection removal, and circuit breaker integration.
    /// Implements connection lifecycle management including creation, validation, and cleanup.
    /// </summary>
    public interface IRabbitMqClientConnectionManager : IDisposable
    {
        /// <summary>
        /// Retrieves a healthy connection from the pool or creates a new one if none available.
        /// Automatically removes dead connections and applies retry logic with circuit breaker protection.
        /// </summary>
        Task<IConnection> GetConnectionAsync();

        /// <summary>
        /// Gets or creates a channel from a healthy connection.
        /// Validates connection health before channel creation and retries if connection is dead.
        /// </summary>
        Task<IModel> GetChannelAsync();

        /// <summary>
        /// Indicates whether at least one healthy connection exists in the pool.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Current number of healthy connections in the pool.
        /// </summary>
        int PoolSize { get; }

        /// <summary>
        /// Raised when a connection in the pool is lost.
        /// </summary>
        event EventHandler? ConnectionLost;

        /// <summary>
        /// Raised when a new connection is added to the pool.
        /// </summary>
        event EventHandler? ConnectionRestored;
    }

    /// <summary>
    /// Manages a thread-safe connection pool with automatic health monitoring and dead connection removal.
    /// Thread-safe implementation using SemaphoreSlim for serialized access to connection pool operations.
    /// </summary>
    public class RabbitMqClientConnectionManager : IRabbitMqClientConnectionManager
    {
        private readonly ConnectionFactory _factory;
        private readonly ILogger<RabbitMqClientConnectionManager> _logger;
        private readonly ICircuitBreaker _circuitBreaker;
        private readonly IRetryPolicy _retryPolicy;
        private readonly SemaphoreSlim _connectionSemaphore;
        private readonly List<PooledConnection> _connectionPool;
        private readonly int _maxPoolSize;
        private readonly object _poolLock = new object();
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance with configuration, logging, resilience policies, and pool size.
        /// Sets up connection factory with automatic recovery enabled.
        /// </summary>

        public bool IsConnected
        {
            get
            {
                lock (_poolLock)
                {
                    return _connectionPool.Any(c => c.IsHealthy && c.Connection?.IsOpen == true);
                }
            }
        }

        public int PoolSize
        {
            get
            {
                lock (_poolLock)
                {
                    return _connectionPool.Count(c => c.IsHealthy && c.Connection?.IsOpen == true);
                }
            }
        }

        public event EventHandler? ConnectionLost;
        public event EventHandler? ConnectionRestored;

        public RabbitMqClientConnectionManager(
            IConfiguration configuration,
            ILogger<RabbitMqClientConnectionManager> logger,
            ICircuitBreaker circuitBreaker,
            IRetryPolicy retryPolicy,
            int maxPoolSize = 10)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            _maxPoolSize = maxPoolSize > 0 ? maxPoolSize : throw new ArgumentException("Max pool size must be greater than 0", nameof(maxPoolSize));
            
            _connectionSemaphore = new SemaphoreSlim(1, 1);
            _connectionPool = [];

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

        /// <summary>
        /// Retrieves a healthy connection from the pool. If none exists, creates a new connection
        /// with retry logic and circuit breaker protection. Thread-safe implementation.
        /// </summary>
        public async Task<IConnection> GetConnectionAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMqClientConnectionManager));

            // Clean up dead connections first (optimistic)
            await CleanupDeadConnectionsAsync();

            // Try to find a healthy connection (lock-free read)
            PooledConnection? healthyConnection;
            lock (_poolLock)
            {
                healthyConnection = _connectionPool
                    .FirstOrDefault(c => c.IsHealthy && c.Connection?.IsOpen == true);
            }

            if (healthyConnection != null)
            {
                return healthyConnection.Connection;
            }

            // No healthy connection, acquire semaphore for pool modification
            await _connectionSemaphore.WaitAsync();
            try
            {
                // Double-check pattern: check again after acquiring lock
                lock (_poolLock)
                {
                    healthyConnection = _connectionPool
                        .FirstOrDefault(c => c.IsHealthy && c.Connection?.IsOpen == true);
                }

                if (healthyConnection != null)
                {
                    return healthyConnection.Connection;
                }

                // Check if pool is at max size
                int currentPoolSize;
                lock (_poolLock)
                {
                    currentPoolSize = _connectionPool.Count;
                }

                if (currentPoolSize >= _maxPoolSize)
                {
                    _logger.LogWarning(
                        "Connection pool is at maximum size ({MaxPoolSize}/{MaxPoolSize}). " +
                        "Waiting briefly before retry.", _maxPoolSize, _maxPoolSize);
                    await Task.Delay(100);
                    _connectionSemaphore.Release();
                    return await GetConnectionAsync(); // Retry with backoff
                }

                // Create new connection with resilience policies
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    return await _retryPolicy.ExecuteAsync(async () =>
                    {
                        _logger.LogInformation(
                            "Creating new RabbitMQ client connection (pool: {CurrentSize}/{MaxSize})",
                            currentPoolSize, _maxPoolSize);

                        var connection = _factory.CreateConnection();
                        var pooledConnection = new PooledConnection(connection);

                        // Setup connection shutdown event handler
                        connection.ConnectionShutdown += (sender, args) =>
                        {
                            _logger.LogWarning(
                                "Connection in pool lost. Reason: {Reason}. ConnectionId: {ConnectionId}",
                                args.ReplyText ?? "Unknown", connection.ServerProperties);

                            pooledConnection.MarkUnhealthy();
                            ConnectionLost?.Invoke(this, EventArgs.Empty);
                        };

                        // Add to pool (thread-safe)
                        lock (_poolLock)
                        {
                            _connectionPool.Add(pooledConnection);
                        }

                        _logger.LogInformation(
                            "Added connection to pool. Total pool size: {PoolSize}/{MaxSize}",
                            _connectionPool.Count, _maxPoolSize);

                        ConnectionRestored?.Invoke(this, EventArgs.Empty);

                        return connection;
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get or create RabbitMQ connection");
                throw;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets or creates a channel from a healthy connection.
        /// Validates connection state and removes dead connections automatically.
        /// </summary>
        public async Task<IModel> GetChannelAsync()
        {
            var connection = await GetConnectionAsync();

            // Validate connection is still healthy before creating channel
            if (connection == null || !connection.IsOpen)
            {
                _logger.LogWarning(
                    "Connection is null or closed when attempting to create channel. " +
                    "Removing from pool and retrying.");

                RemoveConnection(connection);
                connection = await GetConnectionAsync();
            }

            // Final validation
            if (connection == null)
            {
                throw new InvalidOperationException(
                    "Failed to obtain a valid connection after retry");
            }

            if (!connection.IsOpen)
            {
                throw new InvalidOperationException(
                    "Unable to create channel from a closed connection even after retry");
            }

            return connection.CreateModel();
        }

        /// <summary>
        /// Removes all dead or unhealthy connections from the pool.
        /// Thread-safe operation using lock.
        /// </summary>
        private Task CleanupDeadConnectionsAsync()
        {
            List<PooledConnection> deadConnections;
            lock (_poolLock)
            {
                deadConnections = _connectionPool
                    .Where(c => !c.IsHealthy || c.Connection?.IsOpen != true)
                    .ToList();
            }

            if (!deadConnections.Any())
                return Task.CompletedTask;

            foreach (var dead in deadConnections)
            {
                _logger.LogDebug("Marking connection for removal from pool");
                if (dead.Connection != null)
                {
                    RemoveConnection(dead.Connection);
                }
            }

            _logger.LogInformation(
                "Cleaned up {Count} dead connection(s) from pool. Remaining pool size: {Remaining}",
                deadConnections.Count, _connectionPool.Count);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a connection from the pool and disposes it safely.
        /// Thread-safe operation.
        /// </summary>
        private void RemoveConnection(IConnection? connection)
        {
            if (connection == null)
                return;

            lock (_poolLock)
            {
                _connectionPool.RemoveAll(c => c.Connection == connection);
            }

            try
            {
                if (connection.IsOpen)
                {
                    connection.Close();
                }
                connection.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing connection during removal");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var pooled in _connectionPool)
            {
                try
                {
                    pooled.Connection.Close();
                    pooled.Connection.Dispose();
                }
                catch { }
            }

            _connectionPool.Clear();
            _connectionSemaphore?.Dispose();

            _disposed = true;
            _logger.LogInformation("RabbitMQ client connection manager disposed");
            
            GC.SuppressFinalize(this);
        }

        private class PooledConnection
        {
            public IConnection Connection { get; }
            public bool IsHealthy { get; private set; }

            public PooledConnection(IConnection connection)
            {
                Connection = connection;
                IsHealthy = true;
            }

            public void MarkUnhealthy()
            {
                IsHealthy = false;
            }
        }
    }

    public static class RabbitMqClientConnectionManagerExtensions
    {
        public static IServiceCollection AddRabbitMqClientConnectionManager(this IServiceCollection services, int maxPoolSize = 10)
        {
            services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
            services.AddSingleton<IRetryPolicy, RetryPolicy>();
            services.AddSingleton<IRabbitMqClientConnectionManager>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<RabbitMqClientConnectionManager>>();
                var circuitBreaker = sp.GetRequiredService<ICircuitBreaker>();
                var retryPolicy = sp.GetRequiredService<IRetryPolicy>();
                return new RabbitMqClientConnectionManager(configuration, logger, circuitBreaker, retryPolicy, maxPoolSize);
            });
            return services;
        }
    }
}

