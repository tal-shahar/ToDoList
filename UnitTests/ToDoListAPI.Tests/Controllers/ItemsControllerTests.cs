using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using ToDoListAPI.Controllers;
using ToDoListAPI.Services;

namespace ToDoListAPI.Tests.Controllers
{
    public class ItemsControllerTests
    {
        private readonly Mock<IItemService> _mockItemService;
        private readonly Mock<ILogger<ItemsController>> _mockLogger;
        private readonly ItemsController _controller;

        public ItemsControllerTests()
        {
            _mockItemService = new Mock<IItemService>();
            _mockLogger = new Mock<ILogger<ItemsController>>();
            _controller = new ItemsController(_mockItemService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateItem_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateItemRequest
            {
                UserId = Guid.NewGuid(),
                Title = "Test Item",
                Description = "Test Description"
            };

            var serviceResponse = new CreateItemResponse
            {
                IsSuccess = true,
                ItemId = Guid.NewGuid(),
                UserId = request.UserId,
                Title = request.Title
            };

            var itemResponse = new ItemResponse
            {
                Id = serviceResponse.ItemId.Value,
                UserId = request.UserId,
                Title = request.Title,
                Description = request.Description,
                IsCompleted = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockItemService
                .Setup(x => x.CreateItemAsync(request))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.CreateItem(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(_controller.GetItem), createdResult.ActionName);
            Assert.Equal(itemResponse.Id, ((ItemResponse)createdResult.Value!).Id);
            _mockItemService.Verify(x => x.CreateItemAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateItem_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateItemRequest
            {
                UserId = Guid.NewGuid(),
                Title = "Test Item",
                Description = "Test Description"
            };

            var serviceResponse = new CreateItemResponse
            {
                IsSuccess = false,
                ErrorMessage = "User not found"
            };

            _mockItemService
                .Setup(x => x.CreateItemAsync(request))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.CreateItem(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(serviceResponse.ErrorMessage, badRequestResult.Value);
            _mockItemService.Verify(x => x.CreateItemAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateItem_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateItemRequest
            {
                UserId = Guid.NewGuid(),
                Title = "Test Item",
                Description = "Test Description"
            };

            _mockItemService
                .Setup(x => x.CreateItemAsync(request))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CreateItem(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Internal server error", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetAllItems_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var serviceResponse = new GetAllItemsResponse
            {
                IsSuccess = true,
                Items = new List<ItemResponse>
                {
                    new ItemResponse { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Item 1" },
                    new ItemResponse { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Item 2" }
                }
            };

            _mockItemService
                .Setup(x => x.GetAllItemsAsync())
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.GetAllItems();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var items = Assert.IsAssignableFrom<IEnumerable<ItemResponse>>(okResult.Value);
            Assert.Equal(2, items.Count());
            _mockItemService.Verify(x => x.GetAllItemsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetItem_ValidId_ReturnsOkResult()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var itemResponse = new ItemResponse
            {
                Id = itemId,
                UserId = Guid.NewGuid(),
                Title = "Test Item",
                Description = "Test Description",
                IsCompleted = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var serviceResponse = new GetItemResponse
            {
                IsSuccess = true,
                Item = itemResponse
            };

            _mockItemService
                .Setup(x => x.GetItemAsync(itemId))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.GetItem(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedItem = Assert.IsType<ItemResponse>(okResult.Value);
            Assert.Equal(itemId, returnedItem.Id);
            _mockItemService.Verify(x => x.GetItemAsync(itemId), Times.Once);
        }

        [Fact]
        public async Task GetItem_ItemNotFound_ReturnsNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var serviceResponse = new GetItemResponse
            {
                IsSuccess = false,
                ErrorMessage = "Item not found"
            };

            _mockItemService
                .Setup(x => x.GetItemAsync(itemId))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.GetItem(itemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(serviceResponse.ErrorMessage, notFoundResult.Value);
            _mockItemService.Verify(x => x.GetItemAsync(itemId), Times.Once);
        }

        [Fact]
        public async Task GetUserItems_ValidUserId_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var serviceResponse = new GetUserItemsResponse
            {
                IsSuccess = true,
                Items = new List<ItemResponse>
                {
                    new ItemResponse { Id = Guid.NewGuid(), UserId = userId, Title = "User Item 1" },
                    new ItemResponse { Id = Guid.NewGuid(), UserId = userId, Title = "User Item 2" }
                }
            };

            _mockItemService
                .Setup(x => x.GetUserItemsAsync(userId))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.GetUserItems(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var items = Assert.IsAssignableFrom<IEnumerable<ItemResponse>>(okResult.Value);
            Assert.Equal(2, items.Count());
            Assert.All(items, item => Assert.Equal(userId, item.UserId));
            _mockItemService.Verify(x => x.GetUserItemsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task UpdateItem_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var request = new SharedLibreries.DTOs.UpdateItemRequest
            {
                Title = "Updated Item",
                Description = "Updated Description",
                IsCompleted = true
            };

            var itemResponse = new ItemResponse
            {
                Id = itemId,
                UserId = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                IsCompleted = request.IsCompleted,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var serviceResponse = new UpdateItemResponse
            {
                IsSuccess = true,
                Item = itemResponse
            };

            _mockItemService
                .Setup(x => x.UpdateItemAsync(itemId, request))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.UpdateItem(itemId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedItem = Assert.IsType<ItemResponse>(okResult.Value);
            Assert.Equal(itemId, returnedItem.Id);
            Assert.Equal(request.Title, returnedItem.Title);
            Assert.Equal(request.Description, returnedItem.Description);
            Assert.Equal(request.IsCompleted, returnedItem.IsCompleted);
            _mockItemService.Verify(x => x.UpdateItemAsync(itemId, request), Times.Once);
        }

        [Fact]
        public async Task UpdateItem_ItemNotFound_ReturnsNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var request = new SharedLibreries.DTOs.UpdateItemRequest
            {
                Title = "Updated Item",
                Description = "Updated Description",
                IsCompleted = true
            };

            var serviceResponse = new UpdateItemResponse
            {
                IsSuccess = false,
                ErrorMessage = "Item not found"
            };

            _mockItemService
                .Setup(x => x.UpdateItemAsync(itemId, request))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.UpdateItem(itemId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(serviceResponse.ErrorMessage, notFoundResult.Value);
            _mockItemService.Verify(x => x.UpdateItemAsync(itemId, request), Times.Once);
        }

        [Fact]
        public async Task DeleteItem_ValidId_ReturnsNoContent()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var serviceResponse = new DeleteItemResponse
            {
                IsSuccess = true
            };

            _mockItemService
                .Setup(x => x.DeleteItemAsync(itemId))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.DeleteItem(itemId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockItemService.Verify(x => x.DeleteItemAsync(itemId), Times.Once);
        }

        [Fact]
        public async Task DeleteItem_ItemNotFound_ReturnsNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var serviceResponse = new DeleteItemResponse
            {
                IsSuccess = false,
                ErrorMessage = "Item not found"
            };

            _mockItemService
                .Setup(x => x.DeleteItemAsync(itemId))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.DeleteItem(itemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(serviceResponse.ErrorMessage, notFoundResult.Value);
            _mockItemService.Verify(x => x.DeleteItemAsync(itemId), Times.Once);
        }
    }
}
