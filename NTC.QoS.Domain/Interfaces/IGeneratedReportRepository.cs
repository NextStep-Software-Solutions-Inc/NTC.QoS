using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Repository interface for GeneratedReport entity
/// </summary>
public interface IGeneratedReportRepository : IRepository<GeneratedReport>
{
    /// <summary>
    /// Get all reports for a specific user with optional pagination
    /// </summary>
    Task<List<GeneratedReport>> GetUserReportsAsync(Guid userId, int? skip = null, int? take = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific report by ID for a user
    /// </summary>
    Task<GeneratedReport?> GetUserReportByIdAsync(Guid reportId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count total reports for a user
    /// </summary>
    Task<int> CountUserReportsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find the most semantically similar reports using vector embedding search
    /// </summary>
    /// <param name="queryEmbedding">The embedding vector to compare against</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of report contents ordered by semantic similarity</returns>
    Task<List<string>> FindSimilarReportsAsync(float[] queryEmbedding, int maxResults, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a generated report with its embedding vector
    /// </summary>
    /// <param name="report">The report entity to save</param>
    /// <param name="embedding">The embedding vector for semantic search (can be null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddReportWithEmbeddingAsync(GeneratedReport report, float[]? embedding, CancellationToken cancellationToken = default);
}
