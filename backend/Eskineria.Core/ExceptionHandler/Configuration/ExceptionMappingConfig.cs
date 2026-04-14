namespace Eskineria.Core.ExceptionHandler.Configuration;

internal class ExceptionMappingConfig
{
    public int StatusCode { get; set; }
    public string? Title { get; set; }
    public string? ErrorCode { get; set; }
}
