using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Services;

public class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                await workItem(stoppingToken); // Execute the task
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                _logger.LogInformation("Task cancelled");
            }
            catch (Exception ex)
            {
                // Handle errors during task execution
                _logger.LogError(ex, "Error occurred while executing task.");
            }
        }

        _logger.LogInformation("Queued Hosted Service is stopping.");
    }
}
