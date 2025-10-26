using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedLibreries.Infrastructure.Resilience;

namespace SharedLibreries.Infrastructure.Database
{
    public interface IDatabaseHealthChecker
    {
        Task<bool> IsHealthyAsync();
        Task EnsureDatabaseExistsAsync();
    }

    public class DatabaseHealthChecker<TContext> : IDatabaseHealthChecker where TContext : DbContext
    {
        private readonly TContext _dbContext;
        private readonly ILogger<DatabaseHealthChecker<TContext>> _logger;
        private readonly ICircuitBreaker _circuitBreaker;
        private readonly IRetryPolicy _retryPolicy;

        public DatabaseHealthChecker(
            TContext dbContext,
            ILogger<DatabaseHealthChecker<TContext>> logger,
            ICircuitBreaker circuitBreaker,
            IRetryPolicy retryPolicy)
        {
            _dbContext = dbContext;
            _logger = logger;
            _circuitBreaker = circuitBreaker;
            _retryPolicy = retryPolicy;
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    return await _retryPolicy.ExecuteAsync(async () =>
                    {
                        await _dbContext.Database.CanConnectAsync();
                        return true;
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return false;
            }
        }

        public async Task EnsureDatabaseExistsAsync()
        {
            await _circuitBreaker.ExecuteAsync(async () =>
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _dbContext.Database.EnsureCreatedAsync();
                    _logger.LogInformation("Database ensured to exist");
                });
            });
        }
    }

    public static class DatabaseExtensions
    {
        public static IServiceCollection AddResilientDatabase<TContext>(
            this IServiceCollection services,
            IConfiguration configuration,
            string connectionStringName = "DefaultConnection")
            where TContext : DbContext
        {
            services.AddDbContext<TContext>(options =>
            {
                var connectionString = configuration.GetConnectionString(connectionStringName);
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(30);
                });
                
                options.EnableSensitiveDataLogging(false);
                options.EnableServiceProviderCaching();
            });

            services.AddScoped<IDatabaseHealthChecker, DatabaseHealthChecker<TContext>>();
            
            return services;
        }
    }
}
