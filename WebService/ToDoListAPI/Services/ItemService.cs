using SharedLibreries.Constants;
using SharedLibreries.Contracts;
using SharedLibreries.RabbitMQ;
using CreateItemRequest = SharedLibreries.DTOs.CreateItemRequest;
using UpdateItemRequest = SharedLibreries.DTOs.UpdateItemRequest;

namespace ToDoListAPI.Services
{
    public interface IItemService
    {
        Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request);
        Task<GetItemResponse> GetItemAsync(Guid itemId);
        Task<GetAllItemsResponse> GetAllItemsAsync();
        Task<GetUserItemsResponse> GetUserItemsAsync(Guid userId);
        Task<UpdateItemResponse> UpdateItemAsync(Guid itemId, UpdateItemRequest request);
        Task<DeleteItemResponse> DeleteItemAsync(Guid itemId);
    }

    public class ItemService : IItemService
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly ILogger<ItemService> _logger;

        public ItemService(IRabbitMqService rabbitMqService, ILogger<ItemService> logger)
        {
            _rabbitMqService = rabbitMqService;
            _logger = logger;
        }

        public async Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request)
        {
            try
            {
                var rpcRequest = new SharedLibreries.Contracts.CreateItemRequest
                {
                    UserId = request.UserId,
                    Title = request.Title,
                    Description = request.Description
                };

                return await _rabbitMqService.SendRpcRequestAsync<SharedLibreries.Contracts.CreateItemRequest, CreateItemResponse>(
                    rpcRequest, 
                    QueueNames.ItemQueue, 
                    OperationTypes.CreateItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item via RPC");
                return new CreateItemResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GetItemResponse> GetItemAsync(Guid itemId)
        {
            try
            {
                var rpcRequest = new GetItemRequest
                {
                    ItemId = itemId
                };

                return await _rabbitMqService.SendRpcRequestAsync<GetItemRequest, GetItemResponse>(
                    rpcRequest, 
                    QueueNames.ItemQueue, 
                    OperationTypes.GetItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item {ItemId} via RPC", itemId);
                return new GetItemResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GetAllItemsResponse> GetAllItemsAsync()
        {
            try
            {
                var rpcRequest = new GetAllItemsRequest();

                return await _rabbitMqService.SendRpcRequestAsync<GetAllItemsRequest, GetAllItemsResponse>(
                    rpcRequest, 
                    QueueNames.ItemQueue, 
                    OperationTypes.GetAllItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all items via RPC");
                return new GetAllItemsResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GetUserItemsResponse> GetUserItemsAsync(Guid userId)
        {
            try
            {
                var rpcRequest = new GetUserItemsRequest
                {
                    UserId = userId
                };

                return await _rabbitMqService.SendRpcRequestAsync<GetUserItemsRequest, GetUserItemsResponse>(
                    rpcRequest, 
                    QueueNames.ItemQueue, 
                    OperationTypes.GetUserItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for user {UserId} via RPC", userId);
                return new GetUserItemsResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<UpdateItemResponse> UpdateItemAsync(Guid itemId, UpdateItemRequest request)
        {
            try
            {
                var rpcRequest = new SharedLibreries.Contracts.UpdateItemRequest
                {
                    ItemId = itemId,
                    Title = request.Title,
                    Description = request.Description,
                    IsCompleted = request.IsCompleted
                };

                return await _rabbitMqService.SendRpcRequestAsync<SharedLibreries.Contracts.UpdateItemRequest, UpdateItemResponse>(
                    rpcRequest, 
                    QueueNames.ItemQueue, 
                    OperationTypes.UpdateItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId} via RPC", itemId);
                return new UpdateItemResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<DeleteItemResponse> DeleteItemAsync(Guid itemId)
        {
            try
            {
                var rpcRequest = new DeleteItemRequest
                {
                    ItemId = itemId
                };

                return await _rabbitMqService.SendRpcRequestAsync<DeleteItemRequest, DeleteItemResponse>(
                    rpcRequest, 
                    QueueNames.ItemQueue, 
                    OperationTypes.DeleteItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId} via RPC", itemId);
                return new DeleteItemResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
