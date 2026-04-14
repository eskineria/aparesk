using Eskineria.Core.Auth.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Eskineria.Core.Auth.Extensions;

public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddEskineriaAuthControllers(this IMvcBuilder builder)
    {
        builder.AddApplicationPart(typeof(AccessControlController).Assembly);
        return builder;
    }
}
