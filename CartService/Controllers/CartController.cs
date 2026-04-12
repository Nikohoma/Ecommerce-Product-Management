using CartService.DTOs;
using CartService.Exceptions;
using CartService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CartService.Services.Messaging;


namespace CartService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;
        private readonly IOrderPublisher _publish;

        public CartController(ICartService cartService, ILogger<CartController> logger, IOrderPublisher publish)
        {
            _cartService = cartService;
            _logger = logger;
            _publish = publish;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = GetUserId();
                var cart = await _cartService.GetCartAsync(userId);
                return Ok(cart);
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (CartException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetCart");
                return StatusCode(500, "An unexpected error occurred."); 
            }
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddItemRequest request)
        {
            try
            {
                var userId = GetUserId();
                var item = await _cartService.AddItemAsync(userId, request.ProductId, request.Quantity);
                return Ok(item);
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidQuantityException ex) { return BadRequest(ex.Message); }  
            catch (CartException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AddItem");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut("items/{itemId:int}")]
        public async Task<IActionResult> UpdateItemQuantity(int itemId, [FromBody] UpdateQuantityRequest request)
        {
            try
            {
                var userId = GetUserId();
                var item = await _cartService.UpdateItemQuantityAsync(userId, itemId, request.Quantity);
                return Ok(item);
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (CartItemNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidQuantityException ex) { return BadRequest(ex.Message); }
            catch (CartException ex) { return BadRequest(ex.Message); } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateItemQuantity for item {ItemId}", itemId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("items/{itemId:int}")]
        public async Task<IActionResult> RemoveItem(int itemId)
        {
            try
            {
                var userId = GetUserId();
                await _cartService.RemoveItemAsync(userId, itemId);
                return NoContent();
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (CartItemNotFoundException ex) { return NotFound(ex.Message); }
            catch (CartException ex) { return BadRequest(ex.Message); }  // fix: was missing
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in RemoveItem for item {ItemId}", itemId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("items")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetUserId();
                await _cartService.ClearCartAsync(userId);
                return NoContent();
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (CartException ex) { return BadRequest(ex.Message); } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ClearCart");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }


        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            _logger.LogInformation("Checkout requested for user {UserId}", userId);

            CartCheckoutDto snapshot;

            try
            {
                snapshot = await _cartService.CheckoutAsync(userId);
            }
            catch (CartNotFoundException ex) { return NotFound(ex.Message); }
            catch (CartException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout failed for user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }

            // cart is now CheckedOut in DB — attempt to publish
            try
            {
                await _publish.SendCartToOrderAsync(snapshot);
            }
            catch (Exception ex)
            {
                // critical: cart is CheckedOut but order event faile
                _logger.LogCritical(ex,
                    "Cart {CartId} is CheckedOut but publish to Order Service FAILED. Manual intervention required.",
                    snapshot.CartId);

                // cart IS checked out, just not yet processed
                return StatusCode(202, new
                {
                    message = "Order is being processed. You will be notified shortly.",
                    cartId = snapshot.CartId
                });
            }

            return Accepted(new
            {
                message = "Order received and is being processed.",
                cartId = snapshot.CartId
            });
            // 202 Accepted — not 200 OK — because order is not yet confirmed, just queued
        }



        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)?? throw new UnauthorizedAccessException("User identity not found in token.");
        }
    }

    public record AddItemRequest(int ProductId, int Quantity);
    public record UpdateQuantityRequest(int Quantity);
}