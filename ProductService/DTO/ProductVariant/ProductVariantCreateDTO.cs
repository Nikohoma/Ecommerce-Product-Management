using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTO.ProductVariant
{
    public class ProductVariantCreateDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive integer.")]
        public int ProductId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string SKU { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        public int Stock { get; set; }

        public string? Attributes { get; set; }
    }
}
