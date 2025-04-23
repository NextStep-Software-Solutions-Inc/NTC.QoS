using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using System.Threading.Channels;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessNAPJob : IBackgroundJob
{
    private readonly ILogger<BackgroundTaskQueue> _logger;

    public ProcessNAPJob(ILogger<BackgroundTaskQueue> logger)
    {
        _logger = logger;
    }

    public void Execute()
    {
        throw new NotImplementedException();
    }
}
