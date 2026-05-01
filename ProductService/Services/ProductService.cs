using CatalogService.Data;
using CatalogService.DTO.Products;
using CatalogService.DTO.ProductVariant;
using CatalogService.Exceptions;
using CatalogService.Models;
using CatalogService.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CatalogService.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly ProductDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository repo, ProductDbContext context, ILogger<ProductService> logger)
        {
            _repo = repo;
            _context = context;
            _logger = logger;
        }

        public async Task CreateProduct(ProductCreateDto product)
        {
            if (!await _context.Categories.AnyAsync(c => c.Id == product.CategoryId))
            {
                _logger.LogWarning("CreateProduct failed — CategoryId {CategoryId} not found.", product.CategoryId);
                throw new CategoryNotFoundException(product.CategoryId);
            }

            var newProduct = new Product
            {
                // After creation we start in Draft state.
                Status = ProductStatus.Draft,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                AvailableQuantity = product.Stock,
                CategoryId = product.CategoryId,
                Media = product.MediaUrls
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => new ProductMedia { MediaUrl = url.Trim(), MediaType = "image" })
                    .ToList(),
                Tags = product.Tags,
                Variants = product.Variants
                    .Select(v => new ProductVariant
                    {
                        SKU = v.SKU,
                        Price = v.Price,
                        Stock = v.Stock,
                        Attributes = BuildVariantAttributesPayload(v.Attributes, v.ImageUrl)
                    })
                    .ToList()
            };

            await _repo.CreateProductAsync(newProduct);
            _logger.LogInformation("Product '{Name}' created successfully.", product.Name);
        }

        public async Task DeleteProduct(int id)
        {
            await _repo.DeleteProductAsync(id);
            _logger.LogInformation("Product {ProductId} deleted.", id);
        }

        public async Task<List<Product>> GetAllProducts()
        {
            _logger.LogInformation("Fetching all products.");
            return await _repo.GetAllProductsAsync();
        }

        public async Task<PaginatedResult<Product>> GetPaginatedProducts(int page, int pageSize, ProductStatus? status = null)
        {
            _logger.LogInformation("Fetching products for page {Page} with size {PageSize} and status {Status}.", page, pageSize, status);
            var (items, totalCount) = await _repo.GetPaginatedProductsAsync(page, pageSize, status);
            
            return new PaginatedResult<Product>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Product> GetProductDetails(int id)
        {
            var result = await _repo.GetProductDetailsAsync(id);
            _logger.LogInformation("Fetched details for Product {ProductId}.", id);
            return result;
        }

        public async Task<List<Product>> SearchProduct(string name)
        {
            var find = await _repo.SearchProductAsync(name);
            _logger.LogInformation("Search completed for '{Name}'. Found {Count} results.", name, find.Count);
            return find;
        }

        public async Task<List<Product>> GetProductsByCategory(int categoryId)
        {
            _logger.LogInformation("Fetching products for Category {CategoryId}.", categoryId);
            return await _repo.GetProductsByCategoryAsync(categoryId);
        }

        public async Task<List<Category>> GetCategories()
        {
            _logger.LogInformation("Fetching all categories.");
            return await _repo.GetCategoriesAsync();
        }

        public async Task<Category> CreateCategory(string name)
        {
            var trimmed = name?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                throw new ArgumentException("Category name is required.", nameof(name));
            }

            var existing = await _repo.GetCategoriesAsync();
            if (existing.Any(c => c.Name.ToLower() == trimmed.ToLower()))
            {
                throw new InvalidOperationException("Category already exists.");
            }

            var category = new Category { Name = trimmed };
            var created = await _repo.CreateCategoryAsync(category);
            _logger.LogInformation("Category '{CategoryName}' created.", trimmed);
            return created;
        }

        public async Task SubmitProduct(int productId)
        {
            await _repo.SubmitProduct(productId);
            _logger.LogInformation("Product {ProductId} submitted for approval.", productId);
        }

        public async Task ApproveProduct(int productId)
        {
            await _repo.ApproveProductAsync(productId);
            _logger.LogInformation("Product {ProductId} approved.", productId);
        }

        public async Task RejectProduct(int productId)
        {
            await _repo.RejectProductAsync(productId);
            _logger.LogInformation("Product {ProductId} rejected.", productId);
        }

        public async Task UpdatePrice(int productId, decimal newPrice)
        {
            await _repo.UpdatePriceAsync(productId, newPrice);
            _logger.LogInformation("Product {ProductId} price updated to {Price}.", productId, newPrice);
        }

        public async Task UpdateStock(int productId, int newStock)
        {
            await _repo.UpdateStockAsync(productId, newStock);
            _logger.LogInformation("Product {ProductId} stock updated to {Stock}.", productId, newStock);
        }

        public async Task UpdateProduct(int id, ProductCreateDto updatedProduct)
        {
            if (!await _context.Categories.AnyAsync(c => c.Id == updatedProduct.CategoryId))
            {
                _logger.LogWarning("UpdateProduct failed — CategoryId {CategoryId} not found.", updatedProduct.CategoryId);
                throw new CategoryNotFoundException(updatedProduct.CategoryId);
            }

            var product = new Product
            {
                // After editing we re-open the product in Draft state.
                Status = ProductStatus.Draft,
                Name = updatedProduct.Name,
                Description = updatedProduct.Description,
                Price = updatedProduct.Price,
                AvailableQuantity = updatedProduct.Stock,
                CategoryId = updatedProduct.CategoryId,
                Media = updatedProduct.MediaUrls
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => new ProductMedia { MediaUrl = url.Trim(), MediaType = "image" })
                    .ToList(),
                Tags = updatedProduct.Tags,
                Variants = updatedProduct.Variants
                    .Select(v => new ProductVariant
                    {
                        SKU = v.SKU,
                        Price = v.Price,
                        Stock = v.Stock,
                        Attributes = BuildVariantAttributesPayload(v.Attributes, v.ImageUrl)
                    })
                    .ToList()
            };

            await _repo.UpdateProductAsync(id, product);
            _logger.LogInformation("Product {ProductId} updated.", id);
        }

        public async Task DeductStock(int productId, int quantity)  
        {
            await _repo.DeductStockAsync(productId, quantity);
            _logger.LogInformation("Deducted {Quantity} units from Product {ProductId}.", quantity, productId);
        }

        // Variant Methods

        public async Task CreateVariant(ProductVariantCreateDto variantDto)
        {
            var variant = new ProductVariant
            {
                ProductId = variantDto.ProductId,
                SKU = variantDto.SKU,
                Price = variantDto.Price,
                Stock = variantDto.Stock,
                Attributes = BuildVariantAttributesPayload(variantDto.Attributes, variantDto.ImageUrl)
            };

            await _repo.CreateVariantAsync(variant);
            _logger.LogInformation("Variant '{SKU}' created for Product {ProductId}.", variant.SKU, variant.ProductId);
        }

        public async Task<List<ProductVariant>> GetVariantsByProduct(int productId)
        {
            _logger.LogInformation("Fetching variants for Product {ProductId}.", productId);
            return await _repo.GetVariantsByProductAsync(productId);
        }

        public async Task<ProductVariant> GetVariantDetails(int variantId)
        {
            _logger.LogInformation("Fetching details for Variant {VariantId}.", variantId);
            return await _repo.GetVariantDetailsAsync(variantId);
        }

        public async Task UpdateVariant(int variantId, ProductVariantCreateDto updatedVariant)
        {
            var variant = new ProductVariant
            {
                SKU = updatedVariant.SKU,
                Price = updatedVariant.Price,
                Stock = updatedVariant.Stock,
                Attributes = BuildVariantAttributesPayload(updatedVariant.Attributes, updatedVariant.ImageUrl)
            };

            await _repo.UpdateVariantAsync(variantId, variant);
            _logger.LogInformation("Variant {VariantId} updated.", variantId);
        }

        public async Task UpdateVariantPrice(int variantId, decimal newPrice)
        {
            await _repo.UpdateVariantPriceAsync(variantId, newPrice);
            _logger.LogInformation("Variant {VariantId} price updated to {Price}.", variantId, newPrice);
        }

        public async Task UpdateVariantStock(int variantId, int newStock)
        {
            await _repo.UpdateVariantStockAsync(variantId, newStock);
            _logger.LogInformation("Variant {VariantId} stock updated to {Stock}.", variantId, newStock);
        }

        public async Task DeleteVariant(int variantId)
        {
            await _repo.DeleteVariantAsync(variantId);
            _logger.LogInformation("Variant {VariantId} deleted.", variantId);
        }

        public async Task DeductVariantStock(int variantId, int quantity)  
        {
            await _repo.DeductVariantStockAsync(variantId, quantity);
            _logger.LogInformation("Deducted {Quantity} units from Variant {VariantId}.", quantity, variantId);
        }

        private static string? BuildVariantAttributesPayload(string? attributes, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(attributes) && string.IsNullOrWhiteSpace(imageUrl))
            {
                return attributes;
            }

            var payload = new
            {
                details = string.IsNullOrWhiteSpace(attributes) ? null : attributes,
                imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim()
            };

            return JsonSerializer.Serialize(payload);
        }
    }
}