using Eskineria.Application.Features.Products.Dtos.Requests;
using FluentValidation;

namespace Eskineria.Application.Features.Products.Validators;

public sealed class AdjustProductStockRequestValidator : AbstractValidator<AdjustProductStockRequest>
{
    public AdjustProductStockRequestValidator()
    {
        RuleFor(x => x.QuantityDelta)
            .NotEqual(0)
            .WithMessage("NotEqual");
    }
}
