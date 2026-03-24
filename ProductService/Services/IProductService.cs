using CatalogService.DTO.Products;
using CatalogService.Models;

namespace CatalogService.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetAllProducts();
        Task CreateProduct(ProductCreateDto product);
        Task DeleteProduct(int id);
        Task<Product> GetProductDetails(int id);
        Task UpdateProduct(int id, ProductCreateDto updatedProduct);
        Task<Product> SearchProduct(string name);

    }
}
