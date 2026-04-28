namespace Aparesk.Eskineria.Domain.Entities;

public class GeneralAssemblyDecision
{
    public Guid Id { get; set; }
    public Guid GeneralAssemblyId { get; set; }
    public int DecisionNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public GeneralAssembly GeneralAssembly { get; set; } = null!;
}
