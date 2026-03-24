using CatalogService.DTO.Products;
using CatalogService.Models;
using CatalogService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        public ProductService(IProductRepository repo)
        {
            _repo = repo;
        }
        public async Task CreateProduct(ProductCreateDto product)
        {
            var newProduct = new Product() { Name = product.Name, Description = product.Description, Price = product.Price, Stock = product.Stock };
            await _repo.CreateProductAsync(newProduct);
            Console.WriteLine("Product created successfully.");
        }

        public async Task DeleteProduct(int id)
        {
            await _repo.DeleteProductAsync(id);
            Console.WriteLine("Product Deleted successfully.");
        }

        public async Task<List<Product>> GetAllProducts()
        {
            var list = await _repo.GetAllProductsAsync();
            return list;
        }

        public async Task<Product> GetProductDetails(int id)
        {
            var result = await _repo.GetProductDetailsAsync(id);
            return result;
        }

        public async Task UpdateProduct(int id, ProductCreateDto updatedProduct)
        {
            var newProduct = new Product() { Name = updatedProduct.Name, Description = updatedProduct.Description, Price = updatedProduct.Price, Stock = updatedProduct.Stock };
            await _repo.UpdateProductAsync(id, newProduct);
            Console.WriteLine("Product updated successfully.");
        }

        public async Task<Product> SearchProduct(string name)
        {
            var find = await _repo.SearchProductAsync(name);
            return find;
        }
    }
}
