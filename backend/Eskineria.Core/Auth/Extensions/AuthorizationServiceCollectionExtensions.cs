using Eskineria.Core.Auth.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Eskineria.Core.Auth.Extensions;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddEskineriaPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();
        services.AddAuthorization();

        return services;
    }
}
