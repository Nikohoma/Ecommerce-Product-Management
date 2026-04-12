using OrderService.DTO;

namespace OrderService.DTOs
{
    public class CartCheckoutDto
    {
        public string UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public DateTime CheckoutTime { get; set; }
    }

    
}