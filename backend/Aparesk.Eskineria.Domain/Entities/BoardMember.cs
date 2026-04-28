using Aparesk.Eskineria.Domain.Enums;

namespace Aparesk.Eskineria.Domain.Entities;

public class BoardMember
{
    public Guid Id { get; set; }
    public Guid GeneralAssemblyId { get; set; }
    public Guid ResidentId { get; set; }
    public BoardType BoardType { get; set; }
    public BoardMemberType MemberType { get; set; }
    public string? Title { get; set; } 
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public GeneralAssembly GeneralAssembly { get; set; } = null!;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public SiteResident Resident { get; set; } = null!;
}
