using Eskineria.Core.Logging.Configuration;
using Eskineria.Core.Logging.Middlewares;
using Eskineria.Core.Logging.Sinks;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace Eskineria.Core.Logging.Extensions;

public static class LoggerConfigurationExtensions
{
    private static readonly IReadOnlyDictionary<string, string> EmptyDotEnvValues =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static WebApplicationBuilder AddEskineriaLogging(this WebApplicationBuilder builder)
        => builder.AddEskineriaLogging(builder.Configuration);

    public static WebApplicationBuilder AddEskineriaLogging(
        this WebApplicationBuilder builder,
        IConfiguration configuration,
        string sectionName = "EskineriaLogging")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new EskineriaLoggingOptions();
        configuration.GetSection(sectionName).Bind(options);
        ValidateAndNormalize(options);

        builder.Services.AddSingleton(Options.Create(options));
        builder.Logging.ClearProviders();

        builder.Host.UseSerilog((_, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName);

            if (TryParseLogEventLevel(options.MinimumLevel, out var minimumLevel))
            {
                loggerConfiguration.MinimumLevel.Is(minimumLevel);
            }

            foreach (var (sourceContext, levelText) in options.MinimumLevelOverrides)
            {
                if (string.IsNullOrWhiteSpace(sourceContext) ||
                    !TryParseLogEventLevel(levelText, out var overrideLevel))
                {
                    continue;
                }

                loggerConfiguration.MinimumLevel.Override(sourceContext.Trim(), overrideLevel);
            }

            ConfigureKafkaSink(loggerConfiguration, options.Kafka, builder.Environment.ApplicationName);
        });

        return builder;
    }

    public static IApplicationBuilder UseEskineriaLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        return app;
    }

    private static void ValidateAndNormalize(EskineriaLoggingOptions options)
    {
        options.MinimumLevel = NormalizeMinimumLevel(options.MinimumLevel);
        options.MinimumLevelOverrides = NormalizeMinimumLevelOverrides(options.MinimumLevelOverrides);
        options.Kafka = NormalizeKafkaOptions(options.Kafka);

        options.CorrelationIdHeaderName = string.IsNullOrWhiteSpace(options.CorrelationIdHeaderName)
            ? "X-Correlation-Id"
            : options.CorrelationIdHeaderName.Trim();

        if (!options.CorrelationIdHeaderName.StartsWith("X-", StringComparison.OrdinalIgnoreCase))
        {
            options.CorrelationIdHeaderName = $"X-{options.CorrelationIdHeaderName}";
        }

        if (options.MaxBodyLogSizeBytes <= 0)
        {
            options.MaxBodyLogSizeBytes = 100 * 1024;
        }
        else
        {
            options.MaxBodyLogSizeBytes = Math.Min(options.MaxBodyLogSizeBytes, 1024 * 1024);
        }

        if (options.MaxHeaderValueLogLength <= 0)
        {
            options.MaxHeaderValueLogLength = 100;
        }

        if (options.MaxQueryValueLogLength <= 0)
        {
            options.MaxQueryValueLogLength = 200;
        }

        if (options.MaxQueryStringLogLength <= 0)
        {
            options.MaxQueryStringLogLength = 2000;
        }

        options.ExcludedPathPrefixes = NormalizeStringArray(options.ExcludedPathPrefixes, new[] { "/scalar", "/openapi" });
        options.MaskedFields = NormalizeStringArray(options.MaskedFields, new[]
        {
            "password",
            "confirmpassword",
            "token",
            "authorization",
            "secret",
            "apikey",
            "otp",
            "refreshtoken",
            "accesstoken",
            "verificationcode",
            "resetcode",
            "mfacode",
            "twofactorcode",
            "recoverycode",
            "securitycode"
        });
        options.SensitiveHeaders = NormalizeStringArray(options.SensitiveHeaders, new[]
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-Auth-Token",
            "X-CSRF-Token"
        });
    }

    private static KafkaLoggingOptions NormalizeKafkaOptions(KafkaLoggingOptions? options)
    {
        var normalized = options ?? new KafkaLoggingOptions();

        normalized.BootstrapServers = (normalized.BootstrapServers ?? string.Empty).Trim();
        normalized.Topic = string.IsNullOrWhiteSpace(normalized.Topic) ? "eskineria-logs" : normalized.Topic.Trim();
        normalized.ClientId = string.IsNullOrWhiteSpace(normalized.ClientId) ? null : normalized.ClientId.Trim();
        normalized.Username = string.IsNullOrWhiteSpace(normalized.Username) ? null : normalized.Username.Trim();
        normalized.Password = string.IsNullOrWhiteSpace(normalized.Password) ? null : normalized.Password.Trim();
        normalized.SecurityProtocol = NormalizeKafkaSecurityProtocol(normalized.SecurityProtocol);
        normalized.SaslMechanism = NormalizeKafkaSaslMechanism(normalized.SaslMechanism);
        normalized.RestrictedToMinimumLevel = NormalizeMinimumLevel(normalized.RestrictedToMinimumLevel);
        normalized.Environment = NormalizeKafkaEnvironmentOptions(normalized.Environment);
        var dotEnvValues = normalized.Environment.Enabled && normalized.Environment.UseDotEnvFile
            ? LoadDotEnvValues(normalized.Environment.DotEnvFilePath)
            : EmptyDotEnvValues;

        if (normalized.Environment.Enabled)
        {
            normalized.BootstrapServers = ResolveRequiredKafkaSetting(
                normalized.BootstrapServers,
                normalized.Environment.BootstrapServersKey,
                dotEnvValues);

            normalized.Username = ResolveOptionalKafkaSetting(
                normalized.Username,
                normalized.Environment.UsernameKey,
                dotEnvValues);

            normalized.Password = ResolveOptionalKafkaSetting(
                normalized.Password,
                normalized.Environment.PasswordKey,
                dotEnvValues);
        }

        return normalized;
    }

    private static KafkaEnvironmentOptions NormalizeKafkaEnvironmentOptions(KafkaEnvironmentOptions? options)
    {
        var normalized = options ?? new KafkaEnvironmentOptions();

        normalized.DotEnvFilePath = NormalizeDotEnvFilePath(normalized.DotEnvFilePath);
        normalized.BootstrapServersKey = NormalizeEnvironmentKey(normalized.BootstrapServersKey);
        normalized.UsernameKey = NormalizeEnvironmentKey(normalized.UsernameKey);
        normalized.PasswordKey = NormalizeEnvironmentKey(normalized.PasswordKey);

        return normalized;
    }

    private static string NormalizeDotEnvFilePath(string? path)
        => string.IsNullOrWhiteSpace(path) ? ".env" : path.Trim();

    private static string NormalizeEnvironmentKey(string? key)
        => string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();

    private static string NormalizeKafkaSecurityProtocol(string? securityProtocol)
    {
        if (TryParseSecurityProtocol(securityProtocol, out var parsed))
        {
            return parsed.ToString();
        }

        return SecurityProtocol.Plaintext.ToString();
    }

    private static string NormalizeKafkaSaslMechanism(string? saslMechanism)
    {
        if (TryParseSaslMechanism(saslMechanism, out var parsed))
        {
            return parsed.ToString();
        }

        return SaslMechanism.Plain.ToString();
    }

    private static void ConfigureKafkaSink(
        LoggerConfiguration loggerConfiguration,
        KafkaLoggingOptions kafkaOptions,
        string? applicationName)
    {
        if (!kafkaOptions.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(kafkaOptions.BootstrapServers) || string.IsNullOrWhiteSpace(kafkaOptions.Topic))
        {
            SelfLog.WriteLine("Kafka sink is enabled but BootstrapServers/Topic is missing. Sink will not be configured.");
            return;
        }

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers,
            ClientId = string.IsNullOrWhiteSpace(kafkaOptions.ClientId) ? applicationName : kafkaOptions.ClientId
        };

        if (TryParseSecurityProtocol(kafkaOptions.SecurityProtocol, out var securityProtocol))
        {
            producerConfig.SecurityProtocol = securityProtocol;
        }

        if (!string.IsNullOrWhiteSpace(kafkaOptions.Username) && !string.IsNullOrWhiteSpace(kafkaOptions.Password))
        {
            producerConfig.SaslUsername = kafkaOptions.Username;
            producerConfig.SaslPassword = kafkaOptions.Password;

            if (TryParseSaslMechanism(kafkaOptions.SaslMechanism, out var saslMechanism))
            {
                producerConfig.SaslMechanism = saslMechanism;
            }
        }

        var sink = new KafkaLogEventSink(producerConfig, kafkaOptions.Topic);
        if (TryParseLogEventLevel(kafkaOptions.RestrictedToMinimumLevel, out var restrictedLevel))
        {
            loggerConfiguration.WriteTo.Sink(sink, restrictedToMinimumLevel: restrictedLevel);
            return;
        }

        loggerConfiguration.WriteTo.Sink(sink);
    }

    private static bool TryParseSecurityProtocol(string? value, out SecurityProtocol parsed)
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out parsed);

    private static bool TryParseSaslMechanism(string? value, out SaslMechanism parsed)
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out parsed);

    private static string ResolveRequiredKafkaSetting(
        string currentValue,
        string envKey,
        IReadOnlyDictionary<string, string> dotEnvValues)
    {
        var envValue = GetEnvironmentValue(envKey, dotEnvValues);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        if (IsReplaceInEnvPlaceholder(currentValue))
        {
            return string.Empty;
        }

        return currentValue.Trim();
    }

    private static string? ResolveOptionalKafkaSetting(
        string? currentValue,
        string envKey,
        IReadOnlyDictionary<string, string> dotEnvValues)
    {
        var envValue = GetEnvironmentValue(envKey, dotEnvValues);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        if (IsReplaceInEnvPlaceholder(currentValue))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(currentValue) ? null : currentValue.Trim();
    }

    private static string? GetEnvironmentValue(string envKey, IReadOnlyDictionary<string, string> dotEnvValues)
    {
        if (string.IsNullOrWhiteSpace(envKey))
        {
            return null;
        }

        var trimmedKey = envKey.Trim();
        var value = Environment.GetEnvironmentVariable(trimmedKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        if (dotEnvValues.TryGetValue(trimmedKey, out var dotEnvValue) && !string.IsNullOrWhiteSpace(dotEnvValue))
        {
            return dotEnvValue.Trim();
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> LoadDotEnvValues(string dotEnvFilePath)
    {
        if (string.IsNullOrWhiteSpace(dotEnvFilePath))
        {
            return EmptyDotEnvValues;
        }

        var fullPath = ResolveDotEnvPath(dotEnvFilePath);
        if (fullPath is null || !File.Exists(fullPath))
        {
            return EmptyDotEnvValues;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(fullPath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed["export ".Length..].TrimStart();
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = trimmed[(separatorIndex + 1)..].Trim();
            values[key] = RemoveWrappingQuotes(value);
        }

        return values;
    }

    private static string? ResolveDotEnvPath(string dotEnvFilePath)
    {
        if (Path.IsPathRooted(dotEnvFilePath))
        {
            return dotEnvFilePath;
        }

        var currentDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), dotEnvFilePath);
        if (File.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        var baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, dotEnvFilePath);
        if (File.Exists(baseDirectoryPath))
        {
            return baseDirectoryPath;
        }

        return currentDirectoryPath;
    }

    private static string RemoveWrappingQuotes(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var startsWithDoubleQuote = value.StartsWith('"');
        var endsWithDoubleQuote = value.EndsWith('"');
        if (startsWithDoubleQuote && endsWithDoubleQuote)
        {
            return value[1..^1];
        }

        var startsWithSingleQuote = value.StartsWith('\'');
        var endsWithSingleQuote = value.EndsWith('\'');
        if (startsWithSingleQuote && endsWithSingleQuote)
        {
            return value[1..^1];
        }

        return value.Trim();
    }

    private static bool IsReplaceInEnvPlaceholder(string? value)
        => string.Equals(value?.Trim(), "REPLACE_IN_ENV", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeMinimumLevel(string? level)
    {
        if (!TryParseLogEventLevel(level, out _))
        {
            return null;
        }

        return level!.Trim();
    }

    private static Dictionary<string, string> NormalizeMinimumLevelOverrides(Dictionary<string, string>? overrides)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (overrides is null || overrides.Count == 0)
        {
            return normalized;
        }

        foreach (var (sourceContext, level) in overrides)
        {
            if (string.IsNullOrWhiteSpace(sourceContext) || !TryParseLogEventLevel(level, out _))
            {
                continue;
            }

            normalized[sourceContext.Trim()] = level.Trim();
        }

        return normalized;
    }

    private static bool TryParseLogEventLevel(string? level, out LogEventLevel parsedLevel)
        => Enum.TryParse(level?.Trim(), ignoreCase: true, out parsedLevel);

    private static string[] NormalizeStringArray(string[]? values, string[] fallback)
    {
        var normalized = fallback
            .Concat(values ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized is { Length: > 0 } ? normalized : fallback;
    }
}
