using Eskineria.Core.Auth.Models;
using FluentValidation;

namespace Eskineria.Core.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("RequiredField")
            .EmailAddress().WithMessage("EmailInvalid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("RequiredField");

        RuleFor(x => x.MfaCode)
            .Matches(@"^\d{6}$").WithMessage("InvalidRequest")
            .When(x => !string.IsNullOrWhiteSpace(x.MfaCode));
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("RequiredField")
            .MaximumLength(100).WithMessage("MaxLength");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("RequiredField")
            .MaximumLength(100).WithMessage("MaxLength");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("RequiredField")
            .EmailAddress().WithMessage("EmailInvalid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("RequiredField")
            .MaximumLength(128).WithMessage("MaxLength");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("CompareMismatch");

    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("RequiredField");
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("RequiredField").MaximumLength(128).WithMessage("MaxLength");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("CompareMismatch");
    }
}

public class UpdateMfaRequestValidator : AbstractValidator<UpdateMfaRequest>
{
    public UpdateMfaRequestValidator()
    {
        // Validation logic is handled in the controller based on user type (password vs social)
        // We allow both to be empty here because either one or the other will be required.
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("RequiredField").EmailAddress().WithMessage("EmailInvalid");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("RequiredField").EmailAddress().WithMessage("EmailInvalid");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("RequiredField")
            .Length(6).WithMessage("EmailVerificationCodeLength")
            .Matches("^[0-9]{6}$").WithMessage("EmailVerificationCodeFormat");
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("RequiredField").MaximumLength(128).WithMessage("MaxLength");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("CompareMismatch");
    }
}

public class VerifyPasswordResetCodeRequestValidator : AbstractValidator<VerifyPasswordResetCodeRequest>
{
    public VerifyPasswordResetCodeRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("RequiredField")
            .EmailAddress().WithMessage("EmailInvalid");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("RequiredField")
            .Length(6).WithMessage("EmailVerificationCodeLength")
            .Matches("^[0-9]{6}$").WithMessage("EmailVerificationCodeFormat");
    }
}

public class ResendPasswordResetCodeRequestValidator : AbstractValidator<ResendPasswordResetCodeRequest>
{
    public ResendPasswordResetCodeRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("RequiredField")
            .EmailAddress().WithMessage("EmailInvalid");
    }
}

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("RequiredField");
        RuleFor(x => x.Token).NotEmpty().WithMessage("RequiredField");
    }
}

public class VerifyEmailCodeRequestValidator : AbstractValidator<VerifyEmailCodeRequest>
{
    public VerifyEmailCodeRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("RequiredField")
            .EmailAddress().WithMessage("EmailInvalid");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("RequiredField")
            .Length(6).WithMessage("EmailVerificationCodeLength")
            .Matches("^[0-9]{6}$").WithMessage("EmailVerificationCodeFormat");
    }
}

public class ResendEmailVerificationCodeRequestValidator : AbstractValidator<ResendEmailVerificationCodeRequest>
{
    public ResendEmailVerificationCodeRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("RequiredField")
            .EmailAddress().WithMessage("EmailInvalid");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("RequiredField");
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("RequiredField");
    }
}

public class UpdateUserInfoRequestValidator : AbstractValidator<UpdateUserInfoRequest>
{
    public UpdateUserInfoRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("RequiredField").MaximumLength(100).WithMessage("MaxLength");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("RequiredField").MaximumLength(100).WithMessage("MaxLength");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("RequiredField")
            .EmailAddress().WithMessage("EmailInvalid");
    }
}

public class SocialLoginRequestValidator : AbstractValidator<SocialLoginRequest>
{
    public SocialLoginRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("RequiredField")
            .Must(provider => string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
            .WithMessage("InvalidRequest");

        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("RequiredField");
    }
}
