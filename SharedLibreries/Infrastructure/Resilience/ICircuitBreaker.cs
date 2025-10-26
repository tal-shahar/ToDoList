namespace SharedLibreries.Infrastructure.Resilience
{
    public interface ICircuitBreaker
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
        Task ExecuteAsync(Func<Task> operation);
        bool IsOpen { get; }
        CircuitBreakerState State { get; }
    }

    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }
}
