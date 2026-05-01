using System.Text.Json.Serialization;

namespace CatalogService.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        [JsonIgnore]
        public Product Product { get; set; }

        public string SKU { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Attributes { get; set; } // JSON for flexible attributes
    }
}
