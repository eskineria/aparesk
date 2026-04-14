using Eskineria.Core.Notifications.Email;
using Microsoft.Extensions.Options;

namespace Eskineria.Core.Auth.Services;

public sealed class AuthEmailTemplateHelper
{
    private readonly IEmailTemplateProvider _emailTemplateProvider;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly EmailOptions _emailOptions;

    public AuthEmailTemplateHelper(
        IEmailTemplateProvider emailTemplateProvider,
        ITemplateRenderer templateRenderer,
        IOptions<EmailOptions> emailOptions)
    {
        _emailTemplateProvider = emailTemplateProvider;
        _templateRenderer = templateRenderer;
        _emailOptions = emailOptions.Value;
    }

    public async Task<LoadedEmailTemplate> LoadTemplateAsync(string templateKey, string fallbackFileName, string? recipient = null)
    {
        var template = await _emailTemplateProvider.GetActiveTemplateAsync(templateKey, recipient);
        if (template != null && !string.IsNullOrWhiteSpace(template.Body))
        {
            return new LoadedEmailTemplate(
                template.Subject,
                template.Body,
                string.IsNullOrWhiteSpace(template.TrackingKey) ? template.Key : template.TrackingKey,
                template.Culture);
        }

        var fallbackBody = await LoadTemplateFromFileAsync(fallbackFileName);
        return new LoadedEmailTemplate(string.Empty, fallbackBody, templateKey, string.Empty);
    }

    public async Task<string> RenderSubjectAsync<T>(string? subjectTemplate, T model, string fallbackSubject)
    {
        if (string.IsNullOrWhiteSpace(subjectTemplate))
        {
            return fallbackSubject;
        }

        try
        {
            var rendered = await _templateRenderer.RenderAsync(subjectTemplate, model);
            return string.IsNullOrWhiteSpace(rendered) ? fallbackSubject : rendered.Trim();
        }
        catch
        {
            return fallbackSubject;
        }
    }

    public async Task<string> RenderBodyAsync<T>(string templateBody, T model, string fallbackBody)
    {
        if (string.IsNullOrWhiteSpace(templateBody))
        {
            return fallbackBody;
        }

        try
        {
            var rendered = await _templateRenderer.RenderAsync(templateBody, model);
            return string.IsNullOrWhiteSpace(rendered) ? fallbackBody : rendered;
        }
        catch
        {
            return fallbackBody;
        }
    }

    private async Task<string> LoadTemplateFromFileAsync(string templateName)
    {
        try
        {
            var templatePath = _emailOptions.TemplatePath ?? "EmailTemplates";
            var fileName = Path.Combine(AppContext.BaseDirectory, templatePath, $"{templateName}.sbn");

            if (File.Exists(fileName))
            {
                return await File.ReadAllTextAsync(fileName);
            }

            var devFileName = Path.Combine(templatePath, $"{templateName}.sbn");
            if (File.Exists(devFileName))
            {
                return await File.ReadAllTextAsync(devFileName);
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    public sealed record LoadedEmailTemplate(string Subject, string Body, string TrackingKey, string Culture);
}
