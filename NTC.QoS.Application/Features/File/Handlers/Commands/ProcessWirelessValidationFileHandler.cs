using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class ProcessWirelessValidationFileHandler(IBackgroundJobService backgroundJobService) : IRequestHandler<ProcessWirelessValidationFileRequest, bool>
{
    public Task<bool> Handle(ProcessWirelessValidationFileRequest request, CancellationToken cancellationToken)
    {
        backgroundJobService.Enqueue<IProcessWirelessValidationJob>((job) => job.ExecuteAsync(request.File, cancellationToken));
        backgroundJobService.Schedule<IDeleteUploadedFileJob>(job => job.ExecuteAsync(request.File, cancellationToken), TimeSpan.FromDays(1));

        return Task.FromResult(true);
    }
}
