namespace Aparesk.Eskineria.Core.ExceptionHandler.Exceptions;

public interface IValidationException
{
    string Message { get; }
    IDictionary<string, string[]> Errors { get; }
}
