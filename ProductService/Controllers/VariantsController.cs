using CatalogService.DTO.Products;
using CatalogService.DTO.ProductVariant;
using CatalogService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class VariantsController : ControllerBase
{
    private readonly IProductService _service;

    public VariantsController(IProductService service)
    {
        _service = service;
    }

    // Create Variant
    [Authorize(Roles = "Admin,ContentExecutive,ProductManager")]
    [HttpPost]
    public async Task<IActionResult> Create(ProductVariantCreateDto dto)
    {
        await _service.CreateVariant(dto);
        return Ok("Variant created successfully.");
    }

    // Get all variants for a product
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var variants = await _service.GetVariantsByProduct(productId);
        return Ok(variants);
    }

    // Get single variant
    [HttpGet("{variantId}")]
    public async Task<IActionResult> Get(int variantId)
    {
        var variant = await _service.GetVariantDetails(variantId);
        return Ok(variant);
    }

    // Update variant
    [Authorize(Roles = "Admin,ContentExecutive,ProductManager")]
    [HttpPut("{variantId}")]
    public async Task<IActionResult> Update(int variantId, ProductVariantCreateDto dto)
    {
        await _service.UpdateVariant(variantId, dto);
        return NoContent();
    }

    // Update price
    [Authorize(Roles = "Admin,ProductManager")]
    [HttpPatch("{variantId}/price")]
    public async Task<IActionResult> UpdatePrice(int variantId, [FromQuery] decimal newPrice)
    {
        await _service.UpdateVariantPrice(variantId, newPrice);
        return Ok($"Variant {variantId} price updated to {newPrice}");
    }

    // Update stock
    [Authorize(Roles = "Admin,ProductManager")]
    [HttpPatch("{variantId}/inventory")]
    public async Task<IActionResult> UpdateStock(int variantId, [FromQuery] int quantity)
    {
        await _service.UpdateVariantStock(variantId, quantity);
        return Ok($"Variant {variantId} stock updated to {quantity}");
    }

    // Delete variant
    [Authorize(Roles = "Admin")]
    [HttpDelete("{variantId}")]
    public async Task<IActionResult> Delete(int variantId)
    {
        await _service.DeleteVariant(variantId);
        return NoContent();
    }

    // Deduct stock (for Order Service)
    [Authorize(Roles = "OrderService")]
    [HttpPost("{variantId}/deduct-stock")]
    public async Task<IActionResult> DeductStock(int variantId, [FromQuery] int quantity)
    {
        var success = await _service.DeductVariantStock(variantId, quantity);

        if (!success)
            return BadRequest("Insufficient stock");

        return Ok($"Deducted {quantity} units from variant {variantId}");
    }
}   