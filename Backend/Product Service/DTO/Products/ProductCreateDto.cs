using System.ComponentModel.DataAnnotations;
using CatalogService.DTO.ProductVariant;

namespace CatalogService.DTO.Products
{
    public class ProductCreateDto
    {
        [Required,StringLength(25,MinimumLength =3)]
        public string Name { get; set; }

        public string Description { get; set; }
        [Required,Range(0,999999)]
        public decimal Price { get; set; }
        [Required]
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public List<string> MediaUrls { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<ProductVariantForProductDto> Variants { get; set; } = new();
    }
}
