using CatalogService.DTO.Products;
using CatalogService.DTO.ProductVariant;
using CatalogService.Models;

namespace CatalogService.Services
{
    public interface IProductService
    {
        //Task<List<Product>> GetAllProducts();
        //Task CreateProduct(ProductCreateDto product);
        //Task DeleteProduct(int id);
        //Task<Product> GetProductDetails(int id);
        //Task UpdateProduct(int id, ProductCreateDto updatedProduct);
        //Task<Product> SearchProduct(string name);
        //Task<List<Product>> GetProductsByCategory(int categoryId);

        // Basic CRUD
        Task<List<Product>> GetAllProducts();
        Task CreateProduct(ProductCreateDto product);
        Task DeleteProduct(int id);
        Task<Product> GetProductDetails(int id);
        Task UpdateProduct(int id, ProductCreateDto updatedProduct);

        // Search & Filter
        Task<Product> SearchProduct(string name);
        Task<List<Product>> GetProductsByCategory(int categoryId);

        // Product Lifecycle
        Task SubmitProduct(int productId);           // Draft → Submitted
        Task ApproveProduct(int productId);         // Submitted → Active
        Task RejectProduct(int productId);          // Submitted → Rejected

        // Restricted Updates
        Task UpdatePrice(int productId, decimal newPrice);    // Active only
        Task UpdateStock(int productId, int quantity);        // Active only

        // Optional for order integration
        Task<bool> DeductStock(int productId, int quantity);

        // Variant 
        Task CreateVariant(ProductVariantCreateDto variantDto);
        Task<List<ProductVariant>> GetVariantsByProduct(int productId);
        Task<ProductVariant> GetVariantDetails(int variantId);
        Task UpdateVariant(int variantId, ProductVariantCreateDto updatedVariant);
        Task UpdateVariantPrice(int variantId, decimal newPrice);
        Task UpdateVariantStock(int variantId, int newStock);
        Task DeleteVariant(int variantId);
        Task<bool> DeductVariantStock(int variantId, int quantity);

    }
}
