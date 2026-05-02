using CatalogService.Data;
using CatalogService.Exceptions;
using CatalogService.Models;
using CatalogService.Services.Messaging;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using System.Linq;

namespace CatalogService.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;
        private readonly IPublisherForReport _publish;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ProductDbContext context, IPublisherForReport publish, ILogger<ProductRepository> logger)
        {
            _context = context;
            _publish = publish;
            _logger = logger;
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private async Task<Product> GetProductOrThrowAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product is null)
            {
                _logger.LogWarning("Product {ProductId} not found.", productId);
                throw new ProductNotFoundException(productId);   
            }
            return product;
        }

        private async Task<ProductVariant> GetVariantOrThrowAsync(int variantId)
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId);
            if (variant is null)
            {
                _logger.LogWarning("Variant {VariantId} not found.", variantId);
                throw new VariantNotFoundException(variantId);   
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
                throw new ProductAlreadyExistsException(product.Id);  
            }

            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} created.", product.Id);
                await PublishStatusEventAsync(product);
            }
            catch (CatalogException) { throw; }  
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
                    .Include(p => p.Media)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} products.", products.Count);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products.");
                throw;  
            }
        }

        public async Task<(List<Product> Items, int TotalCount)> GetPaginatedProductsAsync(int page, int pageSize, ProductStatus? status = null)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants)
                    .Include(p => p.Media)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(p => p.Status == status.Value);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated products.");
                throw;
            }
        }

        public async Task<Product> GetProductDetailsAsync(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants)
                    .Include(p => p.Media)
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
                throw;  
            }
        }

        public async Task UpdateProductAsync(int id, Product updatedProduct)
        {
            try
            {
                var product = await GetProductOrThrowAsync(id);

                if (product.Status != ProductStatus.Draft && product.Status != ProductStatus.Submitted &&
                    product.Status != ProductStatus.Rejected &&
                    product.Status != ProductStatus.Inactive &&
                    product.Status != ProductStatus.Active)
                {
                    _logger.LogWarning("UpdateProductAsync: invalid status {Status} for product {ProductId}.", product.Status, id);
                    throw new InvalidProductStatusTransitionException(product.Status, ProductStatus.Draft, "FullUpdate");
                }

                product.Status = ProductStatus.Draft;
                product.Name = updatedProduct.Name;
                product.Description = updatedProduct.Description;
                product.Price = updatedProduct.Price;
                product.AvailableQuantity = updatedProduct.AvailableQuantity;
                product.CategoryId = updatedProduct.CategoryId;
                _context.ProductMedias.RemoveRange(product.Media);
                foreach (var media in updatedProduct.Media)
                {
                    product.Media.Add(new ProductMedia
                    {
                        ProductId = product.Id,
                        MediaUrl = media.MediaUrl,
                        MediaType = media.MediaType
                    });
                }

                var existingVariants = await _context.ProductVariants
                    .Where(v => v.ProductId == product.Id)
                    .ToListAsync();
                _context.ProductVariants.RemoveRange(existingVariants);
                foreach (var variant in updatedProduct.Variants)
                {
                    product.Variants.Add(new ProductVariant
                    {
                        ProductId = product.Id,
                        SKU = variant.SKU,
                        Price = variant.Price,
                        Stock = variant.Stock,
                        Attributes = variant.Attributes
                    });
                }

                product.Tags = updatedProduct.Tags;

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

        public async Task<List<Product>> SearchProductAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("SearchProductAsync called with empty query.");
                throw new ArgumentException("Search query cannot be empty.", nameof(query));
            }

            try
            {
                var products = await _context.Products
                    .Where(p => (p.Name.Contains(query) || p.Description.Contains(query))
                             && p.Status != ProductStatus.Draft
                             && p.Status != ProductStatus.Inactive)
                    .Include(p => p.Media)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} products matching '{Query}'.", products.Count, query);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching product by query '{Query}'.", query);
                throw;
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
                    .Include(p => p.Media)
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

        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                return await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories.");
                throw;
            }
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            if (category is null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            try
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category {CategoryName}.", category.Name);
                throw;
            }
        }

        // ─── Product Lifecycle ────────────────────────────────────────────────────

        public async Task SubmitProduct(int productId)
        {
            try
            {
                var product = await GetProductOrThrowAsync(productId);

                if (product.Status != ProductStatus.Draft && product.Status != ProductStatus.Rejected)
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

                if (product.Status != ProductStatus.Submitted && product.Status != ProductStatus.Draft && product.Status != ProductStatus.Rejected)
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

                if (product.Status != ProductStatus.Submitted && product.Status != ProductStatus.Active && product.Status != ProductStatus.Approved)
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

        // Updates

        public async Task UpdatePriceAsync(int productId, decimal newPrice)
        {
            try
            {
                var product = await GetProductOrThrowAsync(productId);

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