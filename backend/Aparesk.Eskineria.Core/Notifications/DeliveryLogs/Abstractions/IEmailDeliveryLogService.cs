using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Models;
using Aparesk.Eskineria.Core.Shared.Response;

namespace Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Abstractions;

public interface IEmailDeliveryLogService
{
    Task<PagedResponse<EmailDeliveryLogItemDto>> GetPagedAsync(
        GetEmailDeliveryLogsRequest request,
        CancellationToken cancellationToken = default);
}
