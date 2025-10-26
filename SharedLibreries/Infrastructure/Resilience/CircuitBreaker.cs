using Microsoft.Extensions.Logging;

namespace SharedLibreries.Infrastructure.Resilience
{
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _recoveryTimeout;
        private readonly ILogger<CircuitBreaker>? _logger;

        private int _failureCount;
        private DateTime _lastFailureTime;
        private CircuitBreakerState _state = CircuitBreakerState.Closed;

        public CircuitBreaker(
            int failureThreshold = 5,
            TimeSpan timeout = default,
            TimeSpan recoveryTimeout = default,
            ILogger<CircuitBreaker>? logger = null)
        {
            _failureThreshold = failureThreshold;
            _timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout;
            _recoveryTimeout = recoveryTimeout == default ? TimeSpan.FromMinutes(1) : recoveryTimeout;
            _logger = logger;
        }

        public bool IsOpen => _state == CircuitBreakerState.Open;
        public CircuitBreakerState State => _state;

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (_state == CircuitBreakerState.Open)
            {
                if (DateTime.UtcNow - _lastFailureTime < _recoveryTimeout)
                {
                    _logger?.LogWarning("Circuit breaker is open, rejecting operation");
                    throw new CircuitBreakerOpenException("Circuit breaker is open");
                }
                else
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _logger?.LogInformation("Circuit breaker transitioning to half-open state");
                }
            }

            try
            {
                using var cts = new CancellationTokenSource(_timeout);
                var result = await operation();
                
                OnSuccess();
                return result;
            }
            catch (Exception ex)
            {
                OnFailure();
                _logger?.LogError(ex, "Operation failed, circuit breaker failure count: {FailureCount}", _failureCount);
                throw;
            }
        }

        public async Task ExecuteAsync(Func<Task> operation)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            });
        }

        private void OnSuccess()
        {
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
        }

        private void OnFailure()
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _logger?.LogWarning("Circuit breaker opened after {FailureCount} failures", _failureCount);
            }
        }
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
}
