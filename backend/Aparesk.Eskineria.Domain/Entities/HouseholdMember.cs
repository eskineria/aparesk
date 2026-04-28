namespace Aparesk.Eskineria.Domain.Entities;

public class HouseholdMember
{
    public Guid Id { get; set; }
    public Guid ResidentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Relationship { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public SiteResident Resident { get; set; } = null!;
}
