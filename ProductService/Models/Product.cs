namespace CatalogService.Models
{
    public enum ProductStatus
    {
        Draft,
        Submitted,
        Approved,
        Rejected,
        Active,  // After Approve
        Inactive  // Delete
    }

    public class Product : BaseEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

  
        public int AvailableQuantity { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Draft;

        //  Relationship
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductMedia> Media { get; set; } = new List<ProductMedia>();
        public List<string> Tags { get; set; } = new();
    }
}
