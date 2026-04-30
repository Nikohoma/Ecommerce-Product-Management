using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTO.Products
{
    public class CategoryCreateDto
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
    }
}
