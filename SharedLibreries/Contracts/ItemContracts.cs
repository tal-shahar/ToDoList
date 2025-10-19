using SharedLibreries.DTOs;

namespace SharedLibreries.Contracts
{
    public class CreateItemRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CreateItemResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? ItemId { get; set; }
        public Guid? UserId { get; set; }
        public string? Title { get; set; }
    }

    public class GetItemRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid ItemId { get; set; }
    }

    public class GetItemResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public ItemResponse? Item { get; set; }
    }

    public class GetAllItemsRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class GetAllItemsResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ItemResponse> Items { get; set; } = new();
    }

    public class GetUserItemsRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
    }

    public class GetUserItemsResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ItemResponse> Items { get; set; } = new();
    }

    public class UpdateItemRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid ItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class UpdateItemResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public ItemResponse? Item { get; set; }
    }

    public class DeleteItemRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid ItemId { get; set; }
    }

    public class DeleteItemResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
