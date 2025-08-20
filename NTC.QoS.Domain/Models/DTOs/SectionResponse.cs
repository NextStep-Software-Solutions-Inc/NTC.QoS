namespace NTC.QoS.Domain.Models.DTOs;

public class SectionResponse<T>
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static SectionResponse<T> From(ApiResponse<T> response) => new()
    {
        Succeeded = response.Succeeded,
        Message = response.Message,
        Data = response.Data
    };

    public static SectionResponse<T> Skipped(string message) => new()
    {
        Succeeded = true,
        Message = message,
        Data = default
    };

    public static SectionResponse<T> Fail(string message) => new()
    {
        Succeeded = false,
        Message = message,
        Data = default
    };
}
