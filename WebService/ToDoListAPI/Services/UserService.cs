using SharedLibreries.Constants;
using SharedLibreries.Contracts;
using SharedLibreries.RabbitMQ;

namespace ToDoListAPI.Services
{
    public interface IUserService
    {
        Task<CreateUserResponse> CreateUserAsync(SharedLibreries.DTOs.CreateUserRequest request);
        Task<GetUserResponse> GetUserAsync(Guid userId);
        Task<GetAllUsersResponse> GetAllUsersAsync();
        Task<UpdateUserResponse> UpdateUserAsync(Guid userId, SharedLibreries.DTOs.UpdateUserRequest request);
        Task<DeleteUserResponse> DeleteUserAsync(Guid userId);
    }

    public class UserService : IUserService
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly ILogger<UserService> _logger;

        public UserService(IRabbitMqService rabbitMqService, ILogger<UserService> logger)
        {
            _rabbitMqService = rabbitMqService;
            _logger = logger;
        }

        public async Task<CreateUserResponse> CreateUserAsync(SharedLibreries.DTOs.CreateUserRequest request)
        {
            try
            {
                var rpcRequest = new CreateUserRequest
                {
                    Name = request.Name,
                    Email = request.Email
                };

                return await _rabbitMqService.SendRpcRequestAsync<CreateUserRequest, CreateUserResponse>(
                    rpcRequest, 
                    QueueNames.UserQueue, 
                    OperationTypes.CreateUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user via RPC");
                return new CreateUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GetUserResponse> GetUserAsync(Guid userId)
        {
            try
            {
                var rpcRequest = new GetUserRequest
                {
                    UserId = userId
                };

                return await _rabbitMqService.SendRpcRequestAsync<GetUserRequest, GetUserResponse>(
                    rpcRequest, 
                    QueueNames.UserQueue, 
                    OperationTypes.GetUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId} via RPC", userId);
                return new GetUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GetAllUsersResponse> GetAllUsersAsync()
        {
            try
            {
                var rpcRequest = new GetAllUsersRequest();

                return await _rabbitMqService.SendRpcRequestAsync<GetAllUsersRequest, GetAllUsersResponse>(
                    rpcRequest, 
                    QueueNames.UserQueue, 
                    OperationTypes.GetAllUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users via RPC");
                return new GetAllUsersResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<UpdateUserResponse> UpdateUserAsync(Guid userId, SharedLibreries.DTOs.UpdateUserRequest request)
        {
            try
            {
                var rpcRequest = new UpdateUserRequest
                {
                    UserId = userId,
                    Name = request.Name,
                    Email = request.Email
                };

                return await _rabbitMqService.SendRpcRequestAsync<UpdateUserRequest, UpdateUserResponse>(
                    rpcRequest, 
                    QueueNames.UserQueue, 
                    OperationTypes.UpdateUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId} via RPC", userId);
                return new UpdateUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<DeleteUserResponse> DeleteUserAsync(Guid userId)
        {
            try
            {
                var rpcRequest = new DeleteUserRequest
                {
                    UserId = userId
                };

                return await _rabbitMqService.SendRpcRequestAsync<DeleteUserRequest, DeleteUserResponse>(
                    rpcRequest, 
                    QueueNames.UserQueue, 
                    OperationTypes.DeleteUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId} via RPC", userId);
                return new DeleteUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
