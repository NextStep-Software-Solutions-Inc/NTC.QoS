using Hangfire;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Services
{
    internal class BackgroundJobService : IBackgroundJobService
    {
        /// <inheritdoc />
        public string Enqueue<TJob>(Expression<Action<TJob>> job)
        {
            return BackgroundJob.Enqueue(job);
        }

        /// <inheritdoc />
        public string Schedule<TJob>(Expression<Action<TJob>> job, DateTime enqueueAt)
        {
            return BackgroundJob.Schedule(job, enqueueAt);
        }

        /// <inheritdoc />
        public string Schedule<TJob>(Expression<Action<TJob>> job, TimeSpan delay)
        {
            return BackgroundJob.Schedule(job, delay);
        }

        /// <inheritdoc />
        public void AddOrUpdateRecurringJob<TJob>(string recurringJobId, Expression<Action<TJob>> job, string cronExpression)
        {
            RecurringJob.AddOrUpdate(recurringJobId, job, cronExpression);
        }

        /// <inheritdoc />
        public void RemoveRecurringJob(string recurringJobId)
        {
            RecurringJob.RemoveIfExists(recurringJobId);
        }

        /// <inheritdoc />
        public void DeleteJob(string jobId)
        {
            BackgroundJob.Delete(jobId);
        }

        /// <inheritdoc />
        public void CancelActiveJob(string jobId)
        {
            // Hangfire does not natively support "cancelling" a running job.
            // However, you can delete the job, and the job itself should respect a CancellationToken.
            BackgroundJob.Delete(jobId);
        }
    }
}
