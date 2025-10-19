using Microsoft.Extensions.Logging;
using Moq;
using SharedLibreries.Constants;
using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using SharedLibreries.RabbitMQ;
using ToDoListAPI.Services;

namespace ToDoListAPI.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IRabbitMqService> _mockRabbitMqService;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockRabbitMqService = new Mock<IRabbitMqService>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _userService = new UserService(_mockRabbitMqService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateUserAsync_ValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateUserRequest
            {
                Name = "John Doe",
                Email = "john.doe@example.com"
            };

            var expectedResponse = new CreateUserResponse
            {
                IsSuccess = true,
                UserId = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.CreateUserRequest, CreateUserResponse>(
                    It.IsAny<SharedLibreries.Contracts.CreateUserRequest>(),
                    QueueNames.UserQueue,
                    OperationTypes.CreateUser))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.CreateUserAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedResponse.UserId, result.UserId);
            Assert.Equal(expectedResponse.Name, result.Name);
            Assert.Equal(expectedResponse.Email, result.Email);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.CreateUserRequest, CreateUserResponse>(
                It.Is<SharedLibreries.Contracts.CreateUserRequest>(r => r.Name == request.Name && r.Email == request.Email),
                QueueNames.UserQueue,
                OperationTypes.CreateUser), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_RabbitMqThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateUserRequest
            {
                Name = "John Doe",
                Email = "john.doe@example.com"
            };

            var exception = new Exception("RabbitMQ connection failed");
            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.CreateUserRequest, CreateUserResponse>(
                    It.IsAny<SharedLibreries.Contracts.CreateUserRequest>(),
                    QueueNames.UserQueue,
                    OperationTypes.CreateUser))
                .ThrowsAsync(exception);

            // Act
            var result = await _userService.CreateUserAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(exception.Message, result.ErrorMessage);
        }

        [Fact]
        public async Task GetUserAsync_ValidUserId_ReturnsSuccessResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResponse = new GetUserResponse
            {
                IsSuccess = true,
                User = new UserResponse
                {
                    Id = userId,
                    Name = "John Doe",
                    Email = "john.doe@example.com",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetUserRequest, GetUserResponse>(
                    It.IsAny<SharedLibreries.Contracts.GetUserRequest>(),
                    QueueNames.UserQueue,
                    OperationTypes.GetUser))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.GetUserAsync(userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.User);
            Assert.Equal(userId, result.User.Id);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetUserRequest, GetUserResponse>(
                It.Is<SharedLibreries.Contracts.GetUserRequest>(r => r.UserId == userId),
                QueueNames.UserQueue,
                OperationTypes.GetUser), Times.Once);
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsSuccessResponse()
        {
            // Arrange
            var expectedResponse = new GetAllUsersResponse
            {
                IsSuccess = true,
                Users = new List<UserResponse>
                {
                    new UserResponse { Id = Guid.NewGuid(), Name = "John Doe", Email = "john@example.com" },
                    new UserResponse { Id = Guid.NewGuid(), Name = "Jane Doe", Email = "jane@example.com" }
                }
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetAllUsersRequest, GetAllUsersResponse>(
                    It.IsAny<SharedLibreries.Contracts.GetAllUsersRequest>(),
                    QueueNames.UserQueue,
                    OperationTypes.GetAllUsers))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Users);
            Assert.Equal(2, result.Users.Count);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.GetAllUsersRequest, GetAllUsersResponse>(
                It.IsAny<SharedLibreries.Contracts.GetAllUsersRequest>(),
                QueueNames.UserQueue,
                OperationTypes.GetAllUsers), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_ValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new SharedLibreries.DTOs.UpdateUserRequest
            {
                Name = "Jane Doe",
                Email = "jane.doe@example.com"
            };

            var expectedResponse = new UpdateUserResponse
            {
                IsSuccess = true,
                User = new UserResponse
                {
                    Id = userId,
                    Name = request.Name,
                    Email = request.Email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.UpdateUserRequest, UpdateUserResponse>(
                    It.IsAny<SharedLibreries.Contracts.UpdateUserRequest>(),
                    QueueNames.UserQueue,
                    OperationTypes.UpdateUser))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.UpdateUserAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.User);
            Assert.Equal(userId, result.User.Id);
            Assert.Equal(request.Name, result.User.Name);
            Assert.Equal(request.Email, result.User.Email);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.UpdateUserRequest, UpdateUserResponse>(
                It.Is<SharedLibreries.Contracts.UpdateUserRequest>(r => r.UserId == userId && r.Name == request.Name && r.Email == request.Email),
                QueueNames.UserQueue,
                OperationTypes.UpdateUser), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_ValidUserId_ReturnsSuccessResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResponse = new DeleteUserResponse
            {
                IsSuccess = true
            };

            _mockRabbitMqService
                .Setup(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.DeleteUserRequest, DeleteUserResponse>(
                    It.IsAny<SharedLibreries.Contracts.DeleteUserRequest>(),
                    QueueNames.UserQueue,
                    OperationTypes.DeleteUser))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRabbitMqService.Verify(x => x.SendRpcRequestAsync<SharedLibreries.Contracts.DeleteUserRequest, DeleteUserResponse>(
                It.Is<SharedLibreries.Contracts.DeleteUserRequest>(r => r.UserId == userId),
                QueueNames.UserQueue,
                OperationTypes.DeleteUser), Times.Once);
        }
    }
}
