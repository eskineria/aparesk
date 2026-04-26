using Aparesk.Eskineria.Core.ExceptionHandler.Configuration;
using Aparesk.Eskineria.Core.ExceptionHandler.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aparesk.Eskineria.Core.ExceptionHandler.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseEskineriaExceptionHandler(
        this IApplicationBuilder app, 
        Action<ExceptionOptions>? configureOptions = null)
    {
        if (configureOptions == null)
        {
            return app.UseMiddleware<GlobalExceptionMiddleware>();
        }

        var options = app.ApplicationServices.GetService<IOptions<ExceptionOptions>>()?.Value
                      ?? new ExceptionOptions();

        configureOptions(options);
        return app.UseMiddleware<GlobalExceptionMiddleware>(options);
    }
}
