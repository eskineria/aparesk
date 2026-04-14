using Eskineria.Application.Features.Products.Dtos.Requests;
using Eskineria.Application.Features.Products.Dtos.Responses;
using Eskineria.Core.Shared.Response;

namespace Eskineria.Application.Features.Products.Abstractions;

public interface IProductService
{
    Task<PagedResponse<ProductListItemDto>> GetPagedAsync(GetProductsRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<ProductDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DataResponse<ProductDetailDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<DataResponse<ProductDetailDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<Response> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
