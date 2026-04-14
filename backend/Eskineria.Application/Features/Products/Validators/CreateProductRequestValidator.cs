using Eskineria.Application.Features.Products.Dtos.Requests;
using Eskineria.Application.Features.Products.Specifications;
using FluentValidation;
using Eskineria.Persistence.Features.Products.Abstractions;

namespace Eskineria.Application.Features.Products.Validators;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator(IProductRepository productRepository)
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("RequiredField")
            .MaximumLength(64).WithMessage("MaxLength")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("RegularExpression")
            .MustAsync(async (sku, cancellationToken) =>
            {
                var specification = new ProductSkuExistsSpecification(sku);
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

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("GreaterThanOrEqualTo");
    }
}
