using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Background job interface for processing CellularCoverage V2 CSV uploads
/// </summary>
public interface IProcessCellularCoverageV2Job
{
    /// <summary>
    /// Executes the V2 coverage processing job
    /// </summary>
    /// <param name="fileDto">File metadata</param>
    /// <param name="networkId">User-selected network</param>
    /// <param name="bandId">User-selected band</param>
    /// <param name="municipalityId">Municipality scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(
        FileDto fileDto,
        Guid networkId,
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken);
}
