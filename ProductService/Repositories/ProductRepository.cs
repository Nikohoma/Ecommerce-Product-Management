using CatalogService.Exceptions;
using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models;
using CatalogService.Services.Messaging;
using Shared.Contracts;

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

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private async Task<Product> GetProductOrThrowAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product is null)
            {
                _logger.LogWarning("Product {ProductId} not found.", productId);
                throw new ProductNotFoundException(productId);   // was: return default
            }
            return product;
        }

        private async Task<ProductVariant> GetVariantOrThrowAsync(int variantId)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant is null)
            {
                _logger.LogWarning("Variant {VariantId} not found.", variantId);
                throw new VariantNotFoundException(variantId);   // was: return default
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

        // ─── CRUD + Search + Filter ───────────────────────────────────────────────

        public async Task CreateProductAsync(Product product)
        {
            if (product is null)
            {
                _logger.LogWarning("CreateProductAsync called with null product.");
                throw new ArgumentNullException(nameof(product));
            }

            if (await _context.Products.AnyAsync(p => p.Id == product.Id))
            {
                _logger.LogWarning("Product {ProductId} already exists.", product.Id);
                throw new ProductAlreadyExistsException(product.Id);  // was: Console.WriteLine; return
            }

            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} created.", product.Id);
                await PublishStatusEventAsync(product);
            }
            catch (CatalogException) { throw; }  // was: swallowed
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error creating product {ProductId}.", product.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating product {ProductId}.", product.Id);
                throw;
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} products.", products.Count);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products.");
                throw;  // was: return default
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
                    _logger.LogWarning("Product {ProductId} not found.", id);
                    throw new ProductNotFoundException(id);
                }

                return product;
            }
            catch (CatalogException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}.", id);
                throw;  // was: return default
            }
        }

        public async Task UpdateProductAsync(int id, Product updatedProduct)
        {
            try
            {
                var product = await GetProductOrThrowAsync(id);

                if (product.Status != ProductStatus.Draft)
                {
                    _logger.LogWarning("UpdateProductAsync: invalid status {Status} for product {ProductId}.", product.Status, id);
                    throw new InvalidProductStatusTransitionException(product.Status, ProductStatus.Draft, "FullUpdate");  // was: commented out
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
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating product {ProductId}.", id);
                throw;
            }
        }

        public async Task<Product> SearchProductAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("SearchProductAsync called with empty name.");
                throw new ArgumentException("Search name cannot be empty.", nameof(name));
            }

            try
            {
                var product = await _context.Products
                    .Where(p => p.Name.ToLower().Contains(name.ToLower())
                             && p.Status != ProductStatus.Draft
                             && p.Status != ProductStatus.Inactive)
                    .FirstOrDefaultAsync();

                if (product is null)
                    _logger.LogWarning("No product found matching '{Name}'.", name);

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching product by name '{Name}'.", name);
                throw;  // was: return default
            }
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.CategoryId == categoryId
                             && p.Status != ProductStatus.Draft
                             && p.Status != ProductStatus.Inactive)
                    .Include(p => p.Category)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} products for category {CategoryId}.", products.Count, categoryId);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for category {CategoryId}.", categoryId);
                throw;  // was: return default
            }
        }

        // ─── Product Lifecycle ────────────────────────────────────────────────────

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
                throw;
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
                throw;
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
                throw;
            }
        }

        // ─── Restricted Updates ───────────────────────────────────────────────────

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
                throw;
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
                throw;
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
                throw;
            }
        }

        public async Task DeductStockAsync(int productId, int quantity)  // was: Task<bool>
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
            }
            catch (CatalogException) { throw; }  // was: return default — this swallowed InsufficientStockException!
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deducting stock for product {ProductId}.", productId);
                throw;
            }
        }

        // ─── Variant Methods ──────────────────────────────────────────────────────

        public async Task CreateVariantAsync(ProductVariant variant)
        {
            if (variant is null)
            {
                _logger.LogWarning("CreateVariantAsync called with null variant.");
                throw new ArgumentNullException(nameof(variant));
            }

            if (!await _context.Products.AnyAsync(p => p.Id == variant.ProductId))
            {
                _logger.LogWarning("Product {ProductId} not found when creating variant.", variant.ProductId);
                throw new ProductNotFoundException(variant.ProductId);   // was: Console.WriteLine; return
            }

            if (await _context.ProductVariants.AnyAsync(v => v.ProductId == variant.ProductId && v.SKU == variant.SKU))
            {
                _logger.LogWarning("SKU '{SKU}' already exists for product {ProductId}.", variant.SKU, variant.ProductId);
                throw new VariantSkuConflictException(variant.SKU, variant.ProductId);  // was: Console.WriteLine; return
            }

            try
            {
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

        public async Task DeductVariantStockAsync(int variantId, int quantity)  // was: Task<bool>
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