using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SharedLibreries.Infrastructure.Resilience
{
    public interface IRetryPolicy
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
        Task ExecuteAsync(Func<Task> operation);
    }

    public class RetryPolicy : IRetryPolicy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _delay;
        private readonly Func<Exception, bool> _shouldRetry;
        private readonly ILogger<RetryPolicy>? _logger;

        public RetryPolicy(
            int maxRetries = 3,
            TimeSpan delay = default,
            Func<Exception, bool>? shouldRetry = null,
            ILogger<RetryPolicy>? logger = null)
        {
            _maxRetries = maxRetries;
            _delay = delay == default ? TimeSpan.FromSeconds(1) : delay;
            _shouldRetry = shouldRetry ?? (ex => true);
            _logger = logger;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            Exception? lastException = null;
            
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (attempt < _maxRetries && _shouldRetry(ex))
                {
                    lastException = ex;
                    var delay = TimeSpan.FromMilliseconds(_delay.TotalMilliseconds * Math.Pow(2, attempt));
                    
                    _logger?.LogWarning(ex, "Operation failed, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})", 
                        delay.TotalMilliseconds, attempt + 1, _maxRetries + 1);
                    
                    await Task.Delay(delay);
                }
            }

            _logger?.LogError(lastException, "Operation failed after {MaxRetries} retries", _maxRetries);
            throw lastException ?? new InvalidOperationException("Operation failed after retries");
        }

        public async Task ExecuteAsync(Func<Task> operation)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            });
        }
    }
}
