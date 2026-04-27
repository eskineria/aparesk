using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Shared.Response;

namespace Aparesk.Eskineria.Application.Features.Management.Abstractions;

public interface ISiteService
{
    Task<PagedResponse<SiteListItemDto>> GetPagedAsync(GetSitesRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<SiteDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DataResponse<SiteDetailDto>> CreateAsync(CreateSiteRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<SiteDetailDto>> UpdateAsync(Guid id, UpdateSiteRequest request, CancellationToken cancellationToken = default);
    Task<Response> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
