using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models;

namespace CatalogService.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;
        public ProductRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task CreateProductAsync(Product product)
        {
            if (product == null) { return; }
            var ifPresent = await _context.Products.AnyAsync(p => p.Id == product.Id);
            if (ifPresent) { Console.WriteLine("Product already present"); return; }
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var present = await _context.Products.Where(p=>p.Id == id).FirstOrDefaultAsync();
            if(present == null) { Console.WriteLine("No such product.");return; }
            _context.Products.Remove(present);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = await _context.Products.ToListAsync();
            if (products!= null) { return products; }
            return new List<Product>();
        }

        public async Task<Product> GetProductDetailsAsync(int id)
        {
            var product = await _context.Products.Where(p => p.Id == id).FirstOrDefaultAsync();
            if (product == null) { Console.WriteLine("No Product Found"); return new Product(); }
            return product;
        }

        public async Task UpdateProductAsync(int id, Product updatedProduct)
        {
            var product = await _context.Products.Where(p=>p.Id == id).FirstOrDefaultAsync();
            if (product == null) { Console.WriteLine("Product not found"); return; }

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.Stock = updatedProduct.Stock;

            Console.WriteLine("Product updated.");
            await _context.SaveChangesAsync();
            

        }
    }
}
