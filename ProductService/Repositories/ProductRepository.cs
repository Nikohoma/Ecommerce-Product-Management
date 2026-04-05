using CatalogService.Exceptions;
using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models;
using CatalogService.Services.Messaging;
using Shared.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace CatalogService.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;
        private readonly PublisherForReport _publish;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ProductDbContext context, PublisherForReport publish, ILogger<ProductRepository> logger)
        {
            _context = context;
            _publish = publish;
            _logger = logger;
        }

        private async Task<Product> GetProductOrThrowAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product is null)
            {
                _logger.LogWarning("Product {ProductId} not found.", productId);
                Console.WriteLine("Product not found"); return default;
            }
            return product;
        }

        private async Task<ProductVariant> GetVariantOrThrowAsync(int variantId)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant is null)
            {
                _logger.LogWarning("Variant {VariantId} not found.", variantId);
                Console.WriteLine("Variant not found."); return default;
            }
            return variant;
        }

        private async Task PublishStatusEventAsync(Product product)
        {
            await _publish.SendProductForReporting(new ProductStatusChangedEvent
            {
                ProductId = product.Id,
                Status = product.Status.ToString(),
                UpdatedAt = DateTime.UtcNow,
                Price = product.Price
            });
        }

        // CRUD + Search + Filter 

        public async Task CreateProductAsync(Product product)
        {
            if (product is null)
            {
                _logger.LogWarning("CreateProductAsync called with null product.");
                return;
            }

            try
            {
                if (await _context.Products.AnyAsync(p => p.Id == product.Id))
                {
                    _logger.LogWarning("Product {ProductId} already exists.", product.Id);
                    Console.WriteLine("Product already exists."); return ;
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} created.", product.Id);

                await PublishStatusEventAsync(product);
            }
            catch (CatalogException) { return; }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error creating product {ProductId}.", product.Id);
                Console.WriteLine("Db Update Exception Occured",ex.Message); return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating product {ProductId}.", product.Id);
                Console.WriteLine("An Error Occured", ex.Message); return;
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                var products = await _context.Products.Include(p => p.Category).Include(p => p.Variants).ToListAsync();

                _logger.LogInformation("Retrieved {Count} products.", products.Count);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products.");
                Console.WriteLine("An error occured : "+ex.Message);
                return default;
            }
        }

        public async Task<Product> GetProductDetailsAsync(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product is null)
                {
                    _logger.LogWarning("Product {ProductId} not found : ", id);
                    throw new ProductNotFoundException(id);
                }

                return product;
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}.", id);
                Console.WriteLine("Error retrieving product : " + ex.Message);
                return default;
            }
        }

        public async Task UpdateProductAsync(int id, Product updatedProduct)
        {
            try
            {
                var product = await GetProductOrThrowAsync(id);

                if (product.Status != ProductStatus.Draft)
                {
                    _logger.LogWarning("UpdateProductAsync: Invalid status {Status} for product {ProductId}.", product.Status, id);
                    //throw new InvalidProductStatusTransitionException(product.Status, ProductStatus.Draft, "FullUpdate");
                    Console.WriteLine("Invalid Status.");
                }

                product.Name = updatedProduct.Name;
                product.Description = updatedProduct.Description;
                product.Price = updatedProduct.Price;
                product.AvailableQuantity = updatedProduct.AvailableQuantity;
                product.CategoryId = updatedProduct.CategoryId;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} updated.", id);
                await PublishStatusEventAsync(product);
            }
            catch (CatalogException) { throw; }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error updating product {ProductId}.", id);
                Console.WriteLine("Db Update Exception : ",ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating product {ProductId}.", id);
                Console.WriteLine("An Error Occured : ",ex.Message); return;
            }
        }

        public async Task<Product> SearchProductAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("SearchProductAsync called with empty name.");
                return default;
            }

            try
            {
                var product = await _context.Products.Where(p => p.Name.ToLower().Contains(name.ToLower())&& p.Status != ProductStatus.Draft&& p.Status != ProductStatus.Inactive).FirstOrDefaultAsync();

                if (product is null)
                    _logger.LogWarning("No product found matching '{Name}'.", name);

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching product by name '{Name}'.", name);
                Console.WriteLine("Error encountered : ",ex.Message); return default;
            }
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            try
            {
                var products = await _context.Products.Where(p => p.CategoryId == categoryId&& p.Status != ProductStatus.Draft&& p.Status != ProductStatus.Inactive).Include(p => p.Category).ToListAsync();

                _logger.LogInformation("Retrieved {Count} products for category {CategoryId}.", products.Count, categoryId);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for category {CategoryId}.", categoryId);
                Console.WriteLine("Error encountered : ",ex.Message); return default;
            }
        }

        // ─── Product Lifecycle ─────────────────────────────────────────────────────

        public async Task SubmitProduct(int productId)
        {
            try
            {
                var product = await GetProductOrThrowAsync(productId);

                if (product.Status != ProductStatus.Draft)
                    throw new InvalidProductStatusTransitionException(product.Status, ProductStatus.Draft, "Submit");

                product.Status = ProductStatus.Submitted;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} submitted.", productId);
                await PublishStatusEventAsync(product);
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting product {ProductId}.", productId);
                Console.WriteLine("Error encountered : ", ex.Message); return ;

            }
        }

        public async Task ApproveProductAsync(int productId)
        {
            try
            {
                var product = await GetProductOrThrowAsync(productId);

                if (product.Status != ProductStatus.Submitted)
                    throw new InvalidProductStatusTransitionException(product.Status, ProductStatus.Submitted, "Approve");

                product.Status = ProductStatus.Active;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} approved.", productId);
                await PublishStatusEventAsync(product);
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error approving product {ProductId}.", productId);
                Console.WriteLine("Error encountered : ", ex.Message); return ;

            }
        }

        public async Task RejectProductAsync(int productId)
        {
            try
            {
                var product = await GetProductOrThrowAsync(productId);

                if (product.Status != ProductStatus.Submitted)
                    throw new InvalidProductStatusTransitionException(product.Status, ProductStatus.Submitted, "Reject");

                product.Status = ProductStatus.Rejected;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} rejected.", productId);
                await PublishStatusEventAsync(product);
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error rejecting product {ProductId}.", productId);
                Console.WriteLine("Error encountered : ", ex.Message); return ;

            }
        }

        // ─── Restricted Updates ────────────────────────────────────────────────────

        public async Task UpdatePriceAsync(int productId, decimal newPrice)
        {
            try
            {
                var product = await GetProductOrThrowAsync(productId);

                if (product.Status != ProductStatus.Active)
                    throw new InvalidProductStatusTransitionException(product.Status, ProductStatus.Active, "UpdatePrice");

                product.Price = newPrice;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Price updated for product {ProductId} → {Price}.", productId, newPrice);
                await PublishStatusEventAsync(product);
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating price for product {ProductId}.", productId);
                Console.WriteLine("Error encountered : ", ex.Message); return;

            }
        }

        public async Task UpdateStockAsync(int productId, int quantity)
        {
            if (quantity < 0)
                throw new NegativeStockException();

            try
            {
                var product = await GetProductOrThrowAsync(productId);
                product.AvailableQuantity = quantity;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Stock updated for product {ProductId} → {Quantity}.", productId, quantity);
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating stock for product {ProductId}.", productId);
                Console.WriteLine("Error encountered : ", ex.Message); return ;

            }
        }

        public async Task DeleteProductAsync(int productId)
        {
            try
            {
                var product = await GetProductOrThrowAsync(productId);
                product.Status = ProductStatus.Inactive;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} soft-deleted.", productId);
                await PublishStatusEventAsync(product);
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting product {ProductId}.", productId);
                Console.WriteLine("Error encountered : ", ex.Message); return ;

            }
        }

        public async Task<bool> DeductStockAsync(int productId, int quantity)
        {
            try
            {
                var product = await GetProductOrThrowAsync(productId);

                if (product.AvailableQuantity < quantity)
                    throw new InsufficientStockException(product.AvailableQuantity, quantity);

                product.AvailableQuantity -= quantity;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deducted {Quantity} from product {ProductId}. Remaining: {Remaining}.",
                    quantity, productId, product.AvailableQuantity);
                return true;
            }
            catch (CatalogException) { return default; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deducting stock for product {ProductId}.", productId);
                Console.WriteLine("Error encountered : ", ex.Message); return default;

            }
        }

        // ─── Variant Methods 

        public async Task CreateVariantAsync(ProductVariant variant)
        {
            if (variant is null)
            {
                _logger.LogWarning("CreateVariantAsync called with null variant.");
                return;
            }

            try
            {
                if (!await _context.Products.AnyAsync(p => p.Id == variant.ProductId)) { Console.WriteLine("Product Not found."); return; }

                if (await _context.ProductVariants.AnyAsync(v => v.ProductId == variant.ProductId && v.SKU == variant.SKU)) { Console.WriteLine("Product Not found."); return; }
               

                _context.ProductVariants.Add(variant);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Variant {VariantId} (SKU: {SKU}) created for product {ProductId}.",
                    variant.Id, variant.SKU, variant.ProductId);
            }
            catch (CatalogException) { throw; }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error creating variant for product {ProductId}.", variant.ProductId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating variant for product {ProductId}.", variant.ProductId);
                throw;
            }
        }

        public async Task<List<ProductVariant>> GetVariantsByProductAsync(int productId)
        {
            try
            {
                var variants = await _context.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .Include(v => v.Product)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} variants for product {ProductId}.", variants.Count, productId);
                return variants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving variants for product {ProductId}.", productId);
                throw;
            }
        }

        public async Task<ProductVariant> GetVariantDetailsAsync(int variantId)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == variantId);

                if (variant is null)
                    throw new VariantNotFoundException(variantId);

                return variant;
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving variant {VariantId}.", variantId);
                throw;
            }
        }

        public async Task UpdateVariantAsync(int variantId, ProductVariant updatedVariant)
        {
            try
            {
                var variant = await GetVariantOrThrowAsync(variantId);

                variant.SKU = updatedVariant.SKU;
                variant.Price = updatedVariant.Price;
                variant.Stock = updatedVariant.Stock;
                variant.Attributes = updatedVariant.Attributes;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Variant {VariantId} updated.", variantId);
            }
            catch (CatalogException) { throw; }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error updating variant {VariantId}.", variantId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating variant {VariantId}.", variantId);
                throw;
            }
        }

        public async Task UpdateVariantPriceAsync(int variantId, decimal newPrice)
        {
            try
            {
                var variant = await GetVariantOrThrowAsync(variantId);
                variant.Price = newPrice;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Price updated for variant {VariantId} → {Price}.", variantId, newPrice);
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating price for variant {VariantId}.", variantId);
                throw;
            }
        }

        public async Task UpdateVariantStockAsync(int variantId, int quantity)
        {
            if (quantity < 0)
                throw new NegativeStockException();

            try
            {
                var variant = await GetVariantOrThrowAsync(variantId);
                variant.Stock = quantity;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Stock updated for variant {VariantId} → {Quantity}.", variantId, quantity);
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating stock for variant {VariantId}.", variantId);
                throw;
            }
        }

        public async Task DeleteVariantAsync(int variantId)
        {
            try
            {
                var variant = await GetVariantOrThrowAsync(variantId);
                _context.ProductVariants.Remove(variant);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Variant {VariantId} deleted.", variantId);
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting variant {VariantId}.", variantId);
                throw;
            }
        }

        public async Task<bool> DeductVariantStockAsync(int variantId, int quantity)
        {
            try
            {
                var variant = await GetVariantOrThrowAsync(variantId);

                if (variant.Stock < quantity)
                    throw new InsufficientStockException(variant.Stock, quantity);

                variant.Stock -= quantity;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deducted {Quantity} from variant {VariantId}. Remaining: {Remaining}.",
                    quantity, variantId, variant.Stock);
                return true;
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deducting stock for variant {VariantId}.", variantId);
                throw;
            }
        }
    }
}