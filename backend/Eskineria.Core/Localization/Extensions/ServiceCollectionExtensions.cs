using Eskineria.Core.Localization.Abstractions;
using Eskineria.Core.Localization.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Eskineria.Core.Localization.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEskineriaLocalization(this IServiceCollection services)
        => services.AddEskineriaLocalization((Action<EskineriaLocalizationOptions>?)null);

    public static IServiceCollection AddEskineriaLocalization(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Localization")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddEskineriaLocalization(options =>
        {
            configuration.GetSection(sectionName).Bind(options);
        });
    }

    public static IServiceCollection AddEskineriaLocalization(
        this IServiceCollection services,
        Action<EskineriaLocalizationOptions>? configureOptions)
    {
        var options = new EskineriaLocalizationOptions();
        configureOptions?.Invoke(options);
        ValidateAndNormalize(options);

        services.AddLocalization(localizationOptions =>
        {
            localizationOptions.ResourcesPath = options.ResourcesPath;
        });

        services.AddSingleton(Options.Create(options));
        return services;
    }

    public static IServiceCollection AddJsonLocalization(this IServiceCollection services)
        => services.AddJsonLocalization((Action<EskineriaLocalizationOptions>?)null);

    public static IServiceCollection AddJsonLocalization(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Localization")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddJsonLocalization(options =>
        {
            configuration.GetSection(sectionName).Bind(options);
        });
    }

    public static IServiceCollection AddJsonLocalization(
        this IServiceCollection services,
        Action<EskineriaLocalizationOptions>? configureOptions)
    {
        services.AddEskineriaLocalization(configureOptions);
        services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
        return services;
    }

    public static IServiceCollection AddDatabaseLocalization(this IServiceCollection services)
        => services.AddDatabaseLocalization((Action<EskineriaLocalizationOptions>?)null);

    public static IServiceCollection AddDatabaseLocalization(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Localization")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddDatabaseLocalization(options =>
        {
            configuration.GetSection(sectionName).Bind(options);
        });
    }

    public static IServiceCollection AddDatabaseLocalization(
        this IServiceCollection services,
        Action<EskineriaLocalizationOptions>? configureOptions)
    {
        services.AddEskineriaLocalization(configureOptions);
        services.AddSingleton<IStringLocalizerFactory, DatabaseStringLocalizerFactory>();
        services.AddSingleton<ILocalizationCacheInvalidator, DatabaseStringLocalizerCacheInvalidator>();
        services.AddScoped<Services.LocalizationSyncService>();
        return services;
    }

    private static void ValidateAndNormalize(EskineriaLocalizationOptions options)
    {
        options.ResourcesPath = string.IsNullOrWhiteSpace(options.ResourcesPath)
            ? "Localization"
            : options.ResourcesPath.Trim().TrimStart('/', '\\');

        options.DefaultCulture = string.IsNullOrWhiteSpace(options.DefaultCulture)
            ? "en-US"
            : options.DefaultCulture.Trim();

        if (options.SupportedCultures == null || options.SupportedCultures.Length == 0)
        {
            options.SupportedCultures = new[] { options.DefaultCulture };
        }
        else
        {
            options.SupportedCultures = options.SupportedCultures
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (options.SupportedCultures.Length == 0)
            {
                options.SupportedCultures = new[] { options.DefaultCulture };
            }
        }

        if (!options.SupportedCultures.Contains(options.DefaultCulture, StringComparer.OrdinalIgnoreCase))
        {
            options.SupportedCultures = options.SupportedCultures
                .Concat(new[] { options.DefaultCulture })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
