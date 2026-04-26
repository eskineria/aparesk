using Aparesk.Eskineria.Application.Features.Products.Dtos.Requests;
using Aparesk.Eskineria.Core.Repository.Specification;
using Aparesk.Eskineria.Domain.Entities;

namespace Aparesk.Eskineria.Application.Features.Products.Specifications;

public sealed class ProductPagedSpecification : Specification<Product>
{
    public ProductPagedSpecification(GetProductsRequest request, int skip, int take)
        : base(CreateCriteria(request))
    {
        ApplyOrderBy(product => product.Name);
        ApplyPaging(skip, take);
    }

    private static System.Linq.Expressions.Expression<Func<Product, bool>> CreateCriteria(GetProductsRequest request)
    {
        var normalizedCurrency = string.IsNullOrWhiteSpace(request.Currency)
            ? null
            : request.Currency.Trim().ToUpperInvariant();
        var normalizedSearchTerm = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : request.SearchTerm.Trim();

        return product =>
            (request.IncludeArchived || !product.IsArchived) &&
            (!request.IsActive.HasValue || product.IsActive == request.IsActive.Value) &&
            (!request.MinPrice.HasValue || product.Price >= request.MinPrice.Value) &&
            (!request.MaxPrice.HasValue || product.Price <= request.MaxPrice.Value) &&
            (normalizedCurrency == null || product.Currency == normalizedCurrency) &&
            (normalizedSearchTerm == null || product.Name.Contains(normalizedSearchTerm) || product.Sku.Contains(normalizedSearchTerm));
    }
}
