using Microsoft.Extensions.Logging;
using Moq;
using SharedLibreries.Constants;
using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using SharedLibreries.RabbitMQ;
using ToDoListAPI.Services;

namespace ToDoListAPI.Tests.Services
{
    public class ItemServiceTests
    {
        private readonly Mock<IRabbitMqService> _mockRabbitMqService;
        private readonly Mock<ILogger<ItemService>> _mockLogger;
        private readonly ItemService _itemService;

        public ItemServiceTests()
        {
            _mockRabbitMqService = new Mock<IRabbitMqService>();
            _mockLogger = new Mock<ILogger<ItemService>>();
            _itemService = new ItemService(_mockRabbitMqService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateItemAsync_ValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateItemRequest
            {
                UserId = Guid.NewGuid(),
                Title = "Test Item",
                Description = "Test Description"
            };

            var expectedResponse = new CreateItemResponse
            {
                IsSuccess = true,
                ItemId = Guid.NewGuid(),
                UserId = request.UserId,
                Title = request.Title
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.CreateItemRequest, CreateItemResponse>(
                    It.IsAny<SharedLibreries.Contracts.CreateItemRequest>(),
                    QueueNames.ItemQueue,
                    OperationTypes.CreateItem))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _itemService.CreateItemAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedResponse.ItemId, result.ItemId);
            Assert.Equal(expectedResponse.UserId, result.UserId);
            Assert.Equal(expectedResponse.Title, result.Title);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.CreateItemRequest, CreateItemResponse>(
                It.Is<SharedLibreries.Contracts.CreateItemRequest>(r => r.UserId == request.UserId && r.Title == request.Title && r.Description == request.Description),
                QueueNames.ItemQueue,
                OperationTypes.CreateItem), Times.Once);
        }

        [Fact]
        public async Task CreateItemAsync_RabbitMqThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateItemRequest
            {
                UserId = Guid.NewGuid(),
                Title = "Test Item",
                Description = "Test Description"
            };

            var exception = new Exception("RabbitMQ connection failed");
            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.CreateItemRequest, CreateItemResponse>(
                    It.IsAny<SharedLibreries.Contracts.CreateItemRequest>(),
                    QueueNames.ItemQueue,
                    OperationTypes.CreateItem))
                .ThrowsAsync(exception);

            // Act
            var result = await _itemService.CreateItemAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(exception.Message, result.ErrorMessage);
        }

        [Fact]
        public async Task GetItemAsync_ValidItemId_ReturnsSuccessResponse()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var expectedResponse = new GetItemResponse
            {
                IsSuccess = true,
                Item = new ItemResponse
                {
                    Id = itemId,
                    UserId = Guid.NewGuid(),
                    Title = "Test Item",
                    Description = "Test Description",
                    IsCompleted = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetItemRequest, GetItemResponse>(
                    It.IsAny<SharedLibreries.Contracts.GetItemRequest>(),
                    QueueNames.ItemQueue,
                    OperationTypes.GetItem))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _itemService.GetItemAsync(itemId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Item);
            Assert.Equal(itemId, result.Item.Id);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetItemRequest, GetItemResponse>(
                It.Is<SharedLibreries.Contracts.GetItemRequest>(r => r.ItemId == itemId),
                QueueNames.ItemQueue,
                OperationTypes.GetItem), Times.Once);
        }

        [Fact]
        public async Task GetAllItemsAsync_ReturnsSuccessResponse()
        {
            // Arrange
            var expectedResponse = new GetAllItemsResponse
            {
                IsSuccess = true,
                Items = new List<ItemResponse>
                {
                    new ItemResponse { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Item 1" },
                    new ItemResponse { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Item 2" }
                }
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetAllItemsRequest, GetAllItemsResponse>(
                    It.IsAny<SharedLibreries.Contracts.GetAllItemsRequest>(),
                    QueueNames.ItemQueue,
                    OperationTypes.GetAllItems))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _itemService.GetAllItemsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Items);
            Assert.Equal(2, result.Items.Count);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetAllItemsRequest, GetAllItemsResponse>(
                It.IsAny<SharedLibreries.Contracts.GetAllItemsRequest>(),
                QueueNames.ItemQueue,
                OperationTypes.GetAllItems), Times.Once);
        }

        [Fact]
        public async Task GetUserItemsAsync_ValidUserId_ReturnsSuccessResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResponse = new GetUserItemsResponse
            {
                IsSuccess = true,
                Items = new List<ItemResponse>
                {
                    new ItemResponse { Id = Guid.NewGuid(), UserId = userId, Title = "User Item 1" },
                    new ItemResponse { Id = Guid.NewGuid(), UserId = userId, Title = "User Item 2" }
                }
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetUserItemsRequest, GetUserItemsResponse>(
                    It.IsAny<SharedLibreries.Contracts.GetUserItemsRequest>(),
                    QueueNames.ItemQueue,
                    OperationTypes.GetUserItems))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _itemService.GetUserItemsAsync(userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Items);
            Assert.Equal(2, result.Items.Count);
            Assert.All(result.Items, item => Assert.Equal(userId, item.UserId));
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetUserItemsRequest, GetUserItemsResponse>(
                It.Is<SharedLibreries.Contracts.GetUserItemsRequest>(r => r.UserId == userId),
                QueueNames.ItemQueue,
                OperationTypes.GetUserItems), Times.Once);
        }

        [Fact]
        public async Task UpdateItemAsync_ValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var request = new SharedLibreries.DTOs.UpdateItemRequest
            {
                Title = "Updated Item",
                Description = "Updated Description",
                IsCompleted = true
            };

            var expectedResponse = new UpdateItemResponse
            {
                IsSuccess = true,
                Item = new ItemResponse
                {
                    Id = itemId,
                    UserId = Guid.NewGuid(),
                    Title = request.Title,
                    Description = request.Description,
                    IsCompleted = request.IsCompleted,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.UpdateItemRequest, UpdateItemResponse>(
                    It.IsAny<SharedLibreries.Contracts.UpdateItemRequest>(),
                    QueueNames.ItemQueue,
                    OperationTypes.UpdateItem))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _itemService.UpdateItemAsync(itemId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Item);
            Assert.Equal(itemId, result.Item.Id);
            Assert.Equal(request.Title, result.Item.Title);
            Assert.Equal(request.Description, result.Item.Description);
            Assert.Equal(request.IsCompleted, result.Item.IsCompleted);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.UpdateItemRequest, UpdateItemResponse>(
                It.Is<SharedLibreries.Contracts.UpdateItemRequest>(r => r.ItemId == itemId && r.Title == request.Title && r.Description == request.Description && r.IsCompleted == request.IsCompleted),
                QueueNames.ItemQueue,
                OperationTypes.UpdateItem), Times.Once);
        }

        [Fact]
        public async Task DeleteItemAsync_ValidItemId_ReturnsSuccessResponse()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var expectedResponse = new DeleteItemResponse
            {
                IsSuccess = true
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.DeleteItemRequest, DeleteItemResponse>(
                    It.IsAny<SharedLibreries.Contracts.DeleteItemRequest>(),
                    QueueNames.ItemQueue,
                    OperationTypes.DeleteItem))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _itemService.DeleteItemAsync(itemId);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.DeleteItemRequest, DeleteItemResponse>(
                It.Is<SharedLibreries.Contracts.DeleteItemRequest>(r => r.ItemId == itemId),
                QueueNames.ItemQueue,
                OperationTypes.DeleteItem), Times.Once);
        }
    }
}
