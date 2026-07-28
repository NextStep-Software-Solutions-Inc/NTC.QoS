using System;

namespace NTC.QoS.Domain.Models.DTOs.File;

public class PreSignedUrlDto
{
    public required string Url { get; set; }
    public string? BlobPath { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
