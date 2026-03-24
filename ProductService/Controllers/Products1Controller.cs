using CatalogService.Data;
using CatalogService.DTO.Products;
using CatalogService.Models;
using CatalogService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class Products1Controller : ControllerBase
{
    private readonly IProductService _service;

    public Products1Controller(IProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _service.GetAllProducts();
        return Ok(products);
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
        return Ok();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, ProductCreateDto updatedProduct)
    {
        await _service.UpdateProduct(id, updatedProduct);

        return Ok(updatedProduct);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = _service.GetProductDetails(id);
        return Ok(product);
    }
}