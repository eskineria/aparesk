using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;

namespace Eskineria.Core.Validation.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private const int MaxValidationFields = 200;
    private const int MaxErrorsPerField = 10;
    private readonly IStringLocalizer<ValidationFilter> _localizer;
    private const string DefaultTitle = "Validation Failed";
    private const string DefaultDetail = "One or more validation errors occurred.";

    public ValidationFilter(IStringLocalizer<ValidationFilter> localizer)
    {
        _localizer = localizer;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Take(MaxValidationFields)
                .ToDictionary(
                    kvp => NormalizeModelStateKey(kvp.Key),
                    kvp => kvp.Value?.Errors
                        .Take(MaxErrorsPerField)
                        .Select(e => LocalizeError(kvp.Key, e.ErrorMessage))
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .Distinct(StringComparer.Ordinal)
                        .ToArray() ?? Array.Empty<string>()
                , StringComparer.Ordinal);

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Title = GetLocalizedOrDefault("ValidationErrorTitle", DefaultTitle),
                Status = 400,
                Detail = GetLocalizedOrDefault("ValidationErrorDetail", DefaultDetail),
                Instance = context.HttpContext.Request.Path
            };
            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }

        await next();
    }

    private string LocalizeError(string modelStateKey, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return GetLocalizedOrDefault("ValidationErrorDetail", DefaultDetail);

        var propertyName = ExtractPropertyName(modelStateKey);

        var localizedPropertyName = string.IsNullOrWhiteSpace(propertyName)
            ? propertyName
            : GetLocalizedOrDefault(propertyName, propertyName);

        var localizedMessage = _localizer[message, localizedPropertyName, string.Empty, string.Empty, string.Empty];
        if (!localizedMessage.ResourceNotFound && !string.IsNullOrWhiteSpace(localizedMessage.Value))
            return localizedMessage.Value;

        return message;
    }

    private static string NormalizeModelStateKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "$";

        return key.Trim();
    }

    private static string ExtractPropertyName(string? modelStateKey)
    {
        if (string.IsNullOrWhiteSpace(modelStateKey))
            return string.Empty;

        var span = modelStateKey.AsSpan().Trim();
        var separatorIndex = span.LastIndexOf('.');
        return separatorIndex >= 0
            ? span[(separatorIndex + 1)..].ToString()
            : span.ToString();
    }

    private string GetLocalizedOrDefault(string key, string fallback)
    {
        var localized = _localizer[key];
        if (!localized.ResourceNotFound && !string.IsNullOrWhiteSpace(localized.Value))
            return localized.Value;

        return fallback;
    }
}
