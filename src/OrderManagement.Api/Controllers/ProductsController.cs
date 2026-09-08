using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.DTOs.Products;
using OrderManagement.Application.Services.Products;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    [HttpGet]
    public async Task<ActionResult<ProductListResponse>> GetList(
        [FromQuery] ProductListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetListAsync(
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }
}