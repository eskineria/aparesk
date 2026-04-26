using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Abstractions;
using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Models;
using Aparesk.Eskineria.Core.Repository.Specification;
using Aparesk.Eskineria.Core.Shared.Response;
using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Entities;

namespace Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Services;

public class EmailDeliveryLogService : IEmailDeliveryLogService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 200;

    private readonly IEmailDeliveryLogRepository _emailDeliveryLogRepository;

    public EmailDeliveryLogService(IEmailDeliveryLogRepository emailDeliveryLogRepository)
    {
        _emailDeliveryLogRepository = emailDeliveryLogRepository;
    }

    public async Task<PagedResponse<EmailDeliveryLogItemDto>> GetPagedAsync(
        GetEmailDeliveryLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var normalizedPageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var term = request.SearchTerm?.Trim();
        var templateKey = request.TemplateKey?.Trim();
        var status = request.Status?.Trim();
        var offset = (normalizedPageNumber - 1) * normalizedPageSize;

        var spec = new QuerySpecification<EmailDeliveryLog>(x =>
                (string.IsNullOrWhiteSpace(term) ||
                 x.Recipient.Contains(term) ||
                 x.Subject.Contains(term) ||
                 (x.ErrorMessage != null && x.ErrorMessage.Contains(term))) &&
                (string.IsNullOrWhiteSpace(templateKey) || x.TemplateKey == templateKey) &&
                (string.IsNullOrWhiteSpace(status) || x.Status == status) &&
                (!request.FromUtc.HasValue || x.CreatedAt >= request.FromUtc.Value) &&
                (!request.ToUtc.HasValue || x.CreatedAt <= request.ToUtc.Value))
            .OrderByDescending(x => x.CreatedAt)
            .Paging(offset, normalizedPageSize);

        var pagedResult = await _emailDeliveryLogRepository.GetPagedListAsync(spec, cancellationToken);

        var items = pagedResult.Items
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new EmailDeliveryLogItemDto
            {
                Id = x.Id,
                Channel = x.Channel,
                Recipient = x.Recipient,
                Subject = x.Subject,
                TemplateKey = x.TemplateKey,
                Culture = x.Culture,
                Status = x.Status,
                ProviderName = x.ProviderName,
                MessageId = x.MessageId,
                ErrorMessage = x.ErrorMessage,
                CreatedAt = x.CreatedAt,
            })
            .ToList();

        return new PagedResponse<EmailDeliveryLogItemDto>(
            items,
            pagedResult.Index,
            pagedResult.Size,
            pagedResult.Count,
            pagedResult.Pages,
            pagedResult.HasPrevious,
            pagedResult.HasNext);
    }
}
