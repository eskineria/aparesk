using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Eskineria.Core.Mapping.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEskineriaMapping(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        var assembliesToScan = assemblies
            .Where(assembly => assembly != null)
            .Distinct()
            .ToArray();

        if (assembliesToScan.Length == 0)
        {
            throw new ArgumentException("At least one assembly is required for mapping registration.", nameof(assemblies));
        }

        services.AddSingleton(provider =>
        {
            var config = new TypeAdapterConfig();
            foreach (var assembly in assembliesToScan)
            {
                new MappingProfile(assembly).Apply(config);
            }
            config.Compile();
            return config;
        });

        services.AddScoped<IMapper, ServiceMapper>();
        
        return services;
    }
}
