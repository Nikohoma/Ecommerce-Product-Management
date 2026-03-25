namespace CatalogService.DTO.ProductVariant
{
    public class ProductVariantCreateDto
    {
        public int ProductId { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Attributes { get; set; }
    }
}
