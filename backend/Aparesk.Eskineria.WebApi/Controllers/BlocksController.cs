using Aparesk.Eskineria.Application.Features.Management.Abstractions;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Core.Auth.Authorization;
using Aparesk.Eskineria.Core.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aparesk.Eskineria.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class BlocksController : ApiControllerBase
{
    private readonly IBlockService _blockService;

    public BlocksController(IBlockService blockService)
    {
        _blockService = blockService;
    }

    [HttpGet]
    [HasPermission("Blocks", "Read")]
    public async Task<IActionResult> GetPaged([FromQuery] GetBlocksRequest request, CancellationToken cancellationToken)
    {
        var response = await _blockService.GetPagedAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Blocks", "Read")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _blockService.GetByIdAsync(id, cancellationToken);
        return FromResponse(response);
    }

    [HttpPost]
    [HasPermission("Blocks", "Manage")]
    public async Task<IActionResult> Create([FromBody] CreateBlockRequest request, CancellationToken cancellationToken)
    {
        var response = await _blockService.CreateAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Blocks", "Manage")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateBlockRequest request, CancellationToken cancellationToken)
    {
        var response = await _blockService.UpdateAsync(id, request, cancellationToken);
        return FromResponse(response);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("Blocks", "Manage")]
    public async Task<IActionResult> Archive([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _blockService.ArchiveAsync(id, cancellationToken);
        return FromResponse(response);
    }
}
