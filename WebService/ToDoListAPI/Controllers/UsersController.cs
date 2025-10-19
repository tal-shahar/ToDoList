using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SharedLibreries.Constants;
using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using SharedLibreries.RabbitMQ;
using ToDoListAPI.Services;

namespace ToDoListAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<UserResponse>> CreateUser([FromBody] SharedLibreries.DTOs.CreateUserRequest request)
        {
            try
            {
                var result = await _userService.CreateUserAsync(request);
                if (result.IsSuccess && result.UserId.HasValue)
                {
                    var userResponse = new UserResponse
                    {
                        Id = result.UserId.Value,
                        Name = result.Name ?? request.Name,
                        Email = result.Email ?? request.Email,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    return CreatedAtAction(nameof(GetUser), new { id = userResponse.Id }, userResponse);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetAllUsers()
        {
            try
            {
                var result = await _userService.GetAllUsersAsync();
                if (result.IsSuccess)
                {
                    return Ok(result.Users);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponse>> GetUser(Guid id)
        {
            try
            {
                var result = await _userService.GetUserAsync(id);
                if (result.IsSuccess && result.User != null)
                {
                    return Ok(result.User);
                }
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result.ErrorMessage);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserResponse>> UpdateUser(Guid id, [FromBody] SharedLibreries.DTOs.UpdateUserRequest request)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(id, request);
                if (result.IsSuccess && result.User != null)
                {
                    return Ok(result.User);
                }
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result.ErrorMessage);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(Guid id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                if (result.IsSuccess)
                {
                    return NoContent();
                }
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result.ErrorMessage);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
