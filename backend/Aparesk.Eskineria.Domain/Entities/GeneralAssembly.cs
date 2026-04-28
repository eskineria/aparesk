using Aparesk.Eskineria.Domain.Enums;

namespace Aparesk.Eskineria.Domain.Entities;

public class GeneralAssembly
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public DateOnly MeetingDate { get; set; }
    public string Term { get; set; } = string.Empty; // e.g. "2024-2025"
    public MeetingType Type { get; set; }
    public bool IsCompleted { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Site Site { get; set; } = null!;

    public ICollection<GeneralAssemblyAgendaItem> AgendaItems { get; set; } = new List<GeneralAssemblyAgendaItem>();
    public ICollection<GeneralAssemblyDecision> Decisions { get; set; } = new List<GeneralAssemblyDecision>();
    public ICollection<BoardMember> BoardMembers { get; set; } = new List<BoardMember>();
}
