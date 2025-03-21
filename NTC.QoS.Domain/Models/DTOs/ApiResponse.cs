namespace NTC.QoS.Domain.Models.DTOs;

public class ApiResponse<T>
{

    public T? Data { get; set; }
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]> Errors { get; set; } = [];

    public ApiResponse() { }

    public ApiResponse(T data, bool succeeded = true, string message = "")
    {
        Data = data;
        Succeeded = succeeded;
        Message = message;
    }

    public ApiResponse(Dictionary<string, string[]> errors)
    {
        Succeeded = false;
        Errors = errors;
    }
    public ApiResponse(string message)
    {
        Succeeded = false;
        Message = message;
    }

}
