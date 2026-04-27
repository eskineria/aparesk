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
public sealed class ResidentsController : ApiControllerBase
{
    private readonly IResidentService _residentService;

    public ResidentsController(IResidentService residentService)
    {
        _residentService = residentService;
    }

    [HttpGet]
    [HasPermission("Residents", "Read")]
    public async Task<IActionResult> GetPaged([FromQuery] GetResidentsRequest request, CancellationToken cancellationToken)
    {
        var response = await _residentService.GetPagedAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Residents", "Read")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _residentService.GetByIdAsync(id, cancellationToken);
        return FromResponse(response);
    }

    [HttpPost]
    [HasPermission("Residents", "Manage")]
    public async Task<IActionResult> Create([FromBody] CreateResidentRequest request, CancellationToken cancellationToken)
    {
        var response = await _residentService.CreateAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Residents", "Manage")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateResidentRequest request, CancellationToken cancellationToken)
    {
        var response = await _residentService.UpdateAsync(id, request, cancellationToken);
        return FromResponse(response);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("Residents", "Manage")]
    public async Task<IActionResult> Archive([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _residentService.ArchiveAsync(id, cancellationToken);
        return FromResponse(response);
    }
}
