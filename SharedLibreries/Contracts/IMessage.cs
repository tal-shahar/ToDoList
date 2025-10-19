namespace SharedLibreries.Contracts
{
    public interface IMessage
    {
        string CorrelationId { get; set; }
        DateTime Timestamp { get; set; }
    }

    public interface IRequest : IMessage
    {
    }

    public interface IResponse : IMessage
    {
        bool IsSuccess { get; set; }
        string? ErrorMessage { get; set; }
    }
}
