using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Shared.Response;

namespace Aparesk.Eskineria.Application.Features.Management.Abstractions;

public interface IResidentService
{
    Task<PagedResponse<ResidentListItemDto>> GetPagedAsync(GetResidentsRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<ResidentDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DataResponse<ResidentDetailDto>> CreateAsync(CreateResidentRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<ResidentDetailDto>> UpdateAsync(Guid id, UpdateResidentRequest request, CancellationToken cancellationToken = default);
    Task<Response> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
