using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aparesk.Eskineria.Core.Repository.Extensions;

public static class ServiceCollectionExtensions
{
    private const int MaxAllowedPageSize = 1000;

    /// <summary>
    /// Registers Aparesk.Eskineria Repository abstractions with support for multiple DbContexts.
    /// This allows injecting IRepository<TContext, TEntity> for explicit multi-db support.
    /// </summary>
    public static IServiceCollection AddEskineriaRepository(
        this IServiceCollection services, 
        Action<RepositoryOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RepositoryOptions();
        configureOptions?.Invoke(options);
        NormalizeOptions(options);

        services.TryAddSingleton(options);
        
        // Register the global open generic repositories (for IRepository<TContext, TEntity>)
        services.TryAddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.TryAddScoped(typeof(IReadRepository<,>), typeof(EfRepository<,>));

        return services;
    }

    /// <summary>
    /// Registers Aparesk.Eskineria Repository for a specific DbContext.
    /// Currently just a pass-through to the global registration.
    /// </summary>
    public static IServiceCollection AddEskineriaRepository<TContext>(
        this IServiceCollection services, 
        Action<RepositoryOptions>? configureOptions = null)
        where TContext : DbContext
    {
        return services.AddEskineriaRepository(configureOptions);
    }

    private static void NormalizeOptions(RepositoryOptions options)
    {
        if (options.DefaultPageSize <= 0)
        {
            options.DefaultPageSize = 10;
        }

        if (options.MaxPageSize <= 0)
        {
            options.MaxPageSize = 100;
        }
        else if (options.MaxPageSize > MaxAllowedPageSize)
        {
            options.MaxPageSize = MaxAllowedPageSize;
        }

        if (options.MaxPageSize < options.DefaultPageSize)
        {
            options.MaxPageSize = options.DefaultPageSize;
        }
    }
}
