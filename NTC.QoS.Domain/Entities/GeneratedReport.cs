using Pgvector;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Represents an AI-generated report stored in the system
/// </summary>
public class GeneratedReport : BaseEntity
{
    /// <summary>
    /// The prompt/instructions used to generate the report
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// The complete generated report content in markdown format
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Number of files that were analyzed to generate the report
    /// </summary>
    public int FilesAnalyzed { get; set; }

    /// <summary>
    /// JSON array of file metadata (names, sizes, types) that were processed
    /// </summary>
    public string? FileMetadata { get; set; }

    /// <summary>
    /// User who generated this report
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to User
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// The date and time when report generation was completed
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Optional title for the report (can be extracted from first heading or user-provided)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Vector embedding of the report content for semantic search (768 dimensions for text-embedding-004)
    /// </summary>
    [Column(TypeName = "vector(768)")]
    public Vector? Embedding { get; set; }
}
