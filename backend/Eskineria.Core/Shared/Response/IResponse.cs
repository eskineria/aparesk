namespace Eskineria.Core.Shared.Response;

public interface IResponse
{
    bool Success { get; }
    string Message { get; }
    int StatusCode { get; }
}
