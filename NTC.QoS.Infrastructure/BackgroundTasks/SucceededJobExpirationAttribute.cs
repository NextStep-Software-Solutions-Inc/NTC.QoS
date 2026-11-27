using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Shortens Hangfire job history retention for succeeded high-frequency jobs (such as polling jobs)
/// while preserving default retention for failed jobs so operators can still inspect errors.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SucceededJobExpirationAttribute(int expirationMinutes = 60) : JobFilterAttribute, IApplyStateFilter
{
    public TimeSpan ExpirationTimeout { get; } = TimeSpan.FromMinutes(Math.Max(1, expirationMinutes));

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is SucceededState)
        {
            context.JobExpirationTimeout = ExpirationTimeout;
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
    }
}
