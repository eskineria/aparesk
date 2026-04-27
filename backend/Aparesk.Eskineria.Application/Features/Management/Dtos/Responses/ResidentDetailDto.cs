namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;

public sealed class ResidentDetailDto : ResidentListItemDto
{
    public string? IdentityNumber { get; set; }
    public string? Occupation { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool KvkkConsentGiven { get; set; }
    public bool CommunicationConsentGiven { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
