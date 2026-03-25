using CatalogService.DTO.Products;
using CatalogService.Models;

namespace CatalogService.Repositories
{
    public interface IProductRepository
    {
        //Task<List<Product>> GetAllProductsAsync();
        //Task CreateProductAsync(Product product);
        //Task DeleteProductAsync(int id);
        //Task<Product> GetProductDetailsAsync(int id);
        //Task UpdateProductAsync(int id, Product updatedProduct);
        //Task<Product> SearchProductAsync(string name);
        //Task<List<Product>> GetProductsByCategoryAsync(int categoryId);

        //Task SubmitProduct(int productId);

        // Basic CRUD
        Task<List<Product>> GetAllProductsAsync();
        Task CreateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        Task<Product> GetProductDetailsAsync(int id);
        Task UpdateProductAsync(int id, Product updatedProduct);

        // Search & Filter
        Task<Product> SearchProductAsync(string name);
        Task<List<Product>> GetProductsByCategoryAsync(int categoryId);

        // Product Lifecycle
        Task SubmitProduct(int productId);           // Draft → Submitted
        Task ApproveProductAsync(int productId);    // Submitted → Active
        Task RejectProductAsync(int productId);     // Submitted → Rejected

        // Restricted Updates
        Task UpdatePriceAsync(int productId, decimal newPrice);    // Active only
        Task UpdateStockAsync(int productId, int quantity);        // Active only

        // Optional future: stock deduction for orders
        Task<bool> DeductStockAsync(int productId, int quantity);

        // Variant methods
        Task CreateVariantAsync(ProductVariant variant);
        Task<List<ProductVariant>> GetVariantsByProductAsync(int productId);
        Task<ProductVariant> GetVariantDetailsAsync(int variantId);
        Task UpdateVariantAsync(int variantId, ProductVariant updatedVariant);
        Task UpdateVariantPriceAsync(int variantId, decimal newPrice);
        Task UpdateVariantStockAsync(int variantId, int quantity);
        Task DeleteVariantAsync(int variantId);
        Task<bool> DeductVariantStockAsync(int variantId, int quantity);

    }
}
