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
        // CRUD + Search + Filter
        public async Task CreateProductAsync(Product product)
        {
            if (product == null) { return; }
            var ifPresent = await _context.Products.AnyAsync(p => p.Id == product.Id);
            if (ifPresent) { Console.WriteLine("Product already present"); return; }
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = await _context.Products.Include(p => p.Category).ToListAsync();

            return products ?? new List<Product>();
        }

        public async Task<Product> GetProductDetailsAsync(int id)
        {
            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                Console.WriteLine("No Product Found");
                return default;
            }

            return product;
        }

        public async Task UpdateProductAsync(int id, Product updatedProduct)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) { Console.WriteLine("Product not found"); return; }

            // Only draft products can have full updates
            if (product.Status != ProductStatus.Draft)
            {
                throw new Exception("Only draft products can be fully updated");
            }

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.AvailableQuantity = updatedProduct.AvailableQuantity;
            product.CategoryId = updatedProduct.CategoryId;

            await _context.SaveChangesAsync();
        }

        public async Task<Product> SearchProductAsync(string name)
        {
            var find = await _context.Products.Where(p => p.Name.ToLower().Contains(name.ToLower()) && p.Status != 0).FirstOrDefaultAsync();
            if (find != null) { return find; }
            Console.WriteLine("No Product found.");
            return default;
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .ToListAsync();
        }

        // Product Lifecycle
        public async Task SubmitProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product.Status != ProductStatus.Draft)
                throw new Exception("Only draft products can be submitted");

            product.Status = ProductStatus.Submitted;
            await _context.SaveChangesAsync();
        }
        public async Task ApproveProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new Exception("Product not found");

            if (product.Status != ProductStatus.Submitted)
                throw new Exception("Only submitted products can be approved");

            product.Status = ProductStatus.Active;
            await _context.SaveChangesAsync();
        }

        public async Task RejectProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new Exception("Product not found");

            if (product.Status != ProductStatus.Submitted)
                throw new Exception("Only submitted products can be rejected");

            product.Status = ProductStatus.Rejected;
            await _context.SaveChangesAsync();
        }

        // Restricted Updates
        public async Task UpdatePriceAsync(int productId, decimal newPrice)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new Exception("Product not found");

            if (product.Status != ProductStatus.Active)
                throw new Exception("Only active products can update price");

            product.Price = newPrice;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStockAsync(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new Exception("Product not found");
            if (quantity < 0) throw new Exception("Stock cannot be negative");

            product.AvailableQuantity = quantity;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return;

            // Soft delete
            product.Status = ProductStatus.Inactive;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeductStockAsync(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new Exception("Product not found");

            if (product.AvailableQuantity < quantity)
                return false; // Not enough stock

            product.AvailableQuantity -= quantity;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
