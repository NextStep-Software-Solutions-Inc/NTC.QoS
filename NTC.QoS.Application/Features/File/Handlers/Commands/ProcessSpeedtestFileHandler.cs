using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class ProcessSpeedtestFileHandler(IBackgroundJobService backgroundJobService) : IRequestHandler<ProcessSpeedtestFileRequest, bool>
{
    public Task<bool> Handle(ProcessSpeedtestFileRequest request, CancellationToken cancellationToken)
    {
        backgroundJobService.Enqueue<IProcessSpeedtestJob>((job) => job.ExecuteAsync(request.File, cancellationToken));
        backgroundJobService.Schedule<IDeleteUploadedFileJob>(job => job.ExecuteAsync(request.File, cancellationToken), TimeSpan.FromDays(1));

        return Task.FromResult(true);
    }
}

