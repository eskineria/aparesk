using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using FluentValidation;

namespace Aparesk.Eskineria.Application.Features.Management.Validators;

public sealed class CreateBlockRequestValidator : AbstractValidator<CreateBlockRequest>
{
    public CreateBlockRequestValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("RequiredField");
        RuleFor(x => x.Name).NotEmpty().WithMessage("RequiredField").MaximumLength(200).WithMessage("MaxLength");
        RuleFor(x => x.FloorCount).NotNull().WithMessage("RequiredField").GreaterThan(0).WithMessage("GreaterThan");
        RuleFor(x => x.UnitsPerFloor).NotNull().WithMessage("RequiredField").GreaterThan(0).WithMessage("GreaterThan");
        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("MaxLength");
    }
}
