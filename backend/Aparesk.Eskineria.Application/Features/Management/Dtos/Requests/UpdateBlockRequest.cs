namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;

public class UpdateBlockRequest
{
    public string Name { get; set; } = string.Empty;
    public int? FloorCount { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
