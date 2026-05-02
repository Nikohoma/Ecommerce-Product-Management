using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTO.ProductVariant
{
    // Used when variants are nested inside ProductCreateDto / Product update payloads.
    // ProductId is implied by the owning Product, so we don't validate/require it here.
    public class ProductVariantForProductDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string SKU { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        public int Stock { get; set; }

        public string? Attributes { get; set; }

        [Url(ErrorMessage = "ImageUrl must be a valid URL.")]
        public string? ImageUrl { get; set; }
    }
}

