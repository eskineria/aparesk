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
public sealed class GeneralAssembliesController : ApiControllerBase
{
    private readonly IGeneralAssemblyService _assemblyService;

    public GeneralAssembliesController(IGeneralAssemblyService assemblyService)
    {
        _assemblyService = assemblyService;
    }

    [HttpGet]
    [HasPermission("Sites", "Read")]
    public async Task<IActionResult> GetPaged([FromQuery] GetGeneralAssembliesRequest request, CancellationToken cancellationToken)
    {
        var response = await _assemblyService.GetPagedAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Sites", "Read")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _assemblyService.GetByIdAsync(id, cancellationToken);
        return FromResponse(response);
    }

    [HttpPost]
    [HasPermission("Sites", "Manage")]
    public async Task<IActionResult> Create([FromBody] CreateGeneralAssemblyRequest request, CancellationToken cancellationToken)
    {
        var response = await _assemblyService.CreateAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Sites", "Manage")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateGeneralAssemblyRequest request, CancellationToken cancellationToken)
    {
        var response = await _assemblyService.UpdateAsync(id, request, cancellationToken);
        return FromResponse(response);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("Sites", "Manage")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _assemblyService.DeleteAsync(id, cancellationToken);
        return FromResponse(response);
    }
}
