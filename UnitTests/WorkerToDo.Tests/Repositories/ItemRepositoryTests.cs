using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibreries.Models;
using WorkerServices.WorkerToDo.Data;
using WorkerServices.WorkerToDo.Repositories;

namespace WorkerToDo.Tests.Repositories
{
    public class ItemRepositoryTests : IDisposable
    {
        private readonly ToDoDbContext _context;
        private readonly ItemRepository _repository;

        public ItemRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ToDoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ToDoDbContext(options);
            _repository = new ItemRepository(_context);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingItem_ReturnsItem()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var item = new Item
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Test Item",
                Description = "Test Description",
                IsCompleted = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(item.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(item.Id, result.Id);
            Assert.Equal(item.Title, result.Title);
            Assert.Equal(item.Description, result.Description);
            Assert.Equal(item.UserId, result.UserId);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingItem_ReturnsNull()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(nonExistingId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllItems()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var items = new List<Item>
            {
                new Item { Id = Guid.NewGuid(), UserId = userId, Title = "Item 1", Description = "Description 1", IsCompleted = false, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Item { Id = Guid.NewGuid(), UserId = userId, Title = "Item 2", Description = "Description 2", IsCompleted = true, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            _context.Items.AddRange(items);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, i => i.Title == "Item 1");
            Assert.Contains(result, i => i.Title == "Item 2");
        }

        [Fact]
        public async Task GetItemsByUserIdAsync_ValidUserId_ReturnsUserItems()
        {
            // Arrange
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            var items = new List<Item>
            {
                new Item { Id = Guid.NewGuid(), UserId = userId1, Title = "User 1 Item 1", Description = "Description 1", IsCompleted = false, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Item { Id = Guid.NewGuid(), UserId = userId1, Title = "User 1 Item 2", Description = "Description 2", IsCompleted = true, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Item { Id = Guid.NewGuid(), UserId = userId2, Title = "User 2 Item 1", Description = "Description 3", IsCompleted = false, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            _context.Items.AddRange(items);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetItemsByUserIdAsync(userId1);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, item => Assert.Equal(userId1, item.UserId));
            Assert.Contains(result, i => i.Title == "User 1 Item 1");
            Assert.Contains(result, i => i.Title == "User 1 Item 2");
        }

        [Fact]
        public async Task AddAsync_ValidItem_AddsItemToDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var item = new Item
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Test Item",
                Description = "Test Description",
                IsCompleted = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            await _repository.AddAsync(item);

            // Assert
            var savedItem = await _context.Items.FindAsync(item.Id);
            Assert.NotNull(savedItem);
            Assert.Equal(item.Title, savedItem.Title);
            Assert.Equal(item.Description, savedItem.Description);
            Assert.Equal(item.UserId, savedItem.UserId);
        }

        [Fact]
        public async Task UpdateAsync_ExistingItem_UpdatesItemInDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var item = new Item
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Test Item",
                Description = "Test Description",
                IsCompleted = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Act
            item.Title = "Updated Item";
            item.Description = "Updated Description";
            item.IsCompleted = true;
            await _repository.UpdateAsync(item);

            // Assert
            var updatedItem = await _context.Items.FindAsync(item.Id);
            Assert.NotNull(updatedItem);
            Assert.Equal("Updated Item", updatedItem.Title);
            Assert.Equal("Updated Description", updatedItem.Description);
            Assert.True(updatedItem.IsCompleted);
        }

        [Fact]
        public async Task DeleteAsync_ExistingItem_RemovesItemFromDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var item = new Item
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Test Item",
                Description = "Test Description",
                IsCompleted = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(item.Id);

            // Assert
            var deletedItem = await _context.Items.FindAsync(item.Id);
            Assert.Null(deletedItem);
        }

        [Fact]
        public async Task SoftDeleteAsync_ExistingItem_MarksItemAsDeleted()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var item = new Item
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Test Item",
                Description = "Test Description",
                IsCompleted = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Act
            await _repository.SoftDeleteAsync(item.Id);

            // Assert
            var softDeletedItem = await _context.Items.FindAsync(item.Id);
            Assert.NotNull(softDeletedItem);
            Assert.True(softDeletedItem.IsDeleted);
            Assert.NotNull(softDeletedItem.DeletedAt);
        }

        [Fact]
        public async Task SoftDeleteAsync_NonExistingItem_DoesNotThrow()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act & Assert
            await _repository.SoftDeleteAsync(nonExistingId);
            // Should not throw exception
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
