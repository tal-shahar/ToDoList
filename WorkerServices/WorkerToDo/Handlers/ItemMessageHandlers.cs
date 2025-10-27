using Microsoft.Extensions.Logging;
using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using SharedLibreries.Models;
using SharedLibreries.RabbitMQ;
using WorkerServices.WorkerToDo.Repositories;

namespace WorkerToDo.Handlers
{
    public class CreateItemMessageHandler : IMessageHandler<SharedLibreries.Contracts.CreateItemRequest, CreateItemResponse>
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<CreateItemMessageHandler> _logger;

        public CreateItemMessageHandler(IItemRepository itemRepository, ILogger<CreateItemMessageHandler> logger)
        {
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public async Task<CreateItemResponse> HandleAsync(SharedLibreries.Contracts.CreateItemRequest request)
        {
            try
            {
                _logger.LogInformation("Processing CreateItem request for user {UserId}", request.UserId);

                // Note: User validation should be handled by the API layer or through inter-service communication
                // For now, we assume the user exists since this is called from the API after validation

                var item = new Item
                {
                    UserId = request.UserId,
                    Title = request.Title,
                    Description = request.Description,
                    IsCompleted = false,
                    IsDeleted = false
                };

                await _itemRepository.AddAsync(item);

                _logger.LogInformation("Item created successfully with ID {ItemId} for user {UserId}", item.Id, item.UserId);
                return new CreateItemResponse
                {
                    IsSuccess = true,
                    ItemId = item.Id,
                    UserId = item.UserId,
                    Title = item.Title
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item for user {UserId}", request.UserId);
                return new CreateItemResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class GetItemMessageHandler : IMessageHandler<GetItemRequest, GetItemResponse>
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<GetItemMessageHandler> _logger;

        public GetItemMessageHandler(IItemRepository itemRepository, ILogger<GetItemMessageHandler> logger)
        {
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public async Task<GetItemResponse> HandleAsync(GetItemRequest request)
        {
            try
            {
                _logger.LogInformation("Processing GetItem request for ID {ItemId}", request.ItemId);

                var item = await _itemRepository.GetByIdAsync(request.ItemId);
                if (item == null)
                {
                    return new GetItemResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Item with ID {request.ItemId} not found."
                    };
                }

                return new GetItemResponse
                {
                    IsSuccess = true,
                    Item = new ItemResponse
                    {
                        Id = item.Id,
                        UserId = item.UserId,
                        Title = item.Title,
                        Description = item.Description,
                        IsCompleted = item.IsCompleted,
                        IsDeleted = item.IsDeleted,
                        CreatedAt = item.CreatedAt,
                        UpdatedAt = item.UpdatedAt,
                        DeletedAt = item.DeletedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item with ID {ItemId}", request.ItemId);
                return new GetItemResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class GetAllItemsMessageHandler : IMessageHandler<GetAllItemsRequest, GetAllItemsResponse>
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<GetAllItemsMessageHandler> _logger;

        public GetAllItemsMessageHandler(IItemRepository itemRepository, ILogger<GetAllItemsMessageHandler> logger)
        {
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public async Task<GetAllItemsResponse> HandleAsync(GetAllItemsRequest request)
        {
            try
            {
                _logger.LogInformation("Processing GetAllItems request");

                var items = await _itemRepository.GetAllAsync();
                var itemResponses = items.Select(i => new ItemResponse
                {
                    Id = i.Id,
                    UserId = i.UserId,
                    Title = i.Title,
                    Description = i.Description,
                    IsCompleted = i.IsCompleted,
                    IsDeleted = i.IsDeleted,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    DeletedAt = i.DeletedAt
                }).ToList();

                return new GetAllItemsResponse
                {
                    IsSuccess = true,
                    Items = itemResponses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all items");
                return new GetAllItemsResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class GetUserItemsMessageHandler : IMessageHandler<GetUserItemsRequest, GetUserItemsResponse>
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<GetUserItemsMessageHandler> _logger;

        public GetUserItemsMessageHandler(IItemRepository itemRepository, ILogger<GetUserItemsMessageHandler> logger)
        {
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public async Task<GetUserItemsResponse> HandleAsync(GetUserItemsRequest request)
        {
            try
            {
                _logger.LogInformation("Processing GetUserItems request for user {UserId}", request.UserId);

                // Note: User validation should be handled by the API layer
                // For now, we assume the user exists since this is called from the API after validation

                var items = await _itemRepository.GetItemsByUserIdAsync(request.UserId);
                var itemResponses = items.Select(i => new ItemResponse
                {
                    Id = i.Id,
                    UserId = i.UserId,
                    Title = i.Title,
                    Description = i.Description,
                    IsCompleted = i.IsCompleted,
                    IsDeleted = i.IsDeleted,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    DeletedAt = i.DeletedAt
                }).ToList();

                return new GetUserItemsResponse
                {
                    IsSuccess = true,
                    Items = itemResponses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for user {UserId}", request.UserId);
                return new GetUserItemsResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class UpdateItemMessageHandler : IMessageHandler<SharedLibreries.Contracts.UpdateItemRequest, UpdateItemResponse>
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<UpdateItemMessageHandler> _logger;

        public UpdateItemMessageHandler(IItemRepository itemRepository, ILogger<UpdateItemMessageHandler> logger)
        {
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public async Task<UpdateItemResponse> HandleAsync(SharedLibreries.Contracts.UpdateItemRequest request)
        {
            try
            {
                _logger.LogInformation("Processing UpdateItem request for ID {ItemId}", request.ItemId);

                var item = await _itemRepository.GetByIdAsync(request.ItemId);
                if (item == null)
                {
                    return new UpdateItemResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Item with ID {request.ItemId} not found."
                    };
                }

                item.Title = request.Title;
                item.Description = request.Description;
                item.IsCompleted = request.IsCompleted;

                await _itemRepository.UpdateAsync(item);

                _logger.LogInformation("Item updated successfully with ID {ItemId}", item.Id);
                return new UpdateItemResponse
                {
                    IsSuccess = true,
                    Item = new ItemResponse
                    {
                        Id = item.Id,
                        UserId = item.UserId,
                        Title = item.Title,
                        Description = item.Description,
                        IsCompleted = item.IsCompleted,
                        IsDeleted = item.IsDeleted,
                        CreatedAt = item.CreatedAt,
                        UpdatedAt = item.UpdatedAt,
                        DeletedAt = item.DeletedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item with ID {ItemId}", request.ItemId);
                return new UpdateItemResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class DeleteItemMessageHandler : IMessageHandler<DeleteItemRequest, DeleteItemResponse>
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<DeleteItemMessageHandler> _logger;

        public DeleteItemMessageHandler(IItemRepository itemRepository, ILogger<DeleteItemMessageHandler> logger)
        {
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public async Task<DeleteItemResponse> HandleAsync(DeleteItemRequest request)
        {
            try
            {
                _logger.LogInformation("Processing DeleteItem request for ID {ItemId}", request.ItemId);

                var item = await _itemRepository.GetByIdAsync(request.ItemId);
                if (item == null)
                {
                    return new DeleteItemResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Item with ID {request.ItemId} not found."
                    };
                }

                await _itemRepository.SoftDeleteAsync(request.ItemId);

                _logger.LogInformation("Item soft deleted successfully with ID {ItemId}", request.ItemId);
                return new DeleteItemResponse
                {
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item with ID {ItemId}", request.ItemId);
                return new DeleteItemResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
