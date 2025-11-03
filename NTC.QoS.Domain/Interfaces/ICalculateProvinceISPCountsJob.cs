namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for background job that pre-calculates and caches ISP counts for provinces
/// This reduces database load by eliminating expensive spatial queries on every request
/// </summary>
public interface ICalculateProvinceISPCountsJob
{
    /// <summary>
    /// Execute the job to calculate and cache ISP counts for all provinces
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
