using CartService.Data;
using CartService.Models;
using Microsoft.EntityFrameworkCore;

namespace CartService.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly CartDbContext _context;
        private readonly ILogger<CartRepository> _logger;

        public CartRepository(CartDbContext context, ILogger<CartRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CartService.Models.Cart?> GetByUserIdAsync(string userId)
        {
            _logger.LogInformation("Fetching cart for user {UserId}", userId);
            return await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active);
            // only return Active cart — CheckedOut/Abandoned carts are not the "current" cart
        }

        public async Task<CartService.Models.Cart?> GetByIdAsync(int cartId)
        {
            _logger.LogInformation("Fetching cart {CartId}", cartId);
            return await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartId);
        }

        public async Task<CartService.Models.Cart> CreateAsync(CartService.Models.Cart cart)
        {
            _logger.LogInformation("Creating new cart for user {UserId}", cart.UserId);
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cart {CartId} created", cart.Id);
            return cart;
        }

        public async Task<CartItem> AddItemAsync(CartItem item)
        {
            _logger.LogInformation("Adding product {ProductId} to cart {CartId}", item.ProductId, item.CartId);
            _context.CartItems.Add(item);

            // update LastActivityAt on every item change
            await UpdateLastActivityAsync(item.CartId);

            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<CartItem> UpdateItemAsync(CartItem item)
        {
            _logger.LogInformation("Updating item {ItemId} in cart {CartId}", item.Id, item.CartId);
            _context.CartItems.Update(item);

            await UpdateLastActivityAsync(item.CartId);

            await _context.SaveChangesAsync();
            return item;
        }

        public async Task RemoveItemAsync(int itemId)
        {
            _logger.LogInformation("Removing item {ItemId}", itemId);
            var item = await _context.CartItems.FindAsync(itemId);
            if (item is null) return;

            _context.CartItems.Remove(item);

            await UpdateLastActivityAsync(item.CartId);

            await _context.SaveChangesAsync();
        }

        public async Task<CartService.Models.Cart> CheckoutAsync(int cartId)
        {
            _logger.LogInformation("Checkout initiated for cart {CartId}", cartId);

            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId);

            if (cart is null)
            {
                _logger.LogWarning("Checkout failed — cart {CartId} not found", cartId);
                throw new KeyNotFoundException($"Cart {cartId} not found.");
            }

            if (cart.Status != CartStatus.Active)
            {
                _logger.LogWarning("Checkout failed — cart {CartId} is {Status}", cartId, cart.Status);
                throw new InvalidOperationException($"Cart is {cart.Status}. Only Active carts can be checked out.");
            }

            if (!cart.Items.Any())
            {
                _logger.LogWarning("Checkout failed — cart {CartId} is empty", cartId);
                throw new InvalidOperationException("Cannot checkout an empty cart.");
            }

            // transition status
            cart.Status = CartStatus.CheckedOut;
            cart.CheckedOutAt = DateTime.UtcNow;

            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cart {CartId} checked out successfully at {Time}", cartId, cart.CheckedOutAt);

            return cart;
        }


        private async Task UpdateLastActivityAsync(int cartId)
        {
            var cart = await _context.Carts.FindAsync(cartId);
            if (cart is not null)
                cart.LastActivityAt = DateTime.UtcNow;
        }
    }
}