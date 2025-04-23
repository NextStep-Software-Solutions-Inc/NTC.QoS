namespace NTC.QoS.Domain.Models.DTOs.File;

public class ProcessingResultDto
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
    public int SuccessfulCount { get; set; }
    public int TotalProcessed { get; set; }
}
