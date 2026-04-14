using Eskineria.Application.Features.Products.Abstractions;
using Eskineria.Application.Features.Products.Dtos.Requests;
using Eskineria.Core.Auth.Authorization;
using Eskineria.Core.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eskineria.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [HasPermission("Products", "Read")]
    public async Task<IActionResult> GetPaged([FromQuery] GetProductsRequest request, CancellationToken cancellationToken)
    {
        var response = await _productService.GetPagedAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Products", "Read")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _productService.GetByIdAsync(id, cancellationToken);
        return FromResponse(response);
    }

    [HttpPost]
    [HasPermission("Products", "Manage")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _productService.CreateAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Products", "Manage")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _productService.UpdateAsync(id, request, cancellationToken);
        return FromResponse(response);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("Products", "Manage")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _productService.DeleteAsync(id, cancellationToken);
        return FromResponse(response);
    }
}
