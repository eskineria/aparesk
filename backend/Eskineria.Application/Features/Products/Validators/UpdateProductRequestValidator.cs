using Eskineria.Application.Features.Products.Dtos.Requests;
using Eskineria.Application.Features.Products.Specifications;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Eskineria.Persistence.Features.Products.Abstractions;

namespace Eskineria.Application.Features.Products.Validators;

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator(
        IProductRepository productRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("RequiredField")
            .MaximumLength(64).WithMessage("MaxLength")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("RegularExpression")
            .MustAsync(async (sku, cancellationToken) =>
            {
                var routeIdValue = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                if (!Guid.TryParse(routeIdValue, out var routeId))
                {
                    return false;
                }

                var specification = new ProductSkuExistsSpecification(sku, routeId);
                var existing = await productRepository.GetListAsync(specification, cancellationToken);
                return existing.Count == 0;
            })
            .WithMessage("ProductSkuAlreadyExists");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("RequiredField")
            .MaximumLength(200).WithMessage("MaxLength");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("MaxLength");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("GreaterThanOrEqualTo");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("RequiredField")
            .Length(3).WithMessage("Length")
            .Matches("^[A-Za-z]{3}$").WithMessage("RegularExpression");
    }
}
