namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;

public class BlockListItemDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? FloorCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public int UnitCount { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
