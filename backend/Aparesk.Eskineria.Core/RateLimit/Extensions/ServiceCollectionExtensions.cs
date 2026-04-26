using System.Threading.RateLimiting;
using Aparesk.Eskineria.Core.RateLimit.Configuration;
using Aparesk.Eskineria.Core.RateLimit.Handlers;
using Aparesk.Eskineria.Core.RateLimit.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aparesk.Eskineria.Core.RateLimit.Extensions;

public static class ServiceCollectionExtensions
{
    private const int MaxPermitLimit = 10_000;
    private const int MaxWindowSeconds = 86_400;
    private const int MaxQueueLimit = 10_000;
    private const int MaxTokenLimit = 10_000;
    private const int MaxSegmentsPerWindow = 100;
    private const int MaxPolicyNameLength = 64;

    public static IServiceCollection AddEskineriaRateLimit(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "RateLimit")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentException("Section name cannot be null or whitespace.", nameof(sectionName));
        }

        return services.AddEskineriaRateLimit(options =>
        {
            var section = configuration.GetSection(sectionName);
            if (section.Exists())
            {
                section.Bind(options);
            }
        });
    }

    public static IServiceCollection AddEskineriaRateLimit(
        this IServiceCollection services,
        Action<RateLimitOptions>? configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RateLimitOptions();
        configureOptions?.Invoke(options);
        ValidateAndNormalize(options);

        services.AddSingleton(options); // Backward compatibility for direct RateLimitOptions injection
        services.AddSingleton(Options.Create(options));

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.OnRejected = RateLimitResponseHandler.OnRejected;

            if (options.EnableGlobalLimiter)
            {
                limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    if (IsExcludedPath(httpContext.Request.Path, options.ExcludedPathPrefixes))
                    {
                        return RateLimitPartition.GetNoLimiter($"excluded:{httpContext.Request.Path.Value}");
                    }

                    var partitionKey = RateLimitClientIdentifierResolver.Resolve(httpContext);
                    var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
                    var permitLimit = isAuthenticated ? options.Global.AuthenticatedPermitLimit : options.Global.PermitLimit;
                    var windowSeconds = isAuthenticated ? options.Global.AuthenticatedWindowSeconds : options.Global.WindowSeconds;
                    var queueLimit = isAuthenticated ? options.Global.AuthenticatedQueueLimit : options.Global.QueueLimit;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: partitionKey,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromSeconds(windowSeconds),
                            QueueLimit = queueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });
            }

            foreach (var policy in options.Policies)
            {
                limiterOptions.AddPolicy(policy.PolicyName, httpContext =>
                {
                    var partitionKey = $"{policy.PolicyName}:{RateLimitClientIdentifierResolver.Resolve(httpContext)}";

                    return policy.Algorithm switch
                    {
                        RateLimitAlgorithm.FixedWindow => RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = policy.PermitLimit,
                                Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                                QueueLimit = policy.QueueLimit,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            }),
                        RateLimitAlgorithm.SlidingWindow => RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey,
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = policy.PermitLimit,
                                Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                                SegmentsPerWindow = policy.SegmentsPerWindow,
                                QueueLimit = policy.QueueLimit,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            }),
                        RateLimitAlgorithm.TokenBucket => RateLimitPartition.GetTokenBucketLimiter(
                            partitionKey,
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = policy.TokenLimit,
                                TokensPerPeriod = policy.TokensPerPeriod,
                                ReplenishmentPeriod = TimeSpan.FromSeconds(policy.PeriodSeconds),
                                AutoReplenishment = true,
                                QueueLimit = policy.QueueLimit,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            }),
                        RateLimitAlgorithm.Concurrency => RateLimitPartition.GetConcurrencyLimiter(
                            partitionKey,
                            _ => new ConcurrencyLimiterOptions
                            {
                                PermitLimit = policy.PermitLimit,
                                QueueLimit = policy.QueueLimit,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            }),
                        _ => RateLimitPartition.GetNoLimiter(partitionKey)
                    };
                });
            }
        });

        return services;
    }

    private static void ValidateAndNormalize(RateLimitOptions options)
    {
        options.Global ??= new GlobalRateLimitPolicy();
        options.Policies ??= new List<CustomRateLimitPolicy>();

        options.Global.PermitLimit = ClampPositiveOrDefault(options.Global.PermitLimit, 30, MaxPermitLimit);
        options.Global.WindowSeconds = ClampPositiveOrDefault(options.Global.WindowSeconds, 60, MaxWindowSeconds);
        options.Global.QueueLimit = ClampNonNegative(options.Global.QueueLimit, MaxQueueLimit);
        options.Global.AuthenticatedPermitLimit = ClampPositiveOrDefault(options.Global.AuthenticatedPermitLimit, 180, MaxPermitLimit);
        options.Global.AuthenticatedWindowSeconds = ClampPositiveOrDefault(options.Global.AuthenticatedWindowSeconds, 60, MaxWindowSeconds);
        options.Global.AuthenticatedQueueLimit = ClampNonNegative(options.Global.AuthenticatedQueueLimit, MaxQueueLimit);
        options.ExcludedPathPrefixes ??= new List<string>();
        options.ExcludedPathPrefixes = options.ExcludedPathPrefixes
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => NormalizePathPrefix(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < options.Policies.Count; i++)
        {
            var policy = options.Policies[i];
            if (policy == null)
            {
                options.Policies[i] = new CustomRateLimitPolicy
                {
                    PolicyName = $"policy-{i + 1}"
                };
                policy = options.Policies[i];
            }

            policy.PolicyName = string.IsNullOrWhiteSpace(policy.PolicyName)
                ? $"policy-{i + 1}"
                : policy.PolicyName.Trim();
            if (policy.PolicyName.Length > MaxPolicyNameLength)
            {
                policy.PolicyName = policy.PolicyName[..MaxPolicyNameLength];
            }

            policy.PermitLimit = ClampPositiveOrDefault(policy.PermitLimit, 10, MaxPermitLimit);
            policy.WindowSeconds = ClampPositiveOrDefault(policy.WindowSeconds, 60, MaxWindowSeconds);
            policy.QueueLimit = ClampNonNegative(policy.QueueLimit, MaxQueueLimit);

            policy.SegmentsPerWindow = ClampPositiveOrDefault(policy.SegmentsPerWindow, 1, MaxSegmentsPerWindow);
            policy.TokenLimit = ClampPositiveOrDefault(policy.TokenLimit, 5, MaxTokenLimit);
            policy.TokensPerPeriod = ClampPositiveOrDefault(policy.TokensPerPeriod, 1, MaxTokenLimit);
            if (policy.TokensPerPeriod > policy.TokenLimit)
            {
                policy.TokensPerPeriod = policy.TokenLimit;
            }

            policy.PeriodSeconds = ClampPositiveOrDefault(policy.PeriodSeconds, 10, MaxWindowSeconds);
        }

        var duplicates = options.Policies
            .GroupBy(p => p.PolicyName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"RateLimit policy names must be unique. Duplicates: {string.Join(", ", duplicates)}");
        }
    }

    private static bool IsExcludedPath(PathString requestPath, IEnumerable<string> excludedPathPrefixes)
    {
        foreach (var excludedPathPrefix in excludedPathPrefixes)
        {
            if (requestPath.StartsWithSegments(excludedPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePathPrefix(string path)
    {
        var trimmed = path.Trim();
        if (trimmed.Length == 0)
        {
            return "/";
        }

        return trimmed.StartsWith("/", StringComparison.Ordinal) ? trimmed : "/" + trimmed;
    }

    private static int ClampPositiveOrDefault(int value, int defaultValue, int maxValue)
    {
        if (value <= 0)
        {
            return defaultValue;
        }

        return value > maxValue ? maxValue : value;
    }

    private static int ClampNonNegative(int value, int maxValue)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > maxValue ? maxValue : value;
    }
}
