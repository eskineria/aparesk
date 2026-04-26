namespace Aparesk.Eskineria.Core.Auditing.Responses;

public class AuditFieldDiffDto
{
    public string Field { get; set; } = string.Empty;
    public string? Before { get; set; }
    public string? After { get; set; }
    public bool Changed { get; set; }
}
