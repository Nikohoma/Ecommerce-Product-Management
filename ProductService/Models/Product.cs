namespace CatalogService.Models
{
    public enum ProductStatus
    {
        Draft,
        Submitted,
        Approved,
        Rejected,
        Active,
        Inactive
    }

    public class Product : BaseEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        // Rename for clarity (optional but recommended)
        public int AvailableQuantity { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Draft;

        //  Relationship
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}
