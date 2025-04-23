namespace NTC.QoS.Domain.Interfaces;
public interface IBackgroundJob<TParams>
{
    /// <summary>  
    /// Executes the background job with custom parameters.  
    /// </summary>  
    /// <param name="parameters">The custom parameters for the job.</param>  
    /// <param name="cancellationToken">The cancellation token to observe.</param>  
    /// <returns>A task representing the asynchronous operation.</returns>  
    Task ExecuteAsync(TParams parameters, CancellationToken cancellationToken);
}
