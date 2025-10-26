using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibreries.Infrastructure.Resilience;

namespace SharedLibreries.Infrastructure.Database
{
    public abstract class BaseDbContext : DbContext
    {
        private readonly ILogger? _logger;
        private readonly ICircuitBreaker? _circuitBreaker;
        private readonly IRetryPolicy? _retryPolicy;

        protected BaseDbContext(
            DbContextOptions options,
            ILogger? logger = null,
            ICircuitBreaker? circuitBreaker = null,
            IRetryPolicy? retryPolicy = null) : base(options)
        {
            _logger = logger;
            _circuitBreaker = circuitBreaker;
            _retryPolicy = retryPolicy;
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return ExecuteWithResilience(() => base.SaveChanges());
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await ExecuteWithResilienceAsync(() => base.SaveChangesAsync(cancellationToken));
        }

        private int ExecuteWithResilience(Func<int> operation)
        {
            if (_circuitBreaker == null || _retryPolicy == null)
                return operation();

            try
            {
                return _circuitBreaker.ExecuteAsync(async () =>
                {
                    return await _retryPolicy.ExecuteAsync(async () =>
                    {
                        return await Task.FromResult(operation());
                    });
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Database operation failed after retries");
                throw;
            }
        }

        private async Task<int> ExecuteWithResilienceAsync(Func<Task<int>> operation)
        {
            if (_circuitBreaker == null || _retryPolicy == null)
                return await operation();

            try
            {
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    return await _retryPolicy.ExecuteAsync(async () =>
                    {
                        return await operation();
                    });
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Database operation failed after retries");
                throw;
            }
        }

        protected abstract void UpdateTimestamps();

        protected void UpdateTimestampsForEntity<T>(Func<T, DateTime> getUpdatedAt, Action<T, DateTime> setUpdatedAt, Func<T, DateTime>? getCreatedAt = null, Action<T, DateTime>? setCreatedAt = null) where T : class
        {
            var entries = ChangeTracker.Entries<T>();

            foreach (var entry in entries)
            {
                if (entry.Entity is T entity)
                {
                    setUpdatedAt(entity, DateTime.UtcNow);
                    
                    if (entry.State == EntityState.Added && getCreatedAt != null && setCreatedAt != null)
                    {
                        setCreatedAt(entity, DateTime.UtcNow);
                    }
                }
            }
        }
    }
}
