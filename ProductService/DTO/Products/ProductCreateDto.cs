using System.ComponentModel.DataAnnotations;

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
    }
}
