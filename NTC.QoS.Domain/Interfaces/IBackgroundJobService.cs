using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a background job to be executed immediately with parameters.
    /// </summary>
    /// <param name="jobExpression">The job expression to execute.</param>
    /// <returns>The job ID.</returns>
    string Enqueue<TJob>(Expression<Action<TJob>> jobExpression);

    /// <summary>
    /// Schedules a background job to be executed at a specific time with parameters.
    /// </summary>
    /// <param name="jobExpression">The job expression to execute.</param>
    /// <param name="enqueueAt">The time to execute the job.</param>
    /// <returns>The job ID.</returns>
    string Schedule(Expression<Action> jobExpression, DateTime enqueueAt);

    /// <summary>
    /// Schedules a background job to be executed after a delay with parameters.
    /// </summary>
    /// <param name="jobExpression">The job expression to execute.</param>
    /// <param name="delay">The delay after which the job will be executed.</param>
    /// <returns>The job ID.</returns>
    string Schedule(Expression<Action> jobExpression, TimeSpan delay);

    /// <summary>
    /// Adds or updates a recurring job with parameters.
    /// </summary>
    /// <param name="recurringJobId">The ID of the recurring job.</param>
    /// <param name="jobExpression">The job expression to execute.</param>
    /// <param name="cronExpression">The CRON expression for the job schedule.</param>
    void AddOrUpdateRecurringJob(string recurringJobId, Expression<Action> jobExpression, string cronExpression);

    /// <summary>
    /// Removes a recurring job.
    /// </summary>
    /// <param name="recurringJobId">The ID of the recurring job to remove.</param>
    void RemoveRecurringJob(string recurringJobId);

    /// <summary>
    /// Deletes a background job by its ID.
    /// </summary>
    /// <param name="jobId">The ID of the job to delete.</param>
    void DeleteJob(string jobId);

    /// <summary>
    /// Cancels an active background job by its ID.
    /// </summary>
    /// <param name="jobId">The ID of the job to cancel.</param>
    void CancelActiveJob(string jobId);
}

