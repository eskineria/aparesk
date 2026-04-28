namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;

public sealed class ResidentDetailDto : ResidentListItemDto
{
    public string? IdentityNumber { get; set; }
    public string? Occupation { get; set; }
    public bool KvkkConsentGiven { get; set; }
    public bool CommunicationConsentGiven { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? OwnerFirstName { get; set; }
    public string? OwnerLastName { get; set; }
    public string? OwnerPhone { get; set; }
    public List<HouseholdMemberDto> HouseholdMembers { get; set; } = new();
}

public class HouseholdMemberDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Relationship { get; set; }
}
