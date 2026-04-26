using Aparesk.Eskineria.Core.ExceptionHandler.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aparesk.Eskineria.Core.ExceptionHandler.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEskineriaExceptionHandler(
        this IServiceCollection services,
        Action<ExceptionOptions>? configureOptions = null)
    {
        services.AddOptions<ExceptionOptions>();

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        return services;
    }
}
