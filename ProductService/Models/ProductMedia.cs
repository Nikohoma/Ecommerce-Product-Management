using System.Text.Json.Serialization;

namespace CatalogService.Models
{
    public class ProductMedia
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string MediaUrl { get; set; } = string.Empty;
        public string MediaType { get; set; } = "image";

        [JsonIgnore]
        public Product Product { get; set; } = null!;
    }
}
