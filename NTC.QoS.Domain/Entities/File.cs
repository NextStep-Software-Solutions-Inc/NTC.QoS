using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;


public class File : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public FileTypeEnum Type { get; set; } = FileTypeEnum.Unknown;
    public FileStatusEnum Status { get; set; } = FileStatusEnum.Pending;
    public Guid? RegionId { get; set; }
    public virtual Region? Region { get; set; }

    // Batch processing checkpoint tracking (for resume on failure)
    public int? LastProcessedBatchNumber { get; set; }
    public int? LastProcessedRowNumber { get; set; }
    public DateTime? LastCheckpointTime { get; set; }
    public string? ProcessingJobId { get; set; } // Hangfire job ID to prevent duplicate runs
    public string? AssessmentJobId { get; set; } // Connectivity assessment job ID for status tracking

    public virtual List<Speedtest> Speedtests { get; set; } = [];
    public virtual List<Cellular> Cellulars { get; set; } = [];
    public virtual List<Cell> Cells { get; set; } = [];
    public virtual List<NAP> NAPs { get; set; } = [];
}

