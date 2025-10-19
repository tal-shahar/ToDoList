using Microsoft.Extensions.Logging;
using Moq;
using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using SharedLibreries.Models;
using WorkerServices.WorkerUser.Handlers;
using WorkerServices.WorkerUser.Repositories;

namespace WorkerUser.Tests.Handlers
{
    public class CreateUserMessageHandlerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<CreateUserMessageHandler>> _mockLogger;
        private readonly CreateUserMessageHandler _handler;

        public CreateUserMessageHandlerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<CreateUserMessageHandler>>();
            _handler = new CreateUserMessageHandler(_mockUserRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_CreatesUserSuccessfully()
        {
            // Arrange
            var request = new SharedLibreries.Contracts.CreateUserRequest
            {
                Name = "John Doe",
                Email = "john.doe@example.com"
            };

            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.UserId);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Email, result.Email);
            _mockUserRepository.Verify(x => x.GetByEmailAsync(request.Email), Times.Once);
            _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => u.Name == request.Name && u.Email == request.Email)), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_UserAlreadyExists_ReturnsFailure()
        {
            // Arrange
            var request = new SharedLibreries.Contracts.CreateUserRequest
            {
                Name = "John Doe",
                Email = "john.doe@example.com"
            };

            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "Existing User",
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("already exists", result.ErrorMessage);
            _mockUserRepository.Verify(x => x.GetByEmailAsync(request.Email), Times.Once);
            _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_RepositoryThrowsException_ReturnsFailure()
        {
            // Arrange
            var request = new SharedLibreries.Contracts.CreateUserRequest
            {
                Name = "John Doe",
                Email = "john.doe@example.com"
            };

            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(request.Email))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Database error", result.ErrorMessage);
        }
    }

    public class GetUserMessageHandlerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<GetUserMessageHandler>> _mockLogger;
        private readonly GetUserMessageHandler _handler;

        public GetUserMessageHandlerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<GetUserMessageHandler>>();
            _handler = new GetUserMessageHandler(_mockUserRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new GetUserRequest
            {
                UserId = userId
            };

            var user = new User
            {
                Id = userId,
                Name = "John Doe",
                Email = "john.doe@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.User);
            Assert.Equal(userId, result.User.Id);
            Assert.Equal(user.Name, result.User.Name);
            Assert.Equal(user.Email, result.User.Email);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_NonExistingUser_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new GetUserRequest
            {
                UserId = userId
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.ErrorMessage);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }
    }

    public class GetAllUsersMessageHandlerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<GetAllUsersMessageHandler>> _mockLogger;
        private readonly GetAllUsersMessageHandler _handler;

        public GetAllUsersMessageHandlerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<GetAllUsersMessageHandler>>();
            _handler = new GetAllUsersMessageHandler(_mockUserRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ReturnsAllUsers()
        {
            // Arrange
            var request = new GetAllUsersRequest();

            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new User { Id = Guid.NewGuid(), Name = "Jane Doe", Email = "jane@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(users);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Users.Count);
            Assert.Contains(result.Users, u => u.Name == "John Doe");
            Assert.Contains(result.Users, u => u.Name == "Jane Doe");
            _mockUserRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }
    }

    public class UpdateUserMessageHandlerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<UpdateUserMessageHandler>> _mockLogger;
        private readonly UpdateUserMessageHandler _handler;

        public UpdateUserMessageHandlerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UpdateUserMessageHandler>>();
            _handler = new UpdateUserMessageHandler(_mockUserRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ExistingUser_UpdatesUserSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new SharedLibreries.Contracts.UpdateUserRequest
            {
                UserId = userId,
                Name = "Jane Doe",
                Email = "jane.doe@example.com"
            };

            var existingUser = new User
            {
                Id = userId,
                Name = "John Doe",
                Email = "john.doe@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _mockUserRepository
                .Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.User);
            Assert.Equal(userId, result.User.Id);
            Assert.Equal(request.Name, result.User.Name);
            Assert.Equal(request.Email, result.User.Email);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Name == request.Name && u.Email == request.Email)), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_NonExistingUser_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new SharedLibreries.Contracts.UpdateUserRequest
            {
                UserId = userId,
                Name = "Jane Doe",
                Email = "jane.doe@example.com"
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.ErrorMessage);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        }
    }

    public class DeleteUserMessageHandlerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<DeleteUserMessageHandler>> _mockLogger;
        private readonly DeleteUserMessageHandler _handler;

        public DeleteUserMessageHandlerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<DeleteUserMessageHandler>>();
            _handler = new DeleteUserMessageHandler(_mockUserRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_ExistingUser_DeletesUserSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new DeleteUserRequest
            {
                UserId = userId
            };

            var existingUser = new User
            {
                Id = userId,
                Name = "John Doe",
                Email = "john.doe@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _mockUserRepository
                .Setup(x => x.DeleteAsync(userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockUserRepository.Verify(x => x.DeleteAsync(userId), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_NonExistingUser_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new DeleteUserRequest
            {
                UserId = userId
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.ErrorMessage);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockUserRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
