namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Responses;

public class UnitDetailDto : UnitListItemDto
{
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
