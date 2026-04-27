using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using FluentValidation;

namespace Aparesk.Eskineria.Application.Features.Management.Validators;

public sealed class GetBlocksRequestValidator : AbstractValidator<GetBlocksRequest>
{
    public GetBlocksRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("GreaterThan");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("InclusiveBetween");
        RuleFor(x => x.SearchTerm).MaximumLength(200).WithMessage("MaxLength");
    }
}
