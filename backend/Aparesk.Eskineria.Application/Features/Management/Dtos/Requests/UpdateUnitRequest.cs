using Aparesk.Eskineria.Domain.Enums;

namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;

public class UpdateUnitRequest
{
    public Guid? SiteBlockId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? DoorNumber { get; set; }
    public UnitType Type { get; set; } = UnitType.Apartment;
    public int? FloorNumber { get; set; }
    public decimal? GrossAreaSquareMeters { get; set; }
    public decimal? NetAreaSquareMeters { get; set; }
    public decimal? LandShare { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
