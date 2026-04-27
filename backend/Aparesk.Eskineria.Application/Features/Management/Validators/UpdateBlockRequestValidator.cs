using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using FluentValidation;

namespace Aparesk.Eskineria.Application.Features.Management.Validators;

public sealed class UpdateBlockRequestValidator : AbstractValidator<UpdateBlockRequest>
{
    public UpdateBlockRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("RequiredField").MaximumLength(200).WithMessage("MaxLength");
        RuleFor(x => x.FloorCount).GreaterThan(0).WithMessage("GreaterThan").When(x => x.FloorCount.HasValue);
        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("MaxLength");
    }
}
