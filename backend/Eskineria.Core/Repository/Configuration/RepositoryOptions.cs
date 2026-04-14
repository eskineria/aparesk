namespace Eskineria.Core.Repository.Configuration;

public class RepositoryOptions
{
    public int DefaultPageSize { get; set; } = 10;
    public int MaxPageSize { get; set; } = 100;
    public bool AutoSave { get; set; } = true;
    public bool EnableNoTracking { get; set; } = true;
}
