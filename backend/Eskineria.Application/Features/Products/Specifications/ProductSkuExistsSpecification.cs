using Eskineria.Core.Repository.Specification;
using Eskineria.Domain.Entities;

namespace Eskineria.Application.Features.Products.Specifications;

public sealed class ProductSkuExistsSpecification : Specification<Product>
{
    public ProductSkuExistsSpecification(string sku, Guid? excludingId = null)
        : base(product => product.Sku == sku && (!excludingId.HasValue || product.Id != excludingId.Value))
    {
        ApplyPaging(0, 1);
    }
}
