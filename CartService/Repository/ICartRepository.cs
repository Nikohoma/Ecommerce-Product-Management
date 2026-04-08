using CartService.Models;

public interface ICartRepository
{
    Task<CartService.Models.Cart?> GetByUserIdAsync(string userId);
    Task<CartService.Models.Cart> CreateAsync(CartService.Models.Cart cart);
    Task<CartItem> AddItemAsync(CartItem item);
    Task<CartItem> UpdateItemAsync(CartItem item);
    Task RemoveItemAsync(int itemId);
}