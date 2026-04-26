using Aparesk.Eskineria.Core.Auditing.Requests;
using Aparesk.Eskineria.Core.Auditing.Responses;
using Aparesk.Eskineria.Core.Shared.Response;

namespace Aparesk.Eskineria.Core.Auditing.Abstractions;

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
