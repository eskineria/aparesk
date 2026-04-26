using System.Text.Json.Serialization;

namespace Aparesk.Eskineria.Core.Shared.Response;

public class DataResponse<T> : Response
{
    [JsonPropertyOrder(4)]
    public T? Data { get; set; }

    public DataResponse()
    {
    }

    public DataResponse(T? data, bool success, string message, int statusCode) : base(success, message, statusCode)
    {
        Data = data;
    }

    public static DataResponse<T> Succeed(T? data, string message = "Success", int statusCode = 200)
    {
        return new DataResponse<T>(data, true, message, statusCode);
    }
    
    public new static DataResponse<T> Fail(string message, int statusCode = 400)
    {
        return new DataResponse<T>(default, false, message, statusCode);
    }
}
