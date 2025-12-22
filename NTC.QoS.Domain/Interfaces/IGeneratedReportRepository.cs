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
}
