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
    public class ItemsController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(IItemService itemService, ILogger<ItemsController> logger)
        {
            _itemService = itemService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ItemResponse>> CreateItem([FromBody] SharedLibreries.DTOs.CreateItemRequest request)
        {
            try
            {
                var result = await _itemService.CreateItemAsync(request);
                if (result.IsSuccess && result.ItemId.HasValue)
                {
                    var itemResponse = new ItemResponse
                    {
                        Id = result.ItemId.Value,
                        UserId = result.UserId ?? request.UserId,
                        Title = result.Title ?? request.Title,
                        Description = request.Description,
                        IsCompleted = false,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    return CreatedAtAction(nameof(GetItem), new { id = itemResponse.Id }, itemResponse);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemResponse>>> GetAllItems()
        {
            try
            {
                var result = await _itemService.GetAllItemsAsync();
                if (result.IsSuccess)
                {
                    return Ok(result.Items);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all items");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemResponse>> GetItem(Guid id)
        {
            try
            {
                var result = await _itemService.GetItemAsync(id);
                if (result.IsSuccess && result.Item != null)
                {
                    return Ok(result.Item);
                }
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result.ErrorMessage);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item {ItemId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ItemResponse>>> GetUserItems(Guid userId)
        {
            try
            {
                var result = await _itemService.GetUserItemsAsync(userId);
                if (result.IsSuccess)
                {
                    return Ok(result.Items);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ItemResponse>> UpdateItem(Guid id, [FromBody] SharedLibreries.DTOs.UpdateItemRequest request)
        {
            try
            {
                var result = await _itemService.UpdateItemAsync(id, request);
                if (result.IsSuccess && result.Item != null)
                {
                    return Ok(result.Item);
                }
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result.ErrorMessage);
                }
                return BadRequest(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteItem(Guid id)
        {
            try
            {
                var result = await _itemService.DeleteItemAsync(id);
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
                _logger.LogError(ex, "Error deleting item {ItemId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
