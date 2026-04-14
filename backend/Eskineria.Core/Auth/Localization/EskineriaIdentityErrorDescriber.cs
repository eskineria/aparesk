using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Eskineria.Core.Auth.Localization;

public class EskineriaIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly IStringLocalizer<EskineriaIdentityErrorDescriber> _localizer;

    public EskineriaIdentityErrorDescriber(IStringLocalizer<EskineriaIdentityErrorDescriber> localizer)
    {
        _localizer = localizer;
    }

    public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = _localizer["IdentityDefaultError"] };
    public override IdentityError ConcurrencyFailure() => new() { Code = nameof(ConcurrencyFailure), Description = _localizer["IdentityConcurrencyFailure"] };
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = _localizer["IdentityPasswordMismatch"] };
    public override IdentityError InvalidToken() => new() { Code = nameof(InvalidToken), Description = _localizer["IdentityInvalidToken"] };
    public override IdentityError LoginAlreadyAssociated() => new() { Code = nameof(LoginAlreadyAssociated), Description = _localizer["IdentityLoginAlreadyAssociated"] };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = _localizer["IdentityInvalidUserName", userName ?? ""] };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = _localizer["IdentityInvalidEmail", email ?? ""] };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = _localizer["IdentityDuplicateUserName", userName] };
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = _localizer["IdentityDuplicateEmail", email] };
    public override IdentityError InvalidRoleName(string? roleName) => new() { Code = nameof(InvalidRoleName), Description = _localizer["IdentityInvalidRoleName", roleName ?? ""] };
    public override IdentityError DuplicateRoleName(string roleName) => new() { Code = nameof(DuplicateRoleName), Description = _localizer["IdentityDuplicateRoleName", roleName] };
    public override IdentityError UserAlreadyHasPassword() => new() { Code = nameof(UserAlreadyHasPassword), Description = _localizer["IdentityUserAlreadyHasPassword"] };
    public override IdentityError UserLockoutNotEnabled() => new() { Code = nameof(UserLockoutNotEnabled), Description = _localizer["IdentityUserLockoutNotEnabled"] };
    public override IdentityError UserAlreadyInRole(string role) => new() { Code = nameof(UserAlreadyInRole), Description = _localizer["IdentityUserAlreadyInRole", role] };
    public override IdentityError UserNotInRole(string role) => new() { Code = nameof(UserNotInRole), Description = _localizer["IdentityUserNotInRole", role] };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = _localizer["IdentityPasswordTooShort", length] };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = _localizer["IdentityPasswordRequiresNonAlphanumeric"] };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = _localizer["IdentityPasswordRequiresDigit"] };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = _localizer["IdentityPasswordRequiresLower"] };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = _localizer["IdentityPasswordRequiresUpper"] };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = _localizer["IdentityPasswordRequiresUniqueChars", uniqueChars] };
    public override IdentityError RecoveryCodeRedemptionFailed() => new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = _localizer["IdentityRecoveryCodeRedemptionFailed"] };
}
