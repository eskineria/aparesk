using Eskineria.Core.Storage.Abstractions;
using Eskineria.Core.Storage.Configuration;
using Eskineria.Core.Storage.Implementations;
using Eskineria.Core.Storage.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Eskineria.Core.Storage.Extensions;

public static class ServiceCollectionExtensions
{
    private const long DefaultMaxFileSizeBytes = 5 * 1024 * 1024;
    private const long MaxAllowedFileSizeBytes = 512L * 1024 * 1024; // 512 MB hard cap

    public static IServiceCollection AddEskineriaStorage(
        this IServiceCollection services,
        Action<StorageOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new StorageOptions();
        configureOptions(options);
        NormalizeOptions(options);

        services.AddSingleton(options);
        services.AddSingleton<IOptions<StorageOptions>>(_ => Options.Create(options));
        services.TryAddSingleton<FileSecurityProvider>();

        switch (options.ProviderType)
        {
            case StorageProviderType.Local:
                services.AddScoped<IStorageService, EnhancedLocalStorageService>();
                break;
            case StorageProviderType.S3:
                services.AddScoped<IStorageService, S3StorageService>();
                break;
            case StorageProviderType.AzureBlob:
                services.AddScoped<IStorageService, AzureBlobStorageService>();
                break;
            default:
                throw new InvalidOperationException($"Unsupported storage provider: {options.ProviderType}");
        }

        return services;
    }

    private static void NormalizeOptions(StorageOptions options)
    {
        options.Local ??= new LocalStorageOptions();
        options.S3 ??= new S3StorageOptions();
        options.AzureBlob ??= new AzureBlobStorageOptions();
        options.Security ??= new SecurityOptions();
        options.Local.RootPath = string.IsNullOrWhiteSpace(options.Local.RootPath)
            ? Path.Combine("wwwroot", "uploads")
            : options.Local.RootPath.Trim();
        options.Local.BaseUrl = NormalizeLocalBaseUrl(options.Local.BaseUrl);
        options.S3.ServiceUrl = string.IsNullOrWhiteSpace(options.S3.ServiceUrl)
            ? null
            : options.S3.ServiceUrl.Trim();

        if (options.Security.MaxFileSizeBytes <= 0)
        {
            options.Security.MaxFileSizeBytes = DefaultMaxFileSizeBytes;
        }
        else if (options.Security.MaxFileSizeBytes > MaxAllowedFileSizeBytes)
        {
            options.Security.MaxFileSizeBytes = MaxAllowedFileSizeBytes;
        }

        if (options.Security.AllowedExtensions == null || options.Security.AllowedExtensions.Length == 0)
        {
            return;
        }

        options.Security.AllowedExtensions = options.Security.AllowedExtensions
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .Select(ext => ext.Trim())
            .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
            .Select(ext => ext.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeLocalBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "/uploads";
        }

        var trimmed = baseUrl.Trim().TrimEnd('/');
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return trimmed.StartsWith("/", StringComparison.Ordinal)
            ? trimmed
            : $"/{trimmed}";
    }
}
