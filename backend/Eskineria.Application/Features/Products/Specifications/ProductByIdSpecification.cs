using Eskineria.Core.Repository.Specification;
using Eskineria.Domain.Entities;

namespace Eskineria.Application.Features.Products.Specifications;

public sealed class ProductByIdSpecification : Specification<Product>
{
    public ProductByIdSpecification(Guid id)
        : base(product => product.Id == id)
    {
        ApplyPaging(0, 1);
    }
}
