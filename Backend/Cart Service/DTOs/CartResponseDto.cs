namespace CartService.DTOs
{
    public class CartResponseDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public List<CartItemResponseDto> Items { get; set; } = new();
        public decimal TotalPrice { get; set; }
    }

    public class CartItemResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
        public string ImageUrl { get; set; }
    }
}
