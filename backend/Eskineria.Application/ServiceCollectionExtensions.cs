using System.Reflection;
using Eskineria.Application.Features.Products.Abstractions;
using Eskineria.Application.Features.Products.Services;
using Eskineria.Core.Auth.Services;
using Eskineria.Core.Caching.Extensions;
using Eskineria.Core.Mapping.Extensions;
using Eskineria.Core.Notifications.Extensions;
using Eskineria.Core.Storage.Configuration;
using Eskineria.Core.Storage.Extensions;
using Eskineria.Core.Validation.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eskineria.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var applicationAssembly = Assembly.GetExecutingAssembly();
        var coreAssembly = typeof(CurrentUserService).Assembly;

        services.AddEskineriaMapping(applicationAssembly, coreAssembly);
        services.AddEskineriaValidation(new[] { applicationAssembly, coreAssembly });
        services.AddEskineriaCaching(configuration);

        services.AddEskineriaNotifications();
        services.AddEmailChannel(configuration);

        services.AddEskineriaStorage(options =>
        {
            configuration.GetSection("Storage").Bind(options);

            var configuredProvider = configuration["Storage:Provider"];
            if (string.IsNullOrWhiteSpace(configuredProvider))
            {
                configuredProvider = configuration["Storage:ProviderType"];
            }

            if (!string.IsNullOrWhiteSpace(configuredProvider) &&
                Enum.TryParse<StorageProviderType>(configuredProvider, ignoreCase: true, out var providerType))
            {
                options.ProviderType = providerType;
            }
        });

        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
