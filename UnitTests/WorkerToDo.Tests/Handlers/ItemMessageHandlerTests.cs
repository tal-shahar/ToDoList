using Microsoft.Extensions.Logging;
using Moq;
using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using SharedLibreries.Models;
using WorkerServices.WorkerToDo.Repositories;
using WorkerToDo.Handlers;

namespace WorkerToDo.Tests.Handlers
{
    public class CreateItemMessageHandlerTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<ILogger<CreateItemMessageHandler>> _mockLogger;
        private readonly CreateItemMessageHandler _handler;

        public CreateItemMessageHandlerTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockLogger = new Mock<ILogger<CreateItemMessageHandler>>();
            _handler = new CreateItemMessageHandler(_mockItemRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_CreatesItemSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new SharedLibreries.Contracts.CreateItemRequest
            {
                UserId = userId,
                Title = "Test Item",
                Description = "Test Description"
            };

            _mockItemRepository
                .Setup(x => x.AddAsync(It.IsAny<Item>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.ItemId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(request.Title, result.Title);
            _mockItemRepository.Verify(x => x.AddAsync(It.Is<Item>(i => i.UserId == userId && i.Title == request.Title && i.Description == request.Description)), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_RepositoryException_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new SharedLibreries.Contracts.CreateItemRequest
            {
                UserId = userId,
                Title = "Test Item",
                Description = "Test Description"
            };

            _mockItemRepository
                .Setup(x => x.AddAsync(It.IsAny<Item>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Database error", result.ErrorMessage);
        }
    }

    public class GetItemMessageHandlerTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<ILogger<GetItemMessageHandler>> _mockLogger;
        private readonly GetItemMessageHandler _handler;

        public GetItemMessageHandlerTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockLogger = new Mock<ILogger<GetItemMessageHandler>>();
            _handler = new GetItemMessageHandler(_mockItemRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ExistingItem_ReturnsItem()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new GetItemRequest
            {
                ItemId = itemId
            };

            var item = new Item
            {
                Id = itemId,
                UserId = userId,
                Title = "Test Item",
                Description = "Test Description",
                IsCompleted = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Item);
            Assert.Equal(itemId, result.Item.Id);
            Assert.Equal(item.Title, result.Item.Title);
            Assert.Equal(item.Description, result.Item.Description);
            _mockItemRepository.Verify(x => x.GetByIdAsync(itemId), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_NonExistingItem_ReturnsFailure()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var request = new GetItemRequest
            {
                ItemId = itemId
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync((Item?)null);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.ErrorMessage);
            _mockItemRepository.Verify(x => x.GetByIdAsync(itemId), Times.Once);
        }
    }

    public class GetAllItemsMessageHandlerTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<ILogger<GetAllItemsMessageHandler>> _mockLogger;
        private readonly GetAllItemsMessageHandler _handler;

        public GetAllItemsMessageHandlerTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockLogger = new Mock<ILogger<GetAllItemsMessageHandler>>();
            _handler = new GetAllItemsMessageHandler(_mockItemRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ReturnsAllItems()
        {
            // Arrange
            var request = new GetAllItemsRequest();

            var items = new List<Item>
            {
                new Item { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Item 1", Description = "Description 1", IsCompleted = false, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Item { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Item 2", Description = "Description 2", IsCompleted = true, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            _mockItemRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(items);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Items.Count);
            Assert.Contains(result.Items, i => i.Title == "Item 1");
            Assert.Contains(result.Items, i => i.Title == "Item 2");
            _mockItemRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }
    }

    public class GetUserItemsMessageHandlerTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<ILogger<GetUserItemsMessageHandler>> _mockLogger;
        private readonly GetUserItemsMessageHandler _handler;

        public GetUserItemsMessageHandlerTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockLogger = new Mock<ILogger<GetUserItemsMessageHandler>>();
            _handler = new GetUserItemsMessageHandler(_mockItemRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ValidUserId_ReturnsUserItems()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new GetUserItemsRequest
            {
                UserId = userId
            };

            var items = new List<Item>
            {
                new Item { Id = Guid.NewGuid(), UserId = userId, Title = "User Item 1", Description = "Description 1", IsCompleted = false, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Item { Id = Guid.NewGuid(), UserId = userId, Title = "User Item 2", Description = "Description 2", IsCompleted = true, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            _mockItemRepository
                .Setup(x => x.GetItemsByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Items.Count);
            Assert.All(result.Items, item => Assert.Equal(userId, item.UserId));
            Assert.Contains(result.Items, i => i.Title == "User Item 1");
            Assert.Contains(result.Items, i => i.Title == "User Item 2");
            _mockItemRepository.Verify(x => x.GetItemsByUserIdAsync(userId), Times.Once);
        }
    }

    public class UpdateItemMessageHandlerTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<ILogger<UpdateItemMessageHandler>> _mockLogger;
        private readonly UpdateItemMessageHandler _handler;

        public UpdateItemMessageHandlerTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockLogger = new Mock<ILogger<UpdateItemMessageHandler>>();
            _handler = new UpdateItemMessageHandler(_mockItemRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ExistingItem_UpdatesItemSuccessfully()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new SharedLibreries.Contracts.UpdateItemRequest
            {
                ItemId = itemId,
                Title = "Updated Item",
                Description = "Updated Description",
                IsCompleted = true
            };

            var existingItem = new Item
            {
                Id = itemId,
                UserId = userId,
                Title = "Original Item",
                Description = "Original Description",
                IsCompleted = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(existingItem);

            _mockItemRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Item>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Item);
            Assert.Equal(itemId, result.Item.Id);
            Assert.Equal(request.Title, result.Item.Title);
            Assert.Equal(request.Description, result.Item.Description);
            Assert.Equal(request.IsCompleted, result.Item.IsCompleted);
            _mockItemRepository.Verify(x => x.GetByIdAsync(itemId), Times.Once);
            _mockItemRepository.Verify(x => x.UpdateAsync(It.Is<Item>(i => i.Title == request.Title && i.Description == request.Description && i.IsCompleted == request.IsCompleted)), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_NonExistingItem_ReturnsFailure()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var request = new SharedLibreries.Contracts.UpdateItemRequest
            {
                ItemId = itemId,
                Title = "Updated Item",
                Description = "Updated Description",
                IsCompleted = true
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync((Item?)null);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.ErrorMessage);
            _mockItemRepository.Verify(x => x.GetByIdAsync(itemId), Times.Once);
            _mockItemRepository.Verify(x => x.UpdateAsync(It.IsAny<Item>()), Times.Never);
        }
    }

    public class DeleteItemMessageHandlerTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<ILogger<DeleteItemMessageHandler>> _mockLogger;
        private readonly DeleteItemMessageHandler _handler;

        public DeleteItemMessageHandlerTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockLogger = new Mock<ILogger<DeleteItemMessageHandler>>();
            _handler = new DeleteItemMessageHandler(_mockItemRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ExistingItem_SoftDeletesItemSuccessfully()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var request = new DeleteItemRequest
            {
                ItemId = itemId
            };

            var existingItem = new Item
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

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(existingItem);

            _mockItemRepository
                .Setup(x => x.SoftDeleteAsync(itemId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            _mockItemRepository.Verify(x => x.GetByIdAsync(itemId), Times.Once);
            _mockItemRepository.Verify(x => x.SoftDeleteAsync(itemId), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_NonExistingItem_ReturnsFailure()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var request = new DeleteItemRequest
            {
                ItemId = itemId
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync((Item?)null);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.ErrorMessage);
            _mockItemRepository.Verify(x => x.GetByIdAsync(itemId), Times.Once);
            _mockItemRepository.Verify(x => x.SoftDeleteAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
