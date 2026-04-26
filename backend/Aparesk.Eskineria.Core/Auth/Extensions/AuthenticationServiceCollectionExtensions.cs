using System.Text;
using System.Reflection;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Auth.Configuration;
using Aparesk.Eskineria.Core.Auth.Data;
using Aparesk.Eskineria.Core.Auth.Entities;
using Aparesk.Eskineria.Core.Auth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using Aparesk.Eskineria.Core.Auth.Utilities;

using Aparesk.Eskineria.Core.Auth.Localization;
using FluentValidation;

namespace Aparesk.Eskineria.Core.Auth.Extensions;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddEskineriaAuth<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] permissionAssemblies)
        where TContext : EskineriaIdentityDbContext
    {
        // Bind JWT Settings
        var jwtSettings = new JwtSettings();
        configuration.GetSection("JwtSettings").Bind(jwtSettings);
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDevelopment = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
        {
            throw new InvalidOperationException("JwtSettings:Issuer and JwtSettings:Audience must be configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
        {
            throw new InvalidOperationException("JwtSettings:Secret must be configured.");
        }

        if (!isDevelopment &&
            (jwtSettings.Secret.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase) ||
             Encoding.UTF8.GetByteCount(jwtSettings.Secret) < 32))
        {
            throw new InvalidOperationException("JwtSettings:Secret must be configured with at least 32 bytes in non-development environments.");
        }

        var authDataEncryptionKey = configuration["AuthDataProtection:EncryptionKey"];
        var authDataPreviousEncryptionKeys = configuration.GetSection("AuthDataProtection:PreviousEncryptionKeys").Get<string[]>();
        if (!isDevelopment && string.IsNullOrWhiteSpace(authDataEncryptionKey))
        {
            throw new InvalidOperationException("AuthDataProtection:EncryptionKey must be configured in non-development environments.");
        }

        if (!string.IsNullOrWhiteSpace(authDataEncryptionKey))
        {
            AuthSensitiveDataProtector.Configure(authDataEncryptionKey.Trim(), authDataPreviousEncryptionKeys);
        }
        
        // Register Validators
        services.AddValidatorsFromAssembly(typeof(AuthenticationServiceCollectionExtensions).Assembly);

        // Register Identity
        services.AddIdentity<EskineriaUser, EskineriaRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 3;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<TContext>()
        .AddDefaultTokenProviders()
        .AddErrorDescriber<EskineriaIdentityErrorDescriber>();

        // Set token lifespan to 15 minutes
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromMinutes(15);
        });

        // Ensure EskineriaIdentityDbContext is resolvable for services that depend on it
        services.AddScoped<EskineriaIdentityDbContext>(sp => sp.GetRequiredService<TContext>());

        // Register custom services
        services.AddScoped<AuthEmailTemplateHelper>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<AuthSeedService>();

        // Configure Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = !isDevelopment;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // 1. Try to get token from Authorization Header (Standard/Mobile)
                    string? authHeader = context.Request.Headers["Authorization"];
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                    // 2. If no header, try to get from HttpOnly Cookie (Web Protection)
                    else if (context.Request.Cookies.TryGetValue("X-Access-Token", out var accessToken))
                    {
                        context.Token = accessToken;
                    }
                    
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    if (principal == null)
                    {
                        context.Fail(GetLocalizedMessage(
                            context.HttpContext.RequestServices,
                            "InvalidTokenPrincipal",
                            "Invalid token principal."));
                        return;
                    }

                    var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
                    var userIdRaw = principal.FindFirstValue("id") ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
                    if (string.IsNullOrWhiteSpace(jti) || !Guid.TryParse(userIdRaw, out var userId))
                    {
                        context.Fail(GetLocalizedMessage(
                            context.HttpContext.RequestServices,
                            "InvalidTokenClaims",
                            "Invalid token claims."));
                        return;
                    }

                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<EskineriaIdentityDbContext>();
                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<EskineriaUser>>();
                    var now = DateTime.UtcNow;

                    var user = await userManager.FindByIdAsync(userId.ToString());
                    if (user == null || !user.IsActive)
                    {
                        context.Fail(GetLocalizedMessage(
                            context.HttpContext.RequestServices,
                            "SessionRevokedOrExpired",
                            "Session revoked or expired."));
                        return;
                    }

                    if (await userManager.IsLockedOutAsync(user))
                    {
                        context.Fail(GetLocalizedMessage(
                            context.HttpContext.RequestServices,
                            "SessionRevokedOrExpired",
                            "Session revoked or expired."));
                        return;
                    }

                    var securityStampClaimType = userManager.Options.ClaimsIdentity.SecurityStampClaimType;
                    var tokenSecurityStamp = principal.FindFirstValue(securityStampClaimType);
                    if (string.IsNullOrWhiteSpace(tokenSecurityStamp) ||
                        !string.Equals(tokenSecurityStamp, user.SecurityStamp, StringComparison.Ordinal))
                    {
                        context.Fail(GetLocalizedMessage(
                            context.HttpContext.RequestServices,
                            "SessionRevokedOrExpired",
                            "Session revoked or expired."));
                        return;
                    }

                    // Session-aware validation: access token is accepted only while its backing refresh session is active.
                    var isSessionActive = await dbContext.RefreshTokens
                        .AsNoTracking()
                        .AnyAsync(
                            x => x.JwtId == jti &&
                                 x.UserId == userId &&
                                 !x.Invalidated &&
                                 x.ExpiryDate > now,
                            context.HttpContext.RequestAborted);

                    if (!isSessionActive)
                    {
                        context.Fail(GetLocalizedMessage(
                            context.HttpContext.RequestServices,
                            "SessionRevokedOrExpired",
                            "Session revoked or expired."));
                    }
                }
            };
        });

        services.AddEskineriaPermissionAuthorization();
        services.AddSingleton<IPermissionDiscoveryService>(_ =>
        {
            var assembliesToScan = permissionAssemblies
                .Append(typeof(Authorization.HasPermissionAttribute).Assembly)
                .Distinct()
                .ToArray();

            return new PermissionDiscoveryService(assembliesToScan);
        });
        services.AddScoped<IAccessControlService, AccessControlService>();

        return services;
    }

    private static string GetLocalizedMessage(IServiceProvider serviceProvider, string key, string fallback)
    {
        var localizerFactory = serviceProvider.GetService<IStringLocalizerFactory>();
        var localizer = localizerFactory?.Create(typeof(AuthenticationServiceCollectionExtensions));
        if (localizer == null)
        {
            return fallback;
        }

        var localized = localizer[key].Value;
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
