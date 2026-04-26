using Aparesk.Eskineria.Core.Notifications.Abstractions;
using Aparesk.Eskineria.Core.Notifications.Email;
using Aparesk.Eskineria.Core.Notifications.Providers;
using Aparesk.Eskineria.Core.Notifications.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Aparesk.Eskineria.Core.Notifications.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEskineriaNotifications(this IServiceCollection services)
    {
        services.TryAddScoped<INotificationService, CompositeNotificationService>();
        services.TryAddScoped<INotificationDeliveryStore, PersistentNotificationDeliveryStore>();
        return services;
    }

    public static IServiceCollection AddEmailChannel(
        this IServiceCollection services, 
        IConfiguration configuration,
        string sectionName = "Email")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new EmailOptions();
        configuration.GetSection(sectionName).Bind(options);
        ValidateAndNormalize(options);

        services.AddSingleton(Options.Create(options));
        services.TryAddSingleton<ITemplateRenderer, ScribanTemplateRenderer>();
        services.TryAddScoped<IEmailTemplateProvider, NullEmailTemplateProvider>();
        services.TryAddScoped<IEmailSender, SmtpEmailSender>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<INotificationProvider, EmailNotificationProvider>());

        return services;
    }

    private static void ValidateAndNormalize(EmailOptions options)
    {
        options.SmtpHost = options.SmtpHost?.Trim() ?? string.Empty;
        options.SmtpUser = options.SmtpUser?.Trim() ?? string.Empty;
        options.SmtpPass = options.SmtpPass ?? string.Empty;
        options.FromAddress = options.FromAddress?.Trim() ?? string.Empty;
        options.FromName = string.IsNullOrWhiteSpace(options.FromName)
            ? options.FromAddress
            : options.FromName.Trim();
        options.TemplatePath = string.IsNullOrWhiteSpace(options.TemplatePath)
            ? "EmailTemplates"
            : options.TemplatePath.Trim().TrimStart('/', '\\');

        if (string.IsNullOrWhiteSpace(options.SmtpHost))
            throw new InvalidOperationException("EmailOptions.SmtpHost is required.");

        if (options.SmtpPort <= 0 || options.SmtpPort > 65535)
            throw new InvalidOperationException("EmailOptions.SmtpPort must be between 1 and 65535.");

        if (string.IsNullOrWhiteSpace(options.FromAddress))
            throw new InvalidOperationException("EmailOptions.FromAddress is required.");

        // Validate eagerly to fail-fast on invalid startup configuration.
        options.FromAddress = NotificationSecurity.NormalizeAndValidateEmail(options.FromAddress, nameof(options.FromAddress));
    }
}
