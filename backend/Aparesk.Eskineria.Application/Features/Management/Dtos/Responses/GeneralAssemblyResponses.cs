using Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;
using Aparesk.Eskineria.Domain.Enums;

namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;

public class GeneralAssemblyListItemDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public DateTime MeetingDate { get; set; }
    public string Term { get; set; } = string.Empty;
    public MeetingType Type { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class GeneralAssemblyDetailDto : GeneralAssemblyListItemDto
{
    public List<GeneralAssemblyAgendaItemDto> AgendaItems { get; set; } = new();
    public List<GeneralAssemblyDecisionDto> Decisions { get; set; } = new();
    public List<GeneralAssemblyBoardMemberResponseDto> BoardMembers { get; set; } = new();
}

public class GeneralAssemblyAgendaItemDto
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class GeneralAssemblyBoardMemberResponseDto : GeneralAssemblyBoardMemberDto
{
    public string ResidentName { get; set; } = string.Empty;
    public string? UnitNumber { get; set; }
}
