using SharedLibreries.DTOs;

namespace SharedLibreries.Tests.DTOs
{
    public class UserDtoTests
    {
        [Fact]
        public void CreateUserRequest_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var request = new CreateUserRequest();

            // Assert
            Assert.Equal(string.Empty, request.Name);
            Assert.Equal(string.Empty, request.Email);
        }

        [Fact]
        public void CreateUserRequest_Properties_ShouldBeSettable()
        {
            // Arrange
            var name = "John Doe";
            var email = "john.doe@example.com";

            // Act
            var request = new CreateUserRequest
            {
                Name = name,
                Email = email
            };

            // Assert
            Assert.Equal(name, request.Name);
            Assert.Equal(email, request.Email);
        }

        [Fact]
        public void UpdateUserRequest_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var request = new UpdateUserRequest();

            // Assert
            Assert.Equal(string.Empty, request.Name);
            Assert.Equal(string.Empty, request.Email);
        }

        [Fact]
        public void UpdateUserRequest_Properties_ShouldBeSettable()
        {
            // Arrange
            var name = "Jane Doe";
            var email = "jane.doe@example.com";

            // Act
            var request = new UpdateUserRequest
            {
                Name = name,
                Email = email
            };

            // Assert
            Assert.Equal(name, request.Name);
            Assert.Equal(email, request.Email);
        }

        [Fact]
        public void UserResponse_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var response = new UserResponse();

            // Assert
            Assert.Equal(Guid.Empty, response.Id);
            Assert.Equal(string.Empty, response.Name);
            Assert.Equal(string.Empty, response.Email);
            Assert.Equal(DateTime.MinValue, response.CreatedAt);
            Assert.Equal(DateTime.MinValue, response.UpdatedAt);
        }

        [Fact]
        public void UserResponse_Properties_ShouldBeSettable()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = "John Doe";
            var email = "john.doe@example.com";
            var createdAt = DateTime.UtcNow.AddDays(-1);
            var updatedAt = DateTime.UtcNow;

            // Act
            var response = new UserResponse
            {
                Id = id,
                Name = name,
                Email = email,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Assert
            Assert.Equal(id, response.Id);
            Assert.Equal(name, response.Name);
            Assert.Equal(email, response.Email);
            Assert.Equal(createdAt, response.CreatedAt);
            Assert.Equal(updatedAt, response.UpdatedAt);
        }
    }

    public class ItemDtoTests
    {
        [Fact]
        public void CreateItemRequest_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var request = new CreateItemRequest();

            // Assert
            Assert.Equal(Guid.Empty, request.UserId);
            Assert.Equal(string.Empty, request.Title);
            Assert.Null(request.Description);
        }

        [Fact]
        public void CreateItemRequest_Properties_ShouldBeSettable()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var title = "Test Item";
            var description = "Test Description";

            // Act
            var request = new CreateItemRequest
            {
                UserId = userId,
                Title = title,
                Description = description
            };

            // Assert
            Assert.Equal(userId, request.UserId);
            Assert.Equal(title, request.Title);
            Assert.Equal(description, request.Description);
        }

        [Fact]
        public void UpdateItemRequest_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var request = new UpdateItemRequest();

            // Assert
            Assert.Equal(string.Empty, request.Title);
            Assert.Null(request.Description);
            Assert.False(request.IsCompleted);
        }

        [Fact]
        public void UpdateItemRequest_Properties_ShouldBeSettable()
        {
            // Arrange
            var title = "Updated Item";
            var description = "Updated Description";
            var isCompleted = true;

            // Act
            var request = new UpdateItemRequest
            {
                Title = title,
                Description = description,
                IsCompleted = isCompleted
            };

            // Assert
            Assert.Equal(title, request.Title);
            Assert.Equal(description, request.Description);
            Assert.Equal(isCompleted, request.IsCompleted);
        }

        [Fact]
        public void ItemResponse_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var response = new ItemResponse();

            // Assert
            Assert.Equal(Guid.Empty, response.Id);
            Assert.Equal(Guid.Empty, response.UserId);
            Assert.Equal(string.Empty, response.Title);
            Assert.Null(response.Description);
            Assert.False(response.IsCompleted);
            Assert.False(response.IsDeleted);
            Assert.Equal(DateTime.MinValue, response.CreatedAt);
            Assert.Equal(DateTime.MinValue, response.UpdatedAt);
            Assert.Null(response.DeletedAt);
        }

        [Fact]
        public void ItemResponse_Properties_ShouldBeSettable()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var title = "Test Item";
            var description = "Test Description";
            var isCompleted = true;
            var isDeleted = true;
            var createdAt = DateTime.UtcNow.AddDays(-1);
            var updatedAt = DateTime.UtcNow;
            var deletedAt = DateTime.UtcNow;

            // Act
            var response = new ItemResponse
            {
                Id = id,
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
            Assert.Equal(id, response.Id);
            Assert.Equal(userId, response.UserId);
            Assert.Equal(title, response.Title);
            Assert.Equal(description, response.Description);
            Assert.Equal(isCompleted, response.IsCompleted);
            Assert.Equal(isDeleted, response.IsDeleted);
            Assert.Equal(createdAt, response.CreatedAt);
            Assert.Equal(updatedAt, response.UpdatedAt);
            Assert.Equal(deletedAt, response.DeletedAt);
        }
    }
}
