using CartService.DTOs;
using CartService.Models;

namespace CartService.Services
{
    public interface ICartService
    {
        Task<CartService.Models.Cart> GetCartAsync(string userId);
        Task<CartItem> AddItemAsync(string userId, int productId, int quantity);
        Task<CartItem> UpdateItemQuantityAsync(string userId, int itemId, int newQuantity);
        Task RemoveItemAsync(string userId, int itemId);
        Task ClearCartAsync(string userId);
        Task<CartCheckoutDto> CheckoutAsync(string userId);  
    }
}