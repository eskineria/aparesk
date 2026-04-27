namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;

public class CreateBlockRequest
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? FloorCount { get; set; }
    public int? UnitsPerFloor { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
