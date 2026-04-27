using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using FluentValidation;

namespace Aparesk.Eskineria.Application.Features.Management.Validators;

public sealed class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.Number).NotEmpty().WithMessage("RequiredField").MaximumLength(64).WithMessage("MaxLength");
        RuleFor(x => x.DoorNumber).MaximumLength(64).WithMessage("MaxLength");
        RuleFor(x => x.Type).IsInEnum().WithMessage("InvalidEnumValue");
        RuleFor(x => x.GrossAreaSquareMeters).GreaterThanOrEqualTo(0).WithMessage("GreaterThanOrEqualTo").When(x => x.GrossAreaSquareMeters.HasValue);
        RuleFor(x => x.NetAreaSquareMeters).GreaterThanOrEqualTo(0).WithMessage("GreaterThanOrEqualTo").When(x => x.NetAreaSquareMeters.HasValue);
        RuleFor(x => x.LandShare).GreaterThanOrEqualTo(0).WithMessage("GreaterThanOrEqualTo").When(x => x.LandShare.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("MaxLength");
    }
}
