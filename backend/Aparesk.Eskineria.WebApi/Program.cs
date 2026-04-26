using Aparesk.Eskineria.Core.ExceptionHandler.Extensions;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Auth.Authorization;
using Aparesk.Eskineria.Core.Auth.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Aparesk.Eskineria.Core.Logging.Extensions;
using Aparesk.Eskineria.Core.Localization.Abstractions;
using Aparesk.Eskineria.Core.Versioning.Extensions;
using Aparesk.Eskineria.Core.Localization.Extensions;
using Aparesk.Eskineria.Core.RateLimit.Extensions;
using Aparesk.Eskineria.Application;
using Aparesk.Eskineria.Persistence;
using Aparesk.Eskineria.WebApi.Configuration;
using Aparesk.Eskineria.Core.Shared.Startup;
using Aparesk.Eskineria.Core.Shared.Configuration;
using Aparesk.Eskineria.Core.Auditing.Configuration;
using Aparesk.Eskineria.Core.Auditing.Filters;

var builder = WebApplication.CreateBuilder(args);

// Aparesk.Eskineria Logging (Configure FIRST - before any other services)
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

// Aparesk.Eskineria CORS
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

// Aparesk.Eskineria Versioning
builder.Services.AddEskineriaUrlSegmentVersioning();

// Aparesk.Eskineria Localization
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
