using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using FluentValidation;

namespace Aparesk.Eskineria.Application.Features.Management.Validators;

public sealed class CreateSiteRequestValidator : AbstractValidator<CreateSiteRequest>
{
    public CreateSiteRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("RequiredField")
            .MaximumLength(200).WithMessage("MaxLength");

        RuleFor(x => x.TaxNumber).MaximumLength(32).WithMessage("MaxLength");
        RuleFor(x => x.TaxOffice).MaximumLength(100).WithMessage("MaxLength");
        RuleFor(x => x.Phone).MaximumLength(32).WithMessage("MaxLength");
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().WithMessage("Email").When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AddressLine).MaximumLength(500).WithMessage("MaxLength");
        RuleFor(x => x.District).MaximumLength(100).WithMessage("MaxLength");
        RuleFor(x => x.City).MaximumLength(100).WithMessage("MaxLength");
        RuleFor(x => x.PostalCode).MaximumLength(16).WithMessage("MaxLength");
    }
}
