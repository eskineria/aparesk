using Aparesk.Eskineria.Domain.Enums;

namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;

public class UnitListItemDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public Guid? SiteBlockId { get; set; }
    public string? BlockName { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? DoorNumber { get; set; }
    public UnitType Type { get; set; }
    public int? FloorNumber { get; set; }
    public decimal? GrossAreaSquareMeters { get; set; }
    public decimal? NetAreaSquareMeters { get; set; }
    public decimal? LandShare { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
