using Eskineria.Core.Auditing.Requests;
using Eskineria.Core.Auditing.Responses;
using Eskineria.Core.Shared.Response;

namespace Eskineria.Core.Auditing.Abstractions;

public interface IAuditLogService
{
    Task<PagedResponse<AuditLogListItemDto>> GetPagedAsync(
        GetAuditLogsRequest request,
        CancellationToken cancellationToken = default);

    Task<DataResponse<AuditLogFilterOptionsDto>> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<DataResponse<AuditAlertsSummaryDto>> GetAlertsAsync(
        CancellationToken cancellationToken = default);

    Task<DataResponse<AuditIntegritySummaryDto>> GetIntegritySummaryAsync(
        CancellationToken cancellationToken = default);

    Task<DataResponse<AuditLogDiffResultDto>> GetDiffAsync(
        long id,
        CancellationToken cancellationToken = default);
}
