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
public sealed class SitesController : ApiControllerBase
{
    private readonly ISiteService _siteService;

    public SitesController(ISiteService siteService)
    {
        _siteService = siteService;
    }

    [HttpGet]
    [HasPermission("Sites", "Read")]
    public async Task<IActionResult> GetPaged([FromQuery] GetSitesRequest request, CancellationToken cancellationToken)
    {
        var response = await _siteService.GetPagedAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Sites", "Read")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _siteService.GetByIdAsync(id, cancellationToken);
        return FromResponse(response);
    }

    [HttpPost]
    [HasPermission("Sites", "Manage")]
    public async Task<IActionResult> Create([FromBody] CreateSiteRequest request, CancellationToken cancellationToken)
    {
        var response = await _siteService.CreateAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Sites", "Manage")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateSiteRequest request, CancellationToken cancellationToken)
    {
        var response = await _siteService.UpdateAsync(id, request, cancellationToken);
        return FromResponse(response);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("Sites", "Manage")]
    public async Task<IActionResult> Archive([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _siteService.ArchiveAsync(id, cancellationToken);
        return FromResponse(response);
    }
}
