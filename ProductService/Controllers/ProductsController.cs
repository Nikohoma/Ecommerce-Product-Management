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

    //[HttpGet]
    //public async Task<IActionResult> GetAll()
    //{
    //    var products = await _service.GetAllProducts();
    //    return Ok(products);
    //}


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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, ProductCreateDto updatedProduct)
    {
        await _service.UpdateProduct(id, updatedProduct);

        return NoContent();
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
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

    [HttpGet]
    public async Task<IActionResult> GetProducts(
    [FromQuery] string? search,
    [FromQuery] int? categoryId)
    {
        if (!string.IsNullOrEmpty(search))
            return Ok(await _service.SearchProduct(search));

        if (categoryId.HasValue)
            return Ok(await _service.GetProductsByCategory(categoryId.Value));

        return Ok(await _service.GetAllProducts());
    }

}