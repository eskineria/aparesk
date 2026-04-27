namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;

public class SiteListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? District { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public int BlockCount { get; set; }
    public int UnitCount { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
