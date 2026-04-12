using System.Text.Json.Serialization;

namespace OrderService.DTO
{
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        [JsonPropertyName("UnitPrice")]
        public decimal PriceSnapshot { get; set; }
    }
}
