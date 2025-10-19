using SharedLibreries.DTOs;

namespace SharedLibreries.Contracts
{
    public class CreateUserRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CreateUserResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    public class GetUserRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
    }

    public class GetUserResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public UserResponse? User { get; set; }
    }

    public class GetAllUsersRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class GetAllUsersResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<UserResponse> Users { get; set; } = new();
    }

    public class UpdateUserRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateUserResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public UserResponse? User { get; set; }
    }

    public class DeleteUserRequest : IRequest
    {
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
    }

    public class DeleteUserResponse : IResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
