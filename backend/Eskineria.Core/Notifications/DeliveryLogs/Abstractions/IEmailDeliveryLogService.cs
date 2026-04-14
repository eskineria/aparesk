using Eskineria.Core.Notifications.DeliveryLogs.Models;
using Eskineria.Core.Shared.Response;

namespace Eskineria.Core.Notifications.DeliveryLogs.Abstractions;

public interface IEmailDeliveryLogService
{
    Task<PagedResponse<EmailDeliveryLogItemDto>> GetPagedAsync(
        GetEmailDeliveryLogsRequest request,
        CancellationToken cancellationToken = default);
}
