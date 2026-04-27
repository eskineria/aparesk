using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;
using Aparesk.Eskineria.Core.Shared.Response;

namespace Aparesk.Eskineria.Application.Features.Management.Abstractions;

public interface IBlockService
{
    Task<PagedResponse<BlockListItemDto>> GetPagedAsync(GetBlocksRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<BlockDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DataResponse<BlockDetailDto>> CreateAsync(CreateBlockRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<BlockDetailDto>> UpdateAsync(Guid id, UpdateBlockRequest request, CancellationToken cancellationToken = default);
    Task<Response> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
