using Eskineria.Core.Auth.Constants;
using Eskineria.Core.Auth.Models;
using FluentValidation;

namespace Eskineria.Core.Auth.Validators;

public sealed class PagedRequestValidator : AbstractValidator<PagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage(AuthLocalizationKeys.AccessControlPageNumberGreaterThanZero);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200).WithMessage(AuthLocalizationKeys.AccessControlPageSizeBetweenOneAndTwoHundred);

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200).WithMessage(AuthLocalizationKeys.AccessControlSearchTermMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));
    }
}

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(AuthLocalizationKeys.AccessControlRoleNameRequired)
            .MaximumLength(100).WithMessage(AuthLocalizationKeys.AccessControlRoleNameMaxLength);
    }
}

public sealed class UpdateUserRolesRequestValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(AuthLocalizationKeys.AccessControlUserIdRequired);

        RuleFor(x => x.Roles)
            .NotNull().WithMessage(AuthLocalizationKeys.AccessControlRolesRequired)
            .Must(roles => roles != null && roles.Count > 0).WithMessage(AuthLocalizationKeys.AccessControlAtLeastOneRoleRequired);

        RuleForEach(x => x.Roles)
            .NotEmpty().WithMessage(AuthLocalizationKeys.AccessControlRoleNameCannotBeEmpty)
            .MaximumLength(100).WithMessage(AuthLocalizationKeys.AccessControlRoleNameMaxLength);
    }
}

public sealed class UpdateRolePermissionsRequestValidator : AbstractValidator<UpdateRolePermissionsRequest>
{
    public UpdateRolePermissionsRequestValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage(AuthLocalizationKeys.AccessControlRoleNameRequired)
            .MaximumLength(100).WithMessage(AuthLocalizationKeys.AccessControlRoleNameMaxLength);

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage(AuthLocalizationKeys.AccessControlPermissionsRequired);

        RuleForEach(x => x.Permissions)
            .NotEmpty().WithMessage(AuthLocalizationKeys.AccessControlPermissionCannotBeEmpty)
            .MaximumLength(150).WithMessage(AuthLocalizationKeys.AccessControlPermissionMaxLength);
    }
}

public sealed class UpdateUserStatusRequestValidator : AbstractValidator<UpdateUserStatusRequest>
{
    public UpdateUserStatusRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(AuthLocalizationKeys.AccessControlUserIdRequired);
    }
}
