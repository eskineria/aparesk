namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;

public class GetUnitsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? SiteId { get; set; }
    public Guid? SiteBlockId { get; set; }
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeArchived { get; set; }
}
