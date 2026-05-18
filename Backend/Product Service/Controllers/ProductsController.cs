using CatalogService.Data;
using CatalogService.DTO.Products;
using CatalogService.Models;
using CatalogService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }


    [Authorize(Roles = "Admin,ContentExecutive,ProductManager")]
    [HttpPost]
    public async Task<IActionResult> Create(ProductCreateDto product)
    {
        await _service.CreateProduct(product);
        return Ok(product);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteProduct(id);
        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,ContentExecutive,ProductManager")]
    public async Task<IActionResult> Update(int id, ProductCreateDto updatedProduct)
    {
        await _service.UpdateProduct(id, updatedProduct);

        return NoContent();
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _service.GetProductDetails(id);
        return Ok(product);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Search query is required");

        var results = await _service.SearchProduct(query);
        return Ok(results);
    }

    [HttpGet("category/{categoryId}")]
    public async Task<IActionResult> GetProductsByCategory(int categoryId)
    {
        try
        {
            var products = await _service.GetProductsByCategory(categoryId);
            return Ok(products);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _service.GetCategories();
        return Ok(categories);
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Admin,ProductManager")]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest("Category name is required.");
        }

        try
        {
            var created = await _service.CreateCategory(dto.Name);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? search, [FromQuery] int? categoryId,[FromQuery] ProductStatus? status,[FromQuery] int page = 1,[FromQuery] int pageSize = 10)
    {
        if (!string.IsNullOrEmpty(search))
            return Ok(await _service.SearchProduct(search));

        if (categoryId.HasValue)
            return Ok(await _service.GetProductsByCategory(categoryId.Value));

        return Ok(await _service.GetPaginatedProducts(page, pageSize, status));
    }

    [Authorize(Roles = "Admin,ProductManager")]
    [HttpPatch("{id}/price")]
    public async Task<IActionResult> UpdatePrice(int id, [FromQuery] decimal newPrice)
    {
        await _service.UpdatePrice(id, newPrice);
        return Ok(new { message = $"Product {id} price updated to {newPrice}" });
    }

    [Authorize(Roles = "Admin,ProductManager")]
    [HttpPatch("{id}/inventory")]
    public async Task<IActionResult> UpdateStock(int id, [FromQuery] int quantity)
    {
        await _service.UpdateStock(id, quantity);
        return Ok(new { message = $"Product {id} stock updated to {quantity}" });
    }

    [HttpPost("{id}/deduct-stock")]
    [Authorize(Roles = "OrderService")]
    public async Task<IActionResult> DeductStock(int id, [FromQuery] int quantity)
    {
        await _service.DeductStock(id, quantity);
        return Ok($"Deducted {quantity} units from product {id}.");
    }

}