using Aparesk.Eskineria.Application.Features.Products.Dtos.Requests;
using FluentValidation;

namespace Aparesk.Eskineria.Application.Features.Products.Validators;

public sealed class AdjustProductStockRequestValidator : AbstractValidator<AdjustProductStockRequest>
{
    public AdjustProductStockRequestValidator()
    {
        RuleFor(x => x.QuantityDelta)
            .NotEqual(0)
            .WithMessage("NotEqual");
    }
}
