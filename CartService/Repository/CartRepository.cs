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
            return await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
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
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<CartItem> UpdateItemAsync(CartItem item)
        {
            _logger.LogInformation("Updating item {ItemId} in cart {CartId}", item.Id, item.CartId);
            _context.CartItems.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task RemoveItemAsync(int itemId)
        {
            _logger.LogInformation("Removing item {ItemId}", itemId);
            var item = await _context.CartItems.FindAsync(itemId);
            if (item is null) return;
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}