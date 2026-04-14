using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Eskineria.Core.ExceptionHandler.Configuration;

public class ExceptionOptions
{
    internal Dictionary<Type, ExceptionMappingConfig> ExceptionMappings { get; } = new();
    internal Func<HttpContext, Exception, ProblemDetails, Task>? OnBeforeWriteResponse { get; private set; }
    internal bool IncludeExceptionDetailsInResponse { get; private set; }

    /// <summary>
    /// Maps an exception type to a specific status code.
    /// </summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="statusCode">Http status code.</param>
    /// <param name="title">Optional safe title to return to client. If null, standard HTTP phrase is used for security.</param>
    /// <param name="errorCode">Optional specific error code (e.g. ERR_001).</param>
    public ExceptionOptions Map<TException>(int statusCode, string? title = null, string? errorCode = null) where TException : Exception
    {
        ExceptionMappings[typeof(TException)] = new ExceptionMappingConfig 
        { 
            StatusCode = statusCode, 
            Title = title, 
            ErrorCode = errorCode 
        };
        return this;
    }

    public ExceptionOptions ConfigureResponse(Func<HttpContext, Exception, ProblemDetails, Task> callback)
    {
        OnBeforeWriteResponse = callback;
        return this;
    }

    public ExceptionOptions IncludeExceptionDetails(bool include = true)
    {
        IncludeExceptionDetailsInResponse = include;
        return this;
    }
}
