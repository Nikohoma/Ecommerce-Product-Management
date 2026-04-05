// Exceptions/CatalogException.cs
using CatalogService.Models;

namespace CatalogService.Exceptions
{
    public abstract class CatalogException : Exception
    {
        public string Code { get; }
        protected CatalogException(string code, string message) : base(message) => Code = code;
        protected CatalogException(string code, string message, Exception inner) : base(message, inner) => Code = code;
    }

    // 404
    public class ProductNotFoundException : CatalogException
    {
        public int ProductId { get; }
        public ProductNotFoundException(int productId): base("PRODUCT_NOT_FOUND", $"Product {productId} was not found.")=> ProductId = productId;
    }

    public class VariantNotFoundException : CatalogException
    {
        public int VariantId { get; }
        public VariantNotFoundException(int variantId)
            : base("VARIANT_NOT_FOUND", $"Variant {variantId} was not found.")
            => VariantId = variantId;
    }

    // 409
    public class ProductAlreadyExistsException : CatalogException
    {
        public int ProductId { get; }
        public ProductAlreadyExistsException(int productId)
            : base("PRODUCT_ALREADY_EXISTS", $"Product {productId} already exists.")
            => ProductId = productId;
    }

    public class VariantSkuConflictException : CatalogException
    {
        public string SKU { get; }
        public int ProductId { get; }
        public VariantSkuConflictException(string sku, int productId)
            : base("VARIANT_SKU_CONFLICT", $"Variant with SKU '{sku}' already exists for product {productId}.")
            => (SKU, ProductId) = (sku, productId);
    }

    // 422 — lifecycle/state violations
    public class InvalidProductStatusTransitionException : CatalogException
    {
        public ProductStatus Current { get; }
        public ProductStatus Expected { get; }
        public InvalidProductStatusTransitionException(ProductStatus current, ProductStatus expected, string operation)
            : base("INVALID_STATUS_TRANSITION",
                   $"Cannot perform '{operation}': product is '{current}', expected '{expected}'.")
            => (Current, Expected) = (current, expected);
    }

    // 422 — bad input
    public class NegativeStockException : CatalogException
    {
        public NegativeStockException()
            : base("NEGATIVE_STOCK", "Stock quantity cannot be negative.") { }
    }

    public class InsufficientStockException : CatalogException
    {
        public int Available { get; }
        public int Requested { get; }
        public InsufficientStockException(int available, int requested): base("INSUFFICIENT_STOCK",$"Insufficient stock. Available: {available}, Requested: {requested}.")=> (Available, Requested) = (available, requested);
    }
}