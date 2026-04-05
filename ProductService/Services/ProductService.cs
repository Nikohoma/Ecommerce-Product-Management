using CatalogService.Data;
using CatalogService.DTO.Products;
using CatalogService.DTO.ProductVariant;
using CatalogService.Models;
using CatalogService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly ProductDbContext _context;

        public ProductService(IProductRepository repo, ProductDbContext context)
        {
            _repo = repo; _context = context;
        }
        public async Task CreateProduct(ProductCreateDto product)
        {
            if (!await _context.Categories.AnyAsync(c => c.Id == product.CategoryId))
            {
                throw new Exception("Invalid CategoryId");
            }
            var newProduct = new Product()
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                AvailableQuantity = product.Stock,
                CategoryId = product.CategoryId
            };

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

        
        public async Task<Product> SearchProduct(string name)
        {
            var find = await _repo.SearchProductAsync(name);
            return find;
        }

        public async Task<List<Product>> GetProductsByCategory(int categoryId)
        {
            return await _repo.GetProductsByCategoryAsync(categoryId);
        }

        public async Task SubmitProduct(int productId)
        {
            await _repo.SubmitProduct(productId);
            Console.WriteLine("Product submitted for approval.");
        }

        public async Task ApproveProduct(int productId)
        {
            await _repo.ApproveProductAsync(productId);
            Console.WriteLine("Product approved and active.");
        }

        public async Task RejectProduct(int productId)
        {
            await _repo.RejectProductAsync(productId);
            Console.WriteLine("Product rejected.");
        }

        public async Task UpdatePrice(int productId, decimal newPrice)
        {
            await _repo.UpdatePriceAsync(productId, newPrice);
            Console.WriteLine($"Product {productId} price updated to {newPrice}");
        }

        public async Task UpdateStock(int productId, int newStock)
        {
            await _repo.UpdateStockAsync(productId, newStock);
            Console.WriteLine($"Product {productId} stock updated to {newStock}");
        }

        public async Task UpdateProduct(int id, ProductCreateDto updatedProduct)
        {
            if (!await _context.Categories.AnyAsync(c => c.Id == updatedProduct.CategoryId))
                throw new Exception("Invalid CategoryId");

            var product = new Product()
            {
                Name = updatedProduct.Name,
                Description = updatedProduct.Description,
                Price = updatedProduct.Price,
                AvailableQuantity = updatedProduct.Stock,   // align with lifecycle field
                CategoryId = updatedProduct.CategoryId
            };

            await _repo.UpdateProductAsync(id, product);  // only allowed if Draft
            Console.WriteLine("Product updated successfully.");
        }

        public async Task<bool> DeductStock(int productId, int quantity)
        {
            var success = await _repo.DeductStockAsync(productId, quantity);
            if (success)
                Console.WriteLine($"Deducted {quantity} units from Product {productId}");
            else
                Console.WriteLine($"Insufficient stock for Product {productId}");

            return success;
        }

        // ----------------- Variant Methods -----------------

        public async Task CreateVariant(ProductVariantCreateDto variantDto)
        {
            // Check if parent product exists
            var productExists = await _context.Products
                .AnyAsync(p => p.Id == variantDto.ProductId);

            if (!productExists)
                throw new Exception("Invalid ProductId");

            var variant = new ProductVariant()
            {
                ProductId = variantDto.ProductId,
                SKU = variantDto.SKU,
                Price = variantDto.Price,
                Stock = variantDto.Stock,
                Attributes = variantDto.Attributes
            };

            await _repo.CreateVariantAsync(variant);
            Console.WriteLine("Variant created successfully.");
        }

        public async Task<List<ProductVariant>> GetVariantsByProduct(int productId)
        {
            return await _repo.GetVariantsByProductAsync(productId);
        }

        public async Task<ProductVariant> GetVariantDetails(int variantId)
        {
            return await _repo.GetVariantDetailsAsync(variantId);
        }

        public async Task UpdateVariant(int variantId, ProductVariantCreateDto updatedVariant)
        {
            var variant = new ProductVariant()
            {
                SKU = updatedVariant.SKU,
                Price = updatedVariant.Price,
                Stock = updatedVariant.Stock,
                Attributes = updatedVariant.Attributes
            };

            await _repo.UpdateVariantAsync(variantId, variant);
            Console.WriteLine("Variant updated successfully.");
        }

        public async Task UpdateVariantPrice(int variantId, decimal newPrice)
        {
            await _repo.UpdateVariantPriceAsync(variantId, newPrice);
            Console.WriteLine($"Variant {variantId} price updated to {newPrice}");
        }

        public async Task UpdateVariantStock(int variantId, int newStock)
        {
            await _repo.UpdateVariantStockAsync(variantId, newStock);
            Console.WriteLine($"Variant {variantId} stock updated to {newStock}");
        }

        public async Task DeleteVariant(int variantId)
        {
            await _repo.DeleteVariantAsync(variantId);
            Console.WriteLine("Variant deleted successfully.");
        }

        public async Task<bool> DeductVariantStock(int variantId, int quantity)
        {
            var success = await _repo.DeductVariantStockAsync(variantId, quantity);

            if (success)
                Console.WriteLine($"Deducted {quantity} units from Variant {variantId}");
            else
                Console.WriteLine($"Insufficient stock for Variant {variantId}");

            return success;
        }
    }
}
