namespace CartService.DTOs
{
    public class CartCheckoutDto
    {
        public int CartId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CheckedOutAt { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CartItemSnapshotDto> Items { get; set; } = new();
    }
}
