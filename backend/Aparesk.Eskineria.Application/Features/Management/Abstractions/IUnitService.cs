using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Shared.Response;

namespace Aparesk.Eskineria.Application.Features.Management.Abstractions;

public interface IUnitService
{
    Task<PagedResponse<UnitListItemDto>> GetPagedAsync(GetUnitsRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<UnitDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DataResponse<UnitDetailDto>> CreateAsync(CreateUnitRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<UnitDetailDto>> UpdateAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default);
    Task<Response> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
