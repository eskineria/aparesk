using Eskineria.Core.Auditing.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Eskineria.Core.Auditing.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppAuditing<TStore>(this IServiceCollection services) where TStore : class, IAuditingStore
    {
        // Scoped lifetime aligns with common store implementations that depend on scoped resources (e.g. DbContext).
        services.TryAddScoped<IAuditingStore, TStore>();
        return services;
    }
}
