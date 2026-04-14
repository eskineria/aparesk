using Eskineria.Core.ExceptionHandler.Extensions;
using Eskineria.Core.Auth.Abstractions;
using Eskineria.Core.Auth.Authorization;
using Eskineria.Core.Auth.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Eskineria.Core.Logging.Extensions;
using Eskineria.Core.Localization.Abstractions;
using Eskineria.Core.Versioning.Extensions;
using Eskineria.Core.Localization.Extensions;
using Eskineria.Core.RateLimit.Extensions;
using Eskineria.Application;
using Eskineria.Persistence;
using Eskineria.WebApi.Configuration;
using Eskineria.Core.Shared.Startup;
using Eskineria.Core.Shared.Configuration;
using Eskineria.Core.Auditing.Configuration;
using Eskineria.Core.Auditing.Filters;

var builder = WebApplication.CreateBuilder(args);

// Eskineria Logging (Configure FIRST - before any other services)
builder.AddEskineriaLogging();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.Configure<PagingOptions>(builder.Configuration.GetSection("GlobalSettings:Paging"));
builder.Services.Configure<AuditLogOptions>(builder.Configuration.GetSection("AuditLogs"));
builder.Services.AddScoped<AuditActionFilter>();

// Add services to the container
builder.Services.AddControllers(options =>
    {
        options.Filters.AddService<AuditActionFilter>();
    })
    .AddEskineriaAuthControllers()
    .AddDataAnnotationsLocalization()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEskineriaExceptionHandler(options =>
{
    options.Map<ArgumentException>(StatusCodes.Status400BadRequest, "Bad Request", "BAD_REQUEST");
    options.Map<KeyNotFoundException>(StatusCodes.Status404NotFound, "Not Found", "NOT_FOUND");
    options.Map<UnauthorizedAccessException>(StatusCodes.Status403Forbidden, "Forbidden", "FORBIDDEN");
});

// Configuration
builder.Services.AddRouting(options => 
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// Response Caching
builder.Services.AddResponseCaching();

// Layer Services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddPersistenceServices(builder.Configuration, typeof(Program).Assembly, typeof(HasPermissionAttribute).Assembly);

// Web API specific services
builder.Services.AddEskineriaRateLimit(builder.Configuration);

// Eskineria CORS
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (allowedOrigins == null || allowedOrigins.Length == 0)
    {
        allowedOrigins = new[]
        {
            builder.Configuration["FrontendUrl"] ?? "http://localhost:5173"
        };
    }

    allowedOrigins = allowedOrigins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (allowedOrigins.Length == 0)
    {
        allowedOrigins = new[]
        {
            (builder.Configuration["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/')
        };
    }

    if (allowedOrigins.Any(origin => origin.Contains('*')))
    {
        throw new InvalidOperationException("Wildcard CORS origins are not allowed when credentials are enabled.");
    }

    options.AddPolicy("EskineriaFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// OpenAPI
builder.Services.AddOpenApi(options => options.AddEskineriaOpenApi());

// Eskineria Versioning
builder.Services.AddEskineriaUrlSegmentVersioning();

// Eskineria Localization
builder.Services.AddDatabaseLocalization(builder.Configuration);




// Configure ForwardedHeaders for reverse proxy support
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Kestrel limits for file uploads (default 100MB, configurable)
var maxRequestBodySize = builder.Configuration.GetValue<long?>("FileManager:MaxRequestBodySizeBytes") ?? 104_857_600L;
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodySize;
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxRequestBodySize;
});

// Build App
var app = builder.Build();
var isEfDesignTime = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_EF_DESIGN_TIME"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (await app.TryHandleStartupCommandsAsync(args))
{
    return;
}

// Auto-Migration (all environments): creates DB if missing, then applies pending migrations.
if (!isEfDesignTime)
{
    await app.ApplyMigrationsAsync();
}

// Configure Pipeline
app.ConfigurePipeline();

await app.RunConfiguredStartupSeedAsync();

app.Run();
