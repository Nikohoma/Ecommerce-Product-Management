using CatalogService.DTO.Products;
using CatalogService.Models;

namespace CatalogService.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllProductsAsync();
        Task CreateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        Task<Product> GetProductDetailsAsync(int id);
        Task UpdateProductAsync(int id, Product updatedProduct);
            
    }
}
