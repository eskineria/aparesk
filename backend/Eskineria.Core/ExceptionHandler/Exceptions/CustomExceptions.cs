using System.Net;

namespace Eskineria.Core.ExceptionHandler.Exceptions;

public abstract class BaseCustomException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }

    protected BaseCustomException(string message, int statusCode = (int)HttpStatusCode.InternalServerError, string? errorCode = null) 
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public class BusinessException : BaseCustomException
{
    public BusinessException(string message) : base(message, (int)HttpStatusCode.BadRequest)
    {
    }
}

public class NotFoundException : BaseCustomException
{
    public NotFoundException(string message) : base(message, (int)HttpStatusCode.NotFound)
    {
    }
}

public class ConflictException : BaseCustomException
{
    public ConflictException(string message) : base(message, (int)HttpStatusCode.Conflict)
    {
    }
}
