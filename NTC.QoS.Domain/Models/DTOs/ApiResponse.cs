using System.Net;

namespace NTC.QoS.Domain.Models.DTOs;
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[] Error { get; set; } = Array.Empty<string>();
    public int StatusCode { get; set; } = (int)HttpStatusCode.OK; // Default to OK

    public ApiResponse() { }

    public ApiResponse(T data, bool succeeded = true, string message = "")
    {
        Data = data;
        Succeeded = succeeded;
        Message = message;
    }

    public ApiResponse(T data, bool succeeded = true, string message = "", int statusCode = (int)HttpStatusCode.OK)
    {
        Data = data;
        Succeeded = succeeded;
        Message = message;
        StatusCode = statusCode;
    }

    public ApiResponse(string errorMessage)
    {
        Succeeded = false;
        Error = new[] { errorMessage };
    }

    public ApiResponse(string errorMessage, int statusCode = (int)HttpStatusCode.BadRequest)
    {
        Succeeded = false;
        Error = new[] { errorMessage };
        StatusCode = statusCode;
    }

    public ApiResponse(List<string> errorMessages, int statusCode = (int)HttpStatusCode.BadRequest)
    {
        Succeeded = false;
        Error = errorMessages.ToArray();
        StatusCode = statusCode;
    }
}
