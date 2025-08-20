using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class ProcessCellularCoverageFileHandler(IBackgroundJobService backgroundJobService) : IRequestHandler<ProcessCellularCoverageFileRequest, bool>
{
    public Task<bool> Handle(ProcessCellularCoverageFileRequest request, CancellationToken cancellationToken)
    {
        backgroundJobService.Enqueue<IProcessCellularCoverageJob>((job) => job.ExecuteAsync(request.File, cancellationToken));
        backgroundJobService.Schedule<IDeleteUploadedFileJob>(job => job.ExecuteAsync(request.File, cancellationToken), TimeSpan.FromDays(1));

        return Task.FromResult(true);
    }
}

