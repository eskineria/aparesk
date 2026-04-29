using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using FluentValidation;

namespace Aparesk.Eskineria.Application.Features.Management.Validators;

public sealed class GetSitesRequestValidator : AbstractValidator<GetSitesRequest>
{
    public GetSitesRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("GreaterThan");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 1000).WithMessage("InclusiveBetween");
        RuleFor(x => x.SearchTerm).MaximumLength(200).WithMessage("MaxLength");
    }
}
