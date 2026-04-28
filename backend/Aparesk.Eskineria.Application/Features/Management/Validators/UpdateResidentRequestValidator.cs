using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using FluentValidation;

namespace Aparesk.Eskineria.Application.Features.Management.Validators;

public sealed class UpdateResidentRequestValidator : AbstractValidator<UpdateResidentRequest>
{
    public UpdateResidentRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("RequiredField").MaximumLength(100).WithMessage("MaxLength");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("RequiredField").MaximumLength(100).WithMessage("MaxLength");
        RuleFor(x => x.IdentityNumber).MaximumLength(32).WithMessage("MaxLength");
        RuleFor(x => x.Type).IsInEnum().WithMessage("InvalidEnumValue");
        RuleFor(x => x.Phone).MaximumLength(32).WithMessage("MaxLength");
        RuleFor(x => x.Email).MaximumLength(256).WithMessage("MaxLength").EmailAddress().WithMessage("InvalidEmail").When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Occupation).MaximumLength(150).WithMessage("MaxLength");
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("MaxLength");
        RuleFor(x => x.MoveOutDate)
            .GreaterThanOrEqualTo(x => x.MoveInDate)
            .WithMessage("GreaterThanOrEqualTo")
            .When(x => x.MoveInDate.HasValue && x.MoveOutDate.HasValue);
    }
}
