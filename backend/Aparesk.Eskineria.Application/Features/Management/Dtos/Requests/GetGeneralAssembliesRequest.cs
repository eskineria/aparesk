using System.ComponentModel;

namespace Aparesk.Eskineria.Application.Features.Management.Dtos.Requests;

public class GetGeneralAssembliesRequest
{
    [DefaultValue(1)]
    public int PageNumber { get; set; } = 1;

    [DefaultValue(20)]
    public int PageSize { get; set; } = 20;

    public string? SearchTerm { get; set; }
    
    public Guid? SiteId { get; set; }
    
    public bool? IncludeCompleted { get; set; }
}
