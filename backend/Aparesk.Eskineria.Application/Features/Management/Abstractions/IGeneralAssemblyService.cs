using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Shared.Response;

namespace Aparesk.Eskineria.Application.Features.Management.Abstractions;

public interface IGeneralAssemblyService
{
    Task<PagedResponse<GeneralAssemblyListItemDto>> GetPagedAsync(GetGeneralAssembliesRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<GeneralAssemblyDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DataResponse<GeneralAssemblyDetailDto>> CreateAsync(CreateGeneralAssemblyRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<GeneralAssemblyDetailDto>> UpdateAsync(Guid id, UpdateGeneralAssemblyRequest request, CancellationToken cancellationToken = default);
    Task<Response> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
