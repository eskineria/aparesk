using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace Eskineria.Core.Versioning.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly Regex SafeTokenNameRegex = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,62}$", RegexOptions.Compiled);
    private static readonly ApiVersion DefaultApiVersion = new(1, 0);

    public const string DefaultApiVersionHeaderName = "X-API-Version";
    public const string DefaultApiVersionQueryParameterName = "api-version";

    /// <summary>
    /// Adds API versioning with multi-strategy readers (URL segment + header + query string).
    /// </summary>
    public static IServiceCollection AddEskineriaVersioning(
        this IServiceCollection services,
        Action<ApiVersioningOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = DefaultApiVersion;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;

            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader(DefaultApiVersionHeaderName),
                new QueryStringApiVersionReader(DefaultApiVersionQueryParameterName)
            );

            configure?.Invoke(options);
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    /// <summary>
    /// Adds API versioning with URL segment-based versioning only.
    /// Example: /api/v1/users
    /// </summary>
    public static IServiceCollection AddEskineriaUrlSegmentVersioning(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddEskineriaVersioning(options =>
        {
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
    }

    /// <summary>
    /// Adds API versioning with header-based versioning only.
    /// Example: X-API-Version: 1.0
    /// </summary>
    public static IServiceCollection AddEskineriaHeaderVersioning(
        this IServiceCollection services,
        string headerName = DefaultApiVersionHeaderName)
    {
        ArgumentNullException.ThrowIfNull(services);
        var normalizedHeaderName = NormalizeVersionTokenName(headerName, nameof(headerName));

        return services.AddEskineriaVersioning(options =>
        {
            options.ApiVersionReader = new HeaderApiVersionReader(normalizedHeaderName);
        });
    }

    /// <summary>
    /// Adds API versioning with query string-based versioning only.
    /// Example: /api/users?api-version=1.0
    /// </summary>
    public static IServiceCollection AddEskineriaQueryStringVersioning(
        this IServiceCollection services,
        string queryParameterName = DefaultApiVersionQueryParameterName)
    {
        ArgumentNullException.ThrowIfNull(services);
        var normalizedQueryParameterName = NormalizeVersionTokenName(queryParameterName, nameof(queryParameterName));

        return services.AddEskineriaVersioning(options =>
        {
            options.ApiVersionReader = new QueryStringApiVersionReader(normalizedQueryParameterName);
        });
    }

    private static string NormalizeVersionTokenName(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Version token name cannot be empty.", parameterName);

        var normalizedValue = value.Trim();
        if (!SafeTokenNameRegex.IsMatch(normalizedValue))
            throw new ArgumentException("Version token name contains invalid characters.", parameterName);

        return normalizedValue;
    }
}
