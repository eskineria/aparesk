using Aparesk.Eskineria.Domain.Enums;

namespace Aparesk.Eskineria.Domain.Entities;

public class Unit
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Guid? SiteBlockId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? DoorNumber { get; set; }
    public UnitType Type { get; set; }
    public int? FloorNumber { get; set; }
    public decimal? GrossAreaSquareMeters { get; set; }
    public decimal? NetAreaSquareMeters { get; set; }
    public decimal? LandShare { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public Site Site { get; set; } = null!;
    public SiteBlock? SiteBlock { get; set; }
    public ICollection<SiteResident> Residents { get; set; } = new List<SiteResident>();
}
