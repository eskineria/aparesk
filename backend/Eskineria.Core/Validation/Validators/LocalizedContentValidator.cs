using Eskineria.Core.Shared.Localization;
using FluentValidation;
using System.Globalization;

namespace Eskineria.Core.Validation.Validators;

public class LocalizedContentValidator : AbstractValidator<LocalizedContent>
{
    private const int MaxCultureKeyLength = 16;
    private const int MaxLocalizedTextLength = 4000;

    public LocalizedContentValidator()
    {
        RuleFor(x => x)
            .Must(x => x != null && x.Count > 0)
            .WithMessage("At least one language must be provided.");

        RuleForEach(x => x)
            .Must(entry => IsValidCultureKey(entry.Key))
            .WithMessage("Culture key is invalid.");

        RuleForEach(x => x)
            .Must(entry => !string.IsNullOrWhiteSpace(entry.Value))
            .WithMessage("Localized text cannot be empty.");

        RuleForEach(x => x)
            .Must(entry => entry.Key.Trim().Length <= MaxCultureKeyLength)
            .WithMessage($"Culture key length cannot exceed {MaxCultureKeyLength} characters.");

        RuleForEach(x => x)
            .Must(entry => entry.Value.Trim().Length <= MaxLocalizedTextLength)
            .WithMessage($"Localized text cannot exceed {MaxLocalizedTextLength} characters.");

        RuleForEach(x => x)
            .Must(entry => !ContainsControlCharacters(entry.Value))
            .WithMessage("Localized text contains invalid control characters.");
    }

    private static bool IsValidCultureKey(string? cultureKey)
    {
        if (string.IsNullOrWhiteSpace(cultureKey))
            return false;

        var normalized = cultureKey.Trim().Replace('_', '-');
        try
        {
            _ = CultureInfo.GetCultureInfo(normalized);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private static bool ContainsControlCharacters(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t');
    }
}
