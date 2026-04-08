using CartService.Exceptions;
using CartService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CartService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await _cartService.GetCartAsync(userId);
                return Ok(cart);
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (CartException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddItemRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var item = await _cartService.AddItemAsync(userId, request.ProductId, request.Quantity);
                return Ok(item);
            }
            catch (CartException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPut("items/{itemId:int}")]
        public async Task<IActionResult> UpdateItemQuantity(int itemId, [FromBody] UpdateQuantityRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var item = await _cartService.UpdateItemQuantityAsync(userId, itemId, request.Quantity);
                return Ok(item);
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (CartItemNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidQuantityException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("items/{itemId:int}")]
        public async Task<IActionResult> RemoveItem(int itemId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _cartService.RemoveItemAsync(userId, itemId);
                return NoContent();
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (CartItemNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("items")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _cartService.ClearCartAsync(userId);
                return NoContent();
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }
    }

    public record AddItemRequest(int ProductId, int Quantity);
    public record UpdateQuantityRequest(int Quantity);
}