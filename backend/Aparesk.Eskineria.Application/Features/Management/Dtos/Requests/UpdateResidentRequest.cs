using Aparesk.Eskineria.Domain.Enums;

namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;

public class UpdateResidentRequest
{
    public Guid? UnitId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? IdentityNumber { get; set; }
    public ResidentType Type { get; set; } = ResidentType.Owner;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Occupation { get; set; }
    public DateOnly? MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }
    public bool KvkkConsentGiven { get; set; }
    public bool CommunicationConsentGiven { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public string? OwnerFirstName { get; set; }
    public string? OwnerLastName { get; set; }
    public string? OwnerPhone { get; set; }
    public List<UpdateHouseholdMemberRequest> HouseholdMembers { get; set; } = new();
}

public class UpdateHouseholdMemberRequest
{
    public Guid? Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Relationship { get; set; }
}
