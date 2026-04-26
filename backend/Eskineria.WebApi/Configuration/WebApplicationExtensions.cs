using Scalar.AspNetCore;
using System.Data;
using Eskineria.Core.Logging.Extensions;
using Eskineria.Core.ExceptionHandler.Extensions;
using Eskineria.Core.Compliance.Services;
using Eskineria.Core.Localization.Services;
using Microsoft.EntityFrameworkCore;
using Eskineria.Persistence;
using Eskineria.Core.Localization.Entities;
using Eskineria.Core.Settings.Middlewares;
using Eskineria.Core.Shared.Configuration;
using Eskineria.Core.Shared.Localization;
using Microsoft.Data.SqlClient;

namespace Eskineria.WebApi.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // 0. Development Tools (Swagger/Scalar) - Move to the VERY top
        if (app.Environment.IsDevelopment())
        {
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value;
                if (HttpMethods.IsGet(context.Request.Method) &&
                    !string.IsNullOrEmpty(path) &&
                    path.EndsWith("/", StringComparison.Ordinal) &&
                    string.Equals(path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), "scalar", StringComparison.OrdinalIgnoreCase) &&
                    path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length == 2)
                {
                    context.Response.Redirect($"{path.TrimEnd('/')}{context.Request.QueryString}", permanent: false);
                    return;
                }

                await next();
            });

            app.MapOpenApi();
            app.MapScalarApiReference(options => 
            {
                options.WithTitle("Eskineria API Reference")
                       .WithTheme(ScalarTheme.Mars);
            });
        }

        // 1. Localization - Dynamic from DB
        string[] supportedCultures;
        string? configuredDefaultCulture;
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            supportedCultures = dbContext.LanguageResources
                .Select(x => x.Culture)
                .Distinct()
                .ToArray();
            configuredDefaultCulture = dbContext.Settings
                .Where(x => x.Name == SystemSettingKeys.SystemLocalizationDefaultCulture)
                .Select(x => x.Value)
                .FirstOrDefault();
            configuredDefaultCulture = configuredDefaultCulture?.Trim();

            if (supportedCultures.Length == 0)
                supportedCultures = new[] { "en-US", "tr-TR" };
        }

        if (!string.IsNullOrWhiteSpace(configuredDefaultCulture) &&
            !supportedCultures.Contains(configuredDefaultCulture, StringComparer.OrdinalIgnoreCase))
        {
            supportedCultures = supportedCultures
                .Append(configuredDefaultCulture)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var defaultCulture = !string.IsNullOrWhiteSpace(configuredDefaultCulture)
            ? configuredDefaultCulture
            : supportedCultures
                .FirstOrDefault(c => string.Equals(c, "en-US", StringComparison.OrdinalIgnoreCase))
                ?? supportedCultures[0];

        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(defaultCulture)
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        app.UseRequestLocalization(localizationOptions);
        app.UseForwardedHeaders();

        // 3. Core Middlewares
        app.UseEskineriaLogging();
        app.UseCors("EskineriaFrontend");
        app.UseEskineriaExceptionHandler();

        app.Use(async (context, next) =>
        {
            // Skip security headers for Scalar/OpenAPI to avoid MIME type issues
            if (context.Request.Path.StartsWithSegments("/scalar") || 
                context.Request.Path.StartsWithSegments("/openapi"))
            {
                await next();
                return;
            }

            // Security Headers
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            
            // Allow iframes from the frontend URL for previews via CSP frame-ancestors
            var frontendUrl = app.Configuration["FrontendUrl"] ?? "http://localhost:5173";
            context.Response.Headers["Content-Security-Policy"] = $"frame-ancestors 'self' {frontendUrl};";
            
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

            if (IsAuthPath(context.Request.Path))
            {
                context.Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }
            
            // Content Security Policy (CSP) - Strict for production (extended)
            if (!app.Environment.IsDevelopment())
            {
                var existingCsp = context.Response.Headers["Content-Security-Policy"].ToString();
                context.Response.Headers["Content-Security-Policy"] = 
                    existingCsp +
                    "default-src 'self'; " +
                    "script-src 'self'; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self' data:; " +
                    "connect-src 'self'; " +
                    "base-uri 'self'; " +
                    "form-action 'self';";
            }
            
            await next();
        });

        app.UseStaticFiles();
        
        // HTTPS Redirection (Production only)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
            app.UseHsts(); // HTTP Strict Transport Security
        }

        // 4. Auth & Endpoints
        app.UseRouting();
        app.UseResponseCaching(); // Must be before Authentication/Authorization
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseMiddleware<MaintenanceModeMiddleware>();
        app.UseAuthorization();
        
        app.MapControllers();

        return app;
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            Console.WriteLine("Applying migrations...");
            if (dbContext.Database.IsRelational())
            {
                await EnsureDatabaseExistsAsync(dbContext, logger);
                var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
                if (pendingMigrations.Any())
                {
                    if (await ShouldSkipAutomaticMigrationsAsync(dbContext, pendingMigrations))
                    {
                        const string message = "Existing schema detected with out-of-sync migration history. Automatic migrations skipped to protect current data.";
                        Console.WriteLine(message);
                        logger.LogInformation(message);
                    }
                    else
                    {
                        Console.WriteLine($"Found {pendingMigrations.Length} pending migrations. Applying...");
                        await dbContext.Database.MigrateAsync();
                        Console.WriteLine("Migrations applied successfully.");
                    }
                }
                else
                {
                    Console.WriteLine("No pending migrations.");
                }
            }

            await scope.ServiceProvider.GetRequiredService<ComplianceSeedService>().SeedInitialTermsAsync();
            await scope.ServiceProvider.GetRequiredService<LocalizationSyncService>().SyncAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration Error: {ex.Message}");
            logger.LogError(ex, "Database migration or seeding failed");
            throw; 
        }
    }

    private static async Task EnsureDatabaseExistsAsync(
        ApplicationDbContext dbContext,
        ILogger logger)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        var connectionString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        var targetBuilder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = targetBuilder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return;
        }

        var masterBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF DB_ID(@databaseName) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE [' + REPLACE(@databaseName, N']', N']]') + N']';
                EXEC (@sql);
            END
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@databaseName";
        parameter.Value = databaseName;
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync();
        logger.LogInformation("Database '{DatabaseName}' exists or has been created.", databaseName);
    }

    private static async Task<bool> ShouldSkipAutomaticMigrationsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<string> pendingMigrations)
    {
        if (!pendingMigrations.Any(migration => migration.Contains("InitialCreate", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        foreach (var tableName in new[] { "AspNetRoles", "AspNetUsers" })
        {
            if (await TableExistsAsync(dbContext, tableName))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> TableExistsAsync(ApplicationDbContext dbContext, string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            try
            {
                await connection.OpenAsync();
            }
            catch (SqlException ex) when (ex.Number == 4060)
            {
                return false;
            }
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT TOP (1) 1
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = @tableName
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return result is not null and not DBNull;
        }
        finally
        {
            if (shouldCloseConnection && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    public static async Task SyncLocalizationResourcesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<LocalizationSyncService>().SyncAsync();
    }

    private static bool IsAuthPath(PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        var segments = path.Value!
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Any(segment => string.Equals(segment, "auth", StringComparison.OrdinalIgnoreCase));
    }
}
