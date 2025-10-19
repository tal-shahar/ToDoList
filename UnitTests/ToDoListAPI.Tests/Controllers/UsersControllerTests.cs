using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using ToDoListAPI.Controllers;
using ToDoListAPI.Services;

namespace ToDoListAPI.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _controller = new UsersController(_mockUserService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateUser_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateUserRequest
            {
                Name = "John Doe",
                Email = "john.doe@example.com"
            };

            var serviceResponse = new CreateUserResponse
            {
                IsSuccess = true,
                UserId = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email
            };

            var userResponse = new UserResponse
            {
                Id = serviceResponse.UserId.Value,
                Name = request.Name,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserService
                .Setup(x => x.CreateUserAsync(request))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(_controller.GetUser), createdResult.ActionName);
            Assert.Equal(userResponse.Id, ((UserResponse)createdResult.Value!).Id);
            _mockUserService.Verify(x => x.CreateUserAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateUserRequest
            {
                Name = "John Doe",
                Email = "john.doe@example.com"
            };

            var serviceResponse = new CreateUserResponse
            {
                IsSuccess = false,
                ErrorMessage = "User already exists"
            };

            _mockUserService
                .Setup(x => x.CreateUserAsync(request))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(serviceResponse.ErrorMessage, badRequestResult.Value);
            _mockUserService.Verify(x => x.CreateUserAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new SharedLibreries.DTOs.CreateUserRequest
            {
                Name = "John Doe",
                Email = "john.doe@example.com"
            };

            _mockUserService
                .Setup(x => x.CreateUserAsync(request))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Internal server error", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetAllUsers_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var serviceResponse = new GetAllUsersResponse
            {
                IsSuccess = true,
                Users = new List<UserResponse>
                {
                    new UserResponse { Id = Guid.NewGuid(), Name = "John Doe", Email = "john@example.com" },
                    new UserResponse { Id = Guid.NewGuid(), Name = "Jane Doe", Email = "jane@example.com" }
                }
            };

            _mockUserService
                .Setup(x => x.GetAllUsersAsync())
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var users = Assert.IsAssignableFrom<IEnumerable<UserResponse>>(okResult.Value);
            Assert.Equal(2, users.Count());
            _mockUserService.Verify(x => x.GetAllUsersAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUser_ValidId_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userResponse = new UserResponse
            {
                Id = userId,
                Name = "John Doe",
                Email = "john.doe@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var serviceResponse = new GetUserResponse
            {
                IsSuccess = true,
                User = userResponse
            };

            _mockUserService
                .Setup(x => x.GetUserAsync(userId))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserResponse>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);
            _mockUserService.Verify(x => x.GetUserAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUser_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var serviceResponse = new GetUserResponse
            {
                IsSuccess = false,
                ErrorMessage = "User not found"
            };

            _mockUserService
                .Setup(x => x.GetUserAsync(userId))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(serviceResponse.ErrorMessage, notFoundResult.Value);
            _mockUserService.Verify(x => x.GetUserAsync(userId), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new SharedLibreries.DTOs.UpdateUserRequest
            {
                Name = "Jane Doe",
                Email = "jane.doe@example.com"
            };

            var userResponse = new UserResponse
            {
                Id = userId,
                Name = request.Name,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var serviceResponse = new UpdateUserResponse
            {
                IsSuccess = true,
                User = userResponse
            };

            _mockUserService
                .Setup(x => x.UpdateUserAsync(userId, request))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.UpdateUser(userId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserResponse>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);
            Assert.Equal(request.Name, returnedUser.Name);
            Assert.Equal(request.Email, returnedUser.Email);
            _mockUserService.Verify(x => x.UpdateUserAsync(userId, request), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_ValidId_ReturnsNoContent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var serviceResponse = new DeleteUserResponse
            {
                IsSuccess = true
            };

            _mockUserService
                .Setup(x => x.DeleteUserAsync(userId))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockUserService.Verify(x => x.DeleteUserAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var serviceResponse = new DeleteUserResponse
            {
                IsSuccess = false,
                ErrorMessage = "User not found"
            };

            _mockUserService
                .Setup(x => x.DeleteUserAsync(userId))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(serviceResponse.ErrorMessage, notFoundResult.Value);
            _mockUserService.Verify(x => x.DeleteUserAsync(userId), Times.Once);
        }
    }
}
