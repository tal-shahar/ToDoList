using SharedLibreries.Models;
using SharedLibreries.DTOs;
using SharedLibreries.Contracts;
using SharedLibreries.Constants;

namespace SharedLibreries.Tests.Models
{
    public class UserTests
    {
        [Fact]
        public void User_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var user = new User();

            // Assert
            Assert.Equal(Guid.Empty, user.Id);
            Assert.Equal(string.Empty, user.Name);
            Assert.Equal(string.Empty, user.Email);
            Assert.True(user.CreatedAt <= DateTime.UtcNow);
            Assert.True(user.UpdatedAt <= DateTime.UtcNow);
            Assert.NotNull(user.Items);
            Assert.Empty(user.Items);
        }

        [Fact]
        public void User_Properties_ShouldBeSettable()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var name = "John Doe";
            var email = "john.doe@example.com";
            var createdAt = DateTime.UtcNow.AddDays(-1);
            var updatedAt = DateTime.UtcNow;

            // Act
            var user = new User
            {
                Id = userId,
                Name = name,
                Email = email,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Assert
            Assert.Equal(userId, user.Id);
            Assert.Equal(name, user.Name);
            Assert.Equal(email, user.Email);
            Assert.Equal(createdAt, user.CreatedAt);
            Assert.Equal(updatedAt, user.UpdatedAt);
        }

        [Fact]
        public void User_ItemsCollection_ShouldBeInitialized()
        {
            // Arrange & Act
            var user = new User();

            // Assert
            Assert.NotNull(user.Items);
            Assert.IsAssignableFrom<ICollection<Item>>(user.Items);
        }
    }

    public class ItemTests
    {
        [Fact]
        public void Item_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var item = new Item();

            // Assert
            Assert.Equal(Guid.Empty, item.Id);
            Assert.Equal(Guid.Empty, item.UserId);
            Assert.Equal(string.Empty, item.Title);
            Assert.Null(item.Description);
            Assert.False(item.IsCompleted);
            Assert.False(item.IsDeleted);
            Assert.True(item.CreatedAt <= DateTime.UtcNow);
            Assert.True(item.UpdatedAt <= DateTime.UtcNow);
            Assert.Null(item.DeletedAt);
        }

        [Fact]
        public void Item_Properties_ShouldBeSettable()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var title = "Test Item";
            var description = "Test Description";
            var isCompleted = true;
            var isDeleted = true;
            var createdAt = DateTime.UtcNow.AddDays(-1);
            var updatedAt = DateTime.UtcNow;
            var deletedAt = DateTime.UtcNow;

            // Act
            var item = new Item
            {
                Id = itemId,
                UserId = userId,
                Title = title,
                Description = description,
                IsCompleted = isCompleted,
                IsDeleted = isDeleted,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                DeletedAt = deletedAt
            };

            // Assert
            Assert.Equal(itemId, item.Id);
            Assert.Equal(userId, item.UserId);
            Assert.Equal(title, item.Title);
            Assert.Equal(description, item.Description);
            Assert.Equal(isCompleted, item.IsCompleted);
            Assert.Equal(isDeleted, item.IsDeleted);
            Assert.Equal(createdAt, item.CreatedAt);
            Assert.Equal(updatedAt, item.UpdatedAt);
            Assert.Equal(deletedAt, item.DeletedAt);
        }
    }
}
