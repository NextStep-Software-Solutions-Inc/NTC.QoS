using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.BackgroundTasks;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class ProcessNAPFileHandler(IBackgroundJobService backgroundJobService) : IRequestHandler<ProcessNAPFileRequest, bool>
{
    public Task<bool> Handle(ProcessNAPFileRequest request, CancellationToken cancellationToken)
    {
        backgroundJobService.Enqueue<IProcessNAPJob>((job) => job.ExecuteAsync(request.File, cancellationToken));
        backgroundJobService.Schedule<IDeleteUploadedFileJob>(job => job.ExecuteAsync(request.File, cancellationToken), TimeSpan.FromDays(1));

        return Task.FromResult(true);
    }
}

