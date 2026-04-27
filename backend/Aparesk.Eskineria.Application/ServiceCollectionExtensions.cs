using System.Reflection;
using Aparesk.Eskineria.Application.Features.Management.Abstractions;
using Aparesk.Eskineria.Application.Features.Management.Services;
using Aparesk.Eskineria.Application.Features.Products.Abstractions;
using Aparesk.Eskineria.Application.Features.Products.Services;
using Aparesk.Eskineria.Core.Auth.Services;
using Aparesk.Eskineria.Core.Caching.Extensions;
using Aparesk.Eskineria.Core.Mapping.Extensions;
using Aparesk.Eskineria.Core.Notifications.Extensions;
using Aparesk.Eskineria.Core.Storage.Configuration;
using Aparesk.Eskineria.Core.Storage.Extensions;
using Aparesk.Eskineria.Core.Validation.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aparesk.Eskineria.Application;

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

        services.AddScoped<ISiteService, SiteService>();
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IResidentService, ResidentService>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
