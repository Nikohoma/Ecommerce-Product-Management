using CartService.DTOs;
using CartService.Exceptions;
using CartService.Models;
using CartService.Repositories;

namespace CartService.Services
{
    public class cartService : ICartService  
    {
        private readonly ICartRepository _cartRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<cartService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public cartService(ICartRepository cartRepository, IHttpClientFactory httpClientFactory, ILogger<cartService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _cartRepository = cartRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CartService.Models.Cart> GetCartAsync(string userId)
        {
            _logger.LogInformation("Getting cart for user {UserId}", userId);
            return await _cartRepository.GetByUserIdAsync(userId)
                ?? throw new CartNotFoundException(userId);
        }

        public async Task<CartItem> AddItemAsync(string userId, int productId, int quantity)
        {
            if (quantity <= 0)
                throw new InvalidQuantityException(quantity);

            var product = await FetchProductAsync(productId);

            var cart = await _cartRepository.GetByUserIdAsync(userId)
                       ?? await _cartRepository.CreateAsync(new CartService.Models.Cart
                       {
                           UserId = userId,
                           Items = new List<CartItem>()
                       });

            if (cart.Status != CartStatus.Active)
                throw new CartException($"Cart is {cart.Status}. Items can only be added to an Active cart.");

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (existing is not null)
            {
                existing.Quantity += quantity;
                return await _cartRepository.UpdateItemAsync(existing);
            }

            return await _cartRepository.AddItemAsync(new CartItem
            {
                CartId = cart.Id,
                ProductId = productId,
                Quantity = quantity,
                PriceSnapshot = product!.Price
            });
        }

        public async Task<CartItem> UpdateItemQuantityAsync(string userId, int itemId, int newQuantity)
        {
            if (newQuantity <= 0)
                throw new InvalidQuantityException(newQuantity);

            var cart = await _cartRepository.GetByUserIdAsync(userId)
                       ?? throw new CartNotFoundException(userId);

            if (cart.Status != CartStatus.Active)
                throw new CartException($"Cart is {cart.Status}. Only Active carts can be modified.");

            var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
                       ?? throw new CartItemNotFoundException(itemId);

            item.Quantity = newQuantity;
            return await _cartRepository.UpdateItemAsync(item);
        }

        public async Task RemoveItemAsync(string userId, int itemId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId)
                       ?? throw new CartNotFoundException(userId);

            if (cart.Status != CartStatus.Active)
                throw new CartException($"Cart is {cart.Status}. Only Active carts can be modified.");

            var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
                       ?? throw new CartItemNotFoundException(itemId);

            await _cartRepository.RemoveItemAsync(item.Id);
        }

        public async Task ClearCartAsync(string userId)
        {
            _logger.LogInformation("Clearing cart for user {UserId}", userId);
            var cart = await _cartRepository.GetByUserIdAsync(userId)
                       ?? throw new CartNotFoundException(userId);

            if (cart.Status != CartStatus.Active)
                throw new CartException($"Cart is {cart.Status}. Only Active carts can be cleared.");

            foreach (var item in cart.Items.ToList())
                await _cartRepository.RemoveItemAsync(item.Id);
        }

        public async Task<CartCheckoutDto> CheckoutAsync(string userId)
        {
            _logger.LogInformation("Checkout initiated for user {UserId}", userId);

            var cart = await _cartRepository.GetByUserIdAsync(userId)?? throw new CartNotFoundException(userId);

            var checkedOutCart = await _cartRepository.CheckoutAsync(cart.Id);

            _logger.LogInformation("Cart {CartId} checked out for user {UserId}", cart.Id, userId);

            // build snapshot DTO for Order Service (Phase 1: return it; Phase 2: publish to queue)
            return new CartCheckoutDto
            {
                CartId = checkedOutCart.Id,
                UserId = checkedOutCart.UserId,
                CheckedOutAt = checkedOutCart.CheckedOutAt!.Value,
                TotalAmount = checkedOutCart.Total,
                Items = checkedOutCart.Items.Select(i => new CartItemSnapshotDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.PriceSnapshot,
                    LineTotal = i.Quantity * i.PriceSnapshot
                }).ToList()
            };
        }
        private async Task<ProductDto> FetchProductAsync(int productId)
        {
            var client = _httpClientFactory.CreateClient("Catalog");

            var token = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString();
            client.DefaultRequestHeaders.Add("Authorization", token);

            var response = await client.GetAsync($"/api/products/{productId}");

            if (!response.IsSuccessStatusCode)
                throw new CartException($"Product {productId} not found in catalog.");

            return await response.Content.ReadFromJsonAsync<ProductDto>()
                ?? throw new CartException($"Failed to deserialize product {productId}.");
        }
    }
}