using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using System.Threading.Channels;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;
    private readonly ILogger<BackgroundTaskQueue> _logger;

    public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger)
    {
        // Configure the channel's behavior
        var options = new BoundedChannelOptions(100) // Set the maximum capacity
        {
            FullMode = BoundedChannelFullMode.Wait // Determine behavior when the channel is full
        };

        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
        _logger = logger;
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        await _queue.Writer.WriteAsync(workItem);
        _logger.LogInformation("Task queued. Queue length: {QueueLength}", _queue.Reader.Count);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);
        _logger.LogInformation("Task dequeued. Queue length: {QueueLength}", _queue.Reader.Count);
        return workItem;
    }
}
