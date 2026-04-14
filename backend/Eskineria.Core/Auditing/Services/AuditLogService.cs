using System.Globalization;
using System.Text.Json;
using Eskineria.Core.Shared.Configuration;
using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Auditing.Configuration;
using Eskineria.Core.Auditing.Requests;
using Eskineria.Core.Auditing.Responses;
using Eskineria.Core.Auditing.Models;
using Eskineria.Core.Auditing.Utilities;
using Eskineria.Core.Shared.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Eskineria.Core.Auditing.Services;

public class AuditLogService : IAuditLogService
{
    private const int MinimumHardeningSecretLength = 32;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditLogIntegrityRepository _auditLogIntegrityRepository;
    private readonly PagingOptions _pagingOptions;
    private readonly AuditLogOptions _auditLogOptions;
    private readonly string _hardeningSecret;
    private readonly bool _isHardeningEnabled;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        IAuditLogIntegrityRepository auditLogIntegrityRepository,
        IConfiguration configuration,
        IOptions<PagingOptions> pagingOptions,
        IOptions<AuditLogOptions> auditLogOptions)
    {
        _auditLogRepository = auditLogRepository;
        _auditLogIntegrityRepository = auditLogIntegrityRepository;
        _pagingOptions = pagingOptions.Value ?? new PagingOptions();
        _auditLogOptions = auditLogOptions.Value ?? new AuditLogOptions();
        _hardeningSecret = ResolveHardeningSecret(configuration);
        _isHardeningEnabled = _hardeningSecret.Length >= MinimumHardeningSecretLength;
    }

    public async Task<PagedResponse<AuditLogListItemDto>> GetPagedAsync(
        GetAuditLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeRequest(request);
        var offset = (normalizedRequest.PageNumber - 1) * normalizedRequest.PageSize;
        var query = ApplyFilters(_auditLogRepository.Query().AsNoTracking(), normalizedRequest);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.ExecutionTime)
            .ThenByDescending(x => x.Id)
            .Skip(offset)
            .Take(normalizedRequest.PageSize)
            .Select(x => new AuditLogListItemDto
            {
                Id = x.Id,
                UserId = x.UserId,
                ServiceName = x.ServiceName,
                MethodName = x.MethodName,
                Parameters = x.Parameters,
                ExecutionTime = x.ExecutionTime,
                ExecutionDuration = x.ExecutionDuration,
                ClientIpAddress = x.ClientIpAddress,
                BrowserInfo = x.BrowserInfo,
                Exception = x.Exception,
            })
            .ToListAsync(cancellationToken);

        var index = normalizedRequest.PageNumber - 1;
        var pages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)normalizedRequest.PageSize);

        return new PagedResponse<AuditLogListItemDto>(
            items,
            index,
            normalizedRequest.PageSize,
            totalCount,
            pages,
            hasPrevious: index > 0,
            hasNext: index + 1 < pages);
    }

    public async Task<DataResponse<AuditLogFilterOptionsDto>> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var services = await _auditLogRepository.Query()
            .AsNoTracking()
            .Where(x => x.ServiceName != string.Empty)
            .Select(x => x.ServiceName)
            .Distinct()
            .OrderBy(x => x)
            .Take(200)
            .ToListAsync(cancellationToken);

        var methods = await _auditLogRepository.Query()
            .AsNoTracking()
            .Where(x => x.MethodName != string.Empty)
            .Select(x => x.MethodName)
            .Distinct()
            .OrderBy(x => x)
            .Take(200)
            .ToListAsync(cancellationToken);

        return DataResponse<AuditLogFilterOptionsDto>.Succeed(new AuditLogFilterOptionsDto
        {
            Services = services,
            Methods = methods,
        });
    }

    public async Task<DataResponse<AuditAlertsSummaryDto>> GetAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = new AuditAlertsSummaryDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
        };

        var fromUtc = DateTime.UtcNow.AddHours(-1);

        var baseQuery = _auditLogRepository.Query()
            .AsNoTracking()
            .Where(x => x.ExecutionTime >= fromUtc);

        var errorCount = await baseQuery.CountAsync(
            x => x.Exception != null && x.Exception.Trim() != string.Empty,
            cancellationToken);
        var deleteCount = await baseQuery.CountAsync(
            x => x.MethodName.Contains("Delete"),
            cancellationToken);
        var roleSwitchCount = await baseQuery.CountAsync(
            x => x.MethodName.Contains("SwitchRole"),
            cancellationToken);

        if (errorCount >= _auditLogOptions.ErrorAlertThreshold)
        {
            summary.Alerts.Add(new AuditAlertDto
            {
                Key = "audit.high_error_rate",
                Severity = "High",
                MetricValue = errorCount,
                Message = $"High error activity detected in last hour: {errorCount} failed actions.",
            });
        }

        if (deleteCount >= _auditLogOptions.DeleteAlertThreshold)
        {
            summary.Alerts.Add(new AuditAlertDto
            {
                Key = "audit.high_delete_activity",
                Severity = "Medium",
                MetricValue = deleteCount,
                Message = $"Unusually high delete activity detected: {deleteCount} delete actions in last hour.",
            });
        }

        if (roleSwitchCount >= _auditLogOptions.RoleSwitchAlertThreshold)
        {
            summary.Alerts.Add(new AuditAlertDto
            {
                Key = "audit.high_role_switch_activity",
                Severity = "Medium",
                MetricValue = roleSwitchCount,
                Message = $"High role-switch activity detected: {roleSwitchCount} switches in last hour.",
            });
        }

        return DataResponse<AuditAlertsSummaryDto>.Succeed(summary);
    }

    public async Task<DataResponse<AuditIntegritySummaryDto>> GetIntegritySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = new AuditIntegritySummaryDto
        {
            FeatureEnabled = _isHardeningEnabled,
            LastVerifiedAtUtc = DateTime.UtcNow,
        };

        summary.TotalAuditLogCount = await _auditLogRepository.Query().AsNoTracking().CountAsync(cancellationToken);
        summary.IntegrityTableExists = true;
        if (!_isHardeningEnabled)
        {
            return DataResponse<AuditIntegritySummaryDto>.Succeed(summary);
        }

        var normalizedAuditTable = NormalizeAuditTableName(_auditLogOptions.AppAuditLogsTableName);
        var integrityEntries = await _auditLogIntegrityRepository.Query()
            .AsNoTracking()
            .Where(x => x.AuditTable == normalizedAuditTable)
            .OrderBy(x => x.AuditLogId)
            .ToListAsync(cancellationToken);

        if (integrityEntries.Count == 0)
        {
            summary.MissingHardeningCount = summary.FeatureEnabled
                ? summary.TotalAuditLogCount
                : 0;
            summary.MissingSampleAuditLogIds = summary.MissingHardeningCount > 0
                ? await _auditLogRepository.Query()
                    .AsNoTracking()
                    .OrderByDescending(x => x.Id)
                    .Take(_auditLogOptions.IntegritySampleSize)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken)
                : new List<long>();

            return DataResponse<AuditIntegritySummaryDto>.Succeed(summary);
        }

        var hardenedIds = integrityEntries
            .Select(x => x.AuditLogId)
            .Distinct()
            .ToList();

        var hardenedLogs = await _auditLogRepository.Query()
            .AsNoTracking()
            .Where(x => hardenedIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var stats = GetHardeningStats(integrityEntries, hardenedLogs, normalizedAuditTable);

        summary.HardenedLogCount = stats.HardenedCount;
        summary.LastHardenedAuditLogId = stats.LastHardenedAuditLogId;
        summary.BrokenChainCount = stats.BrokenChainCount;
        summary.BrokenSampleAuditLogIds = stats.BrokenSampleAuditLogIds;
        summary.MissingHardeningCount = Math.Max(0, summary.TotalAuditLogCount - summary.HardenedLogCount);
        summary.MissingSampleAuditLogIds = summary.MissingHardeningCount > 0
            ? await GetMissingHardeningSampleIdsAsync(normalizedAuditTable, cancellationToken)
            : new List<long>();

        return DataResponse<AuditIntegritySummaryDto>.Succeed(summary);
    }

    public async Task<DataResponse<AuditLogDiffResultDto>> GetDiffAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return DataResponse<AuditLogDiffResultDto>.Succeed(new AuditLogDiffResultDto
            {
                LogId = id,
                Source = _auditLogOptions.DiffSourceNone,
                HasComparableData = false,
            });
        }

        var current = await GetAuditRecordByIdAsync(id, cancellationToken);
        if (current == null)
        {
            return DataResponse<AuditLogDiffResultDto>.Succeed(new AuditLogDiffResultDto
            {
                LogId = id,
                Source = _auditLogOptions.DiffSourceNone,
                HasComparableData = false,
            });
        }

        var hasPayloadBeforeAfter = TryParseBeforeAfterPayload(
            current.Parameters,
            out var payloadBefore,
            out var payloadAfter);

        var currentMap = ParseParameterMap(current.Parameters);
        var identityMap = payloadAfter.Count > 0 ? payloadAfter : currentMap;
        var previous = await FindComparablePreviousRecordAsync(current, identityMap, cancellationToken);

        var previousMap = previous != null
            ? ParseParameterMap(previous.Parameters)
            : new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var hasPayloadBefore = payloadBefore.Count > 0;
        var hasPayloadAfter = payloadAfter.Count > 0;
        var effectiveAfterMap = hasPayloadAfter ? payloadAfter : currentMap;

        if (hasPayloadBefore && hasPayloadAfter)
        {
            return DataResponse<AuditLogDiffResultDto>.Succeed(
                BuildDiffResult(current.Id, null, _auditLogOptions.DiffSourcePayload, payloadBefore, payloadAfter));
        }

        if (effectiveAfterMap.Count > 0 && previousMap.Count > 0 && previous != null)
        {
            return DataResponse<AuditLogDiffResultDto>.Succeed(
                BuildDiffResult(current.Id, previous.Id, _auditLogOptions.DiffSourcePreviousLog, previousMap, effectiveAfterMap));
        }

        if (hasPayloadBeforeAfter && (hasPayloadBefore || hasPayloadAfter))
        {
            return DataResponse<AuditLogDiffResultDto>.Succeed(
                BuildDiffResult(current.Id, null, _auditLogOptions.DiffSourcePayload, payloadBefore, effectiveAfterMap));
        }

        if (TryExtractBeforeAfterFromSingleMap(currentMap, out var pairedBefore, out var pairedAfter))
        {
            return DataResponse<AuditLogDiffResultDto>.Succeed(
                BuildDiffResult(current.Id, null, _auditLogOptions.DiffSourcePayload, pairedBefore, pairedAfter));
        }

        return DataResponse<AuditLogDiffResultDto>.Succeed(BuildSnapshotResult(current.Id, currentMap));
    }

    private static void MarkBroken(IntegrityStats stats, long auditLogId)
    {
        stats.BrokenChainCount++;
        if (stats.BrokenSampleAuditLogIds.Count < 10 &&
            !stats.BrokenSampleAuditLogIds.Contains(auditLogId))
        {
            stats.BrokenSampleAuditLogIds.Add(auditLogId);
        }
    }

    private static string NormalizeAuditTableName(string tableName)
    {
        var normalized = tableName
            .Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal)
            .Trim();

        var dotIndex = normalized.LastIndexOf('.');
        if (dotIndex >= 0 && dotIndex < normalized.Length - 1)
        {
            normalized = normalized[(dotIndex + 1)..];
        }

        return normalized;
    }

    private static string ResolveHardeningSecret(IConfiguration configuration)
    {
        var secret = configuration["Auditing:Hardening:HmacSecret"];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            return secret.Trim();
        }

        var jwtSecret = configuration["JwtSettings:Secret"];
        if (!string.IsNullOrWhiteSpace(jwtSecret))
        {
            return jwtSecret.Trim();
        }

        return string.Empty;
    }

    private IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, GetAuditLogsRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim();
            query = query.Where(x =>
                x.ServiceName.Contains(search) ||
                x.MethodName.Contains(search) ||
                x.Parameters.Contains(search) ||
                (x.UserId != null && x.UserId.Contains(search)) ||
                (x.ClientIpAddress != null && x.ClientIpAddress.Contains(search)));
        }

        if (request.Id.HasValue)
        {
            query = query.Where(x => x.Id == request.Id.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ServiceName))
        {
            var serviceName = request.ServiceName.Trim();
            query = query.Where(x => x.ServiceName == serviceName);
        }

        if (!string.IsNullOrWhiteSpace(request.MethodName))
        {
            var methodName = request.MethodName.Trim();
            query = query.Where(x => x.MethodName == methodName);
        }

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            var userId = request.UserId.Trim();
            query = query.Where(x => x.UserId == userId);
        }

        if (request.OnlyErrors)
        {
            query = query.Where(x => x.Exception != null && x.Exception.Trim() != string.Empty);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(x => x.ExecutionTime >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(x => x.ExecutionTime <= request.ToUtc.Value);
        }

        return query;
    }

    private IntegrityStats GetHardeningStats(
        IReadOnlyList<AuditLogIntegrity> integrityEntries,
        IReadOnlyDictionary<long, AuditLog> hardenedLogs,
        string normalizedAuditTable)
    {
        var stats = new IntegrityStats();
        var expectedPreviousHash = string.Empty;

        foreach (var integrityEntry in integrityEntries)
        {
            stats.HardenedCount++;

            var auditLogId = integrityEntry.AuditLogId;
            stats.LastHardenedAuditLogId = auditLogId;

            var previousHash = integrityEntry.PreviousHash ?? string.Empty;
            var currentHash = integrityEntry.CurrentHash ?? string.Empty;

            if (!string.Equals(previousHash, expectedPreviousHash, StringComparison.OrdinalIgnoreCase))
            {
                MarkBroken(stats, auditLogId);
            }

            if (!hardenedLogs.TryGetValue(auditLogId, out var auditLog))
            {
                MarkBroken(stats, auditLogId);
                expectedPreviousHash = currentHash;
                continue;
            }

            var expectedHash = AuditIntegrityHasher.ComputeHash(
                _hardeningSecret,
                normalizedAuditTable,
                previousHash,
                auditLogId,
                auditLog.UserId,
                auditLog.ServiceName,
                auditLog.MethodName,
                auditLog.Parameters,
                auditLog.ExecutionTime,
                auditLog.ExecutionDuration,
                auditLog.ClientIpAddress,
                auditLog.BrowserInfo,
                auditLog.Exception);

            if (!string.Equals(currentHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                MarkBroken(stats, auditLogId);
            }

            expectedPreviousHash = currentHash;
        }

        return stats;
    }

    private Task<List<long>> GetMissingHardeningSampleIdsAsync(
        string normalizedAuditTable,
        CancellationToken cancellationToken)
    {
        if (_auditLogOptions.IntegritySampleSize <= 0)
        {
            return Task.FromResult(new List<long>());
        }

        var hardenedIds = _auditLogIntegrityRepository.Query()
            .Where(x => x.AuditTable == normalizedAuditTable)
            .Select(x => x.AuditLogId);

        return _auditLogRepository.Query()
            .AsNoTracking()
            .Where(x => !hardenedIds.Contains(x.Id))
            .OrderByDescending(x => x.Id)
            .Take(_auditLogOptions.IntegritySampleSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<AuditRecord?> GetAuditRecordByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        return await _auditLogRepository.Query()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AuditRecord
            {
                Id = x.Id,
                UserId = x.UserId,
                ServiceName = x.ServiceName,
                MethodName = x.MethodName,
                Parameters = x.Parameters,
                ExecutionTime = x.ExecutionTime,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<List<AuditRecord>> GetPreviousCandidatesAsync(
        AuditRecord current,
        CancellationToken cancellationToken)
    {
        return await _auditLogRepository.Query()
            .AsNoTracking()
            .Where(x =>
                x.Id < current.Id &&
                x.ServiceName == current.ServiceName &&
                x.MethodName == current.MethodName &&
                x.UserId == current.UserId)
            .OrderByDescending(x => x.ExecutionTime)
            .ThenByDescending(x => x.Id)
            .Take(50)
            .Select(x => new AuditRecord
            {
                Id = x.Id,
                UserId = x.UserId,
                ServiceName = x.ServiceName,
                MethodName = x.MethodName,
                Parameters = x.Parameters,
                ExecutionTime = x.ExecutionTime,
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<AuditRecord?> FindComparablePreviousRecordAsync(
        AuditRecord current,
        IReadOnlyDictionary<string, string?> currentMap,
        CancellationToken cancellationToken)
    {
        if (currentMap.Count == 0)
        {
            return null;
        }

        var identityValues = ExtractIdentityValues(currentMap);
        if (identityValues.Count == 0)
        {
            return null;
        }

        var candidates = await GetPreviousCandidatesAsync(current, cancellationToken);
        if (candidates.Count == 0)
        {
            return null;
        }

        AuditRecord? bestMatch = null;
        var bestScore = 0;

        foreach (var candidate in candidates)
        {
            var candidateMap = ParseParameterMap(candidate.Parameters);
            if (candidateMap.Count == 0)
            {
                continue;
            }

            var score = ScoreIdentityMatch(identityValues, candidateMap);
            if (score <= 0 || score < bestScore)
            {
                continue;
            }

            bestScore = score;
            bestMatch = candidate;

            if (bestScore >= identityValues.Count)
            {
                break;
            }
        }

        return bestMatch;
    }

    private static AuditLogDiffResultDto BuildDiffResult(
        long logId,
        long? comparedLogId,
        string source,
        IReadOnlyDictionary<string, string?> beforeMap,
        IReadOnlyDictionary<string, string?> afterMap)
    {
        var changes = BuildDiffEntries(beforeMap, afterMap);
        return new AuditLogDiffResultDto
        {
            LogId = logId,
            ComparedLogId = comparedLogId,
            Source = source,
            HasComparableData = changes.Count > 0,
            Changes = changes,
        };
    }

    private AuditLogDiffResultDto BuildSnapshotResult(
        long logId,
        IReadOnlyDictionary<string, string?> snapshotMap)
    {
        var changes = snapshotMap
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new AuditFieldDiffDto
            {
                Field = x.Key,
                Before = null,
                After = x.Value,
                Changed = false,
            })
            .ToList();

        return new AuditLogDiffResultDto
        {
            LogId = logId,
            ComparedLogId = null,
            Source = _auditLogOptions.DiffSourceSnapshot,
            HasComparableData = false,
            Changes = changes,
        };
    }

    private static List<AuditFieldDiffDto> BuildDiffEntries(
        IReadOnlyDictionary<string, string?> beforeMap,
        IReadOnlyDictionary<string, string?> afterMap)
    {
        return beforeMap.Keys
            .Union(afterMap.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(key =>
            {
                beforeMap.TryGetValue(key, out var before);
                afterMap.TryGetValue(key, out var after);

                return new AuditFieldDiffDto
                {
                    Field = key,
                    Before = before,
                    After = after,
                    Changed = !string.Equals(before?.Trim(), after?.Trim(), StringComparison.Ordinal),
                };
            })
            .ToList();
    }

    private static Dictionary<string, string?> ParseParameterMap(string? parameters)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return result;
        }

        if (TryParseJsonObject(parameters, out var jsonMap))
        {
            return jsonMap;
        }

        foreach (var segment in SplitTopLevel(parameters, ',', ';'))
        {
            var separatorIndex = FindSeparatorIndex(segment);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = NormalizeFieldAlias(segment[..separatorIndex].Trim());
            var value = segment[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result[key] = UnwrapValue(value);
        }

        return result;
    }

    private static bool TryParseBeforeAfterPayload(
        string? parameters,
        out Dictionary<string, string?> beforeMap,
        out Dictionary<string, string?> afterMap)
    {
        beforeMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        afterMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(parameters))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(parameters);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryGetJsonProperty(document.RootElement, "before", out var beforeElement) ||
                !TryGetJsonProperty(document.RootElement, "after", out var afterElement))
            {
                return false;
            }

            beforeMap = ParseJsonElementToMap(beforeElement);
            afterMap = ParseJsonElementToMap(afterElement);
            return beforeMap.Count > 0 || afterMap.Count > 0;
        }
        catch
        {
            var singleMap = ParseParameterMap(parameters);
            return TryExtractBeforeAfterFromSingleMap(singleMap, out beforeMap, out afterMap);
        }
    }

    private static bool TryParseJsonObject(
        string parameters,
        out Dictionary<string, string?> map)
    {
        map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var document = JsonDocument.Parse(parameters);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            map = ParseJsonElementToMap(document.RootElement);
            return map.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, string?> ParseJsonElementToMap(JsonElement element)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        FlattenJsonElement(map, string.Empty, element);
        return map;
    }

    private static void FlattenJsonElement(
        IDictionary<string, string?> target,
        string prefix,
        JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var key = string.IsNullOrWhiteSpace(prefix)
                    ? property.Name
                    : $"{prefix}.{property.Name}";

                FlattenJsonElement(target, key, property.Value);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return;
        }

        target[prefix] = element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.Array => element.GetRawText(),
            _ => element.GetRawText(),
        };
    }

    private static bool TryExtractBeforeAfterFromSingleMap(
        IReadOnlyDictionary<string, string?> parsedMap,
        out Dictionary<string, string?> beforeMap,
        out Dictionary<string, string?> afterMap)
    {
        beforeMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        afterMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in parsedMap)
        {
            if (!TryGetPairedField(key, out var normalizedField, out var isBefore))
            {
                continue;
            }

            if (isBefore)
            {
                beforeMap[normalizedField] = value;
            }
            else
            {
                afterMap[normalizedField] = value;
            }
        }

        return beforeMap.Count > 0 || afterMap.Count > 0;
    }

    private static bool TryGetPairedField(string key, out string field, out bool isBefore)
    {
        field = string.Empty;
        isBefore = false;

        var normalized = key.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var beforePrefixes = new[] { "before", "old" };
        foreach (var prefix in beforePrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                normalized.Length > prefix.Length)
            {
                field = NormalizeFieldName(normalized[prefix.Length..]);
                isBefore = true;
                return !string.IsNullOrWhiteSpace(field);
            }
        }

        var afterPrefixes = new[] { "after", "new" };
        foreach (var prefix in afterPrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                normalized.Length > prefix.Length)
            {
                field = NormalizeFieldName(normalized[prefix.Length..]);
                isBefore = false;
                return !string.IsNullOrWhiteSpace(field);
            }
        }

        var beforeSuffixes = new[] { "_before", ".before", "_old", ".old" };
        foreach (var suffix in beforeSuffixes)
        {
            if (!normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ||
                normalized.Length <= suffix.Length)
            {
                continue;
            }

            field = NormalizeFieldName(normalized[..^suffix.Length]);
            isBefore = true;
            return !string.IsNullOrWhiteSpace(field);
        }

        var afterSuffixes = new[] { "_after", ".after", "_new", ".new" };
        foreach (var suffix in afterSuffixes)
        {
            if (!normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ||
                normalized.Length <= suffix.Length)
            {
                continue;
            }

            field = NormalizeFieldName(normalized[..^suffix.Length]);
            isBefore = false;
            return !string.IsNullOrWhiteSpace(field);
        }

        return false;
    }

    private static string NormalizeFieldName(string value)
    {
        return value
            .Trim('_', '.', '-', ' ')
            .Replace("__", "_", StringComparison.Ordinal)
            .Replace("..", ".", StringComparison.Ordinal);
    }

    private static string NormalizeFieldAlias(string value)
    {
        var normalized = NormalizeFieldName(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        if (TryStripSemanticPrefix(normalized, "before", out var stripped) ||
            TryStripSemanticPrefix(normalized, "after", out stripped) ||
            TryStripSemanticPrefix(normalized, "old", out stripped) ||
            TryStripSemanticPrefix(normalized, "new", out stripped))
        {
            normalized = stripped;
        }

        if (normalized.Equals("FileName", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("FolderName", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("OldName", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("NewName", StringComparison.OrdinalIgnoreCase))
        {
            return "Name";
        }

        return normalized;
    }

    private static bool TryStripSemanticPrefix(string value, string prefix, out string stripped)
    {
        stripped = value;

        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            value.Length <= prefix.Length)
        {
            return false;
        }

        var tail = value[prefix.Length..];
        if (tail.Length == 0)
        {
            return false;
        }

        if (tail[0] is '.' or '_' or '-')
        {
            tail = tail[1..];
        }

        stripped = NormalizeFieldName(tail);
        return !string.IsNullOrWhiteSpace(stripped);
    }

    private static IEnumerable<string> SplitTopLevel(string input, params char[] separators)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            yield break;
        }

        var start = 0;
        var inQuotes = false;
        var nesting = 0;

        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            if (ch == '"' && (i == 0 || input[i - 1] != '\\'))
            {
                inQuotes = !inQuotes;
            }

            if (inQuotes)
            {
                continue;
            }

            if (ch is '{' or '[' or '(')
            {
                nesting++;
                continue;
            }

            if (ch is '}' or ']' or ')')
            {
                nesting = Math.Max(0, nesting - 1);
                continue;
            }

            if (nesting > 0 || !separators.Contains(ch))
            {
                continue;
            }

            var segment = input[start..i].Trim();
            if (!string.IsNullOrWhiteSpace(segment))
            {
                yield return segment;
            }

            start = i + 1;
        }

        if (start >= input.Length)
        {
            yield break;
        }

        var last = input[start..].Trim();
        if (!string.IsNullOrWhiteSpace(last))
        {
            yield return last;
        }
    }

    private static int FindSeparatorIndex(string segment)
    {
        var equalsIndex = segment.IndexOf('=');
        var colonIndex = segment.IndexOf(':');

        if (equalsIndex < 0)
        {
            return colonIndex;
        }

        if (colonIndex < 0)
        {
            return equalsIndex;
        }

        return Math.Min(equalsIndex, colonIndex);
    }

    private static string? UnwrapValue(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        if (trimmed.Length >= 2 &&
            ((trimmed.StartsWith('"') && trimmed.EndsWith('"')) ||
             (trimmed.StartsWith('\'') && trimmed.EndsWith('\''))))
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }

    private static bool TryGetJsonProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        foreach (var item in element.EnumerateObject())
        {
            if (!string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            property = item.Value;
            return true;
        }

        property = default;
        return false;
    }

    private static Dictionary<string, string?> ExtractIdentityValues(IReadOnlyDictionary<string, string?> map)
    {
        var identityValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in map)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalized = key.Trim().ToLowerInvariant();
            if (normalized is "id" or "key" or "culture" ||
                normalized.EndsWith("id", StringComparison.Ordinal))
            {
                identityValues[key] = value.Trim();
            }
        }

        if (identityValues.Count == 0 &&
            map.TryGetValue("Path", out var pathValue) &&
            TryExtractPathIdentity(pathValue, out var pathIdentity))
        {
            identityValues["PathId"] = pathIdentity;
        }

        return identityValues;
    }

    private static int ScoreIdentityMatch(
        IReadOnlyDictionary<string, string?> expected,
        IReadOnlyDictionary<string, string?> candidate)
    {
        var score = 0;
        foreach (var (key, expectedValue) in expected)
        {
            if (!candidate.TryGetValue(key, out var candidateValue))
            {
                continue;
            }

            if (string.Equals(expectedValue?.Trim(), candidateValue?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }
        }

        return score;
    }

    private static bool TryExtractPathIdentity(string? path, out string identity)
    {
        identity = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var sanitized = path.Trim();
        if (sanitized.Contains('?', StringComparison.Ordinal))
        {
            sanitized = sanitized.Split('?', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        }

        var parts = sanitized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var tail = parts[^1];
        if (long.TryParse(tail, out _))
        {
            identity = tail;
            return true;
        }

        return false;
    }

    private sealed class IntegrityStats
    {
        public int HardenedCount { get; set; }
        public long? LastHardenedAuditLogId { get; set; }
        public int BrokenChainCount { get; set; }
        public List<long> BrokenSampleAuditLogIds { get; } = new();
    }

    private sealed class AuditRecord
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string Parameters { get; set; } = string.Empty;
        public DateTime ExecutionTime { get; set; }
    }

    private GetAuditLogsRequest NormalizeRequest(GetAuditLogsRequest request)
    {
        long? normalizedId = request.Id.HasValue && request.Id.Value > 0
            ? request.Id.Value
            : null;
        var normalizedPageNumber = _pagingOptions.NormalizePageNumber(request.PageNumber);
        var normalizedPageSize = _pagingOptions.NormalizePageSize(request.PageSize);

        return new GetAuditLogsRequest
        {
            Id = normalizedId,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            SearchTerm = request.SearchTerm,
            ServiceName = request.ServiceName,
            MethodName = request.MethodName,
            UserId = request.UserId,
            OnlyErrors = request.OnlyErrors,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
        };
    }
}
