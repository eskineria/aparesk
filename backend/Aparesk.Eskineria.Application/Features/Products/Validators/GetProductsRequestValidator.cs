using Aparesk.Eskineria.Application.Features.Products.Dtos.Requests;
using FluentValidation;

namespace Aparesk.Eskineria.Application.Features.Products.Validators;

public sealed class GetProductsRequestValidator : AbstractValidator<GetProductsRequest>
{
    public GetProductsRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("GreaterThan");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("InclusiveBetween");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("MaxLength");

        RuleFor(x => x.Currency)
            .Length(3)
            .WithMessage("Length")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));

        RuleFor(x => x.MinPrice)
            .LessThanOrEqualTo(x => x.MaxPrice ?? decimal.MaxValue)
            .WithMessage("LessThanOrEqualTo")
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue);
    }
}
