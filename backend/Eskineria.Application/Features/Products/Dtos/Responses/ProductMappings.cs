using Eskineria.Application.Features.Products.Dtos.Requests;
using Eskineria.Domain.Entities;
using Mapster;

namespace Eskineria.Application.Features.Products.Dtos.Responses;

public class ProductMappings : Eskineria.Core.Mapping.Abstractions.IMapFrom<Product>
{
    public void Mapping(TypeAdapterConfig config)
    {
        config.NewConfig<CreateProductRequest, Product>();
        config.NewConfig<UpdateProductRequest, Product>();
        config.NewConfig<Product, ProductListItemDto>();
        config.NewConfig<Product, ProductDetailDto>();
    }
}
