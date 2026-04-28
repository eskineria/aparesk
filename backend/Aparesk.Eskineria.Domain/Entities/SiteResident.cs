using Aparesk.Eskineria.Domain.Enums;

namespace Aparesk.Eskineria.Domain.Entities;

public class SiteResident
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Guid? UnitId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? IdentityNumber { get; set; }
    public ResidentType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Occupation { get; set; }
    public DateOnly? MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }
    public bool KvkkConsentGiven { get; set; }
    public bool CommunicationConsentGiven { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public string? OwnerFirstName { get; set; }
    public string? OwnerLastName { get; set; }
    public string? OwnerPhone { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Site Site { get; set; } = null!;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Unit? Unit { get; set; }

    public ICollection<HouseholdMember> HouseholdMembers { get; set; } = new List<HouseholdMember>();
}
