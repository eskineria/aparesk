using Aparesk.Eskineria.Domain.Enums;

namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;

public class CreateGeneralAssemblyRequest
{
    public Guid SiteId { get; set; }
    public DateTime MeetingDate { get; set; }
    public DateTime? SecondMeetingDate { get; set; }
    public string Term { get; set; } = string.Empty;
    public string? Location { get; set; }
    public MeetingType Type { get; set; }
    public bool IsCompleted { get; set; }

    public List<GeneralAssemblyAgendaItemRequestDto> AgendaItems { get; set; } = new();
    public List<GeneralAssemblyDecisionDto> Decisions { get; set; } = new();
    public List<GeneralAssemblyBoardMemberDto> BoardMembers { get; set; } = new();
}

public class GeneralAssemblyAgendaItemRequestDto
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdateGeneralAssemblyRequest : CreateGeneralAssemblyRequest
{
}

public class GeneralAssemblyDecisionDto
{
    public Guid? Id { get; set; }
    public int DecisionNumber { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class GeneralAssemblyBoardMemberDto
{
    public Guid? Id { get; set; }
    public Guid ResidentId { get; set; }
    public BoardType BoardType { get; set; }
    public BoardMemberType MemberType { get; set; }
    public string? Title { get; set; }
}
