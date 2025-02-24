public class ApiResponse<T>
{
    public T? Data { get; set; }
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[] Error { get; set; } = Array.Empty<string>();

    public ApiResponse() { }

    public ApiResponse(T data, bool succeeded = true, string message = "")
    {
        Data = data;
        Succeeded = succeeded;
        Message = message;
    }

    public ApiResponse(string errorMessage)
    {
        Succeeded = false;
        Error = new[] { errorMessage };
    }

}
