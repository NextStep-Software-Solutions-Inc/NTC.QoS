using MediatR;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Link.Handlers.Commands;

public class DeleteLinkHandler(IRepository<Domain.Aggregates.Link> repository) : IRequestHandler<DeleteLinkByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.Link> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteLinkByIdRequest request, CancellationToken cancellationToken)
    {
        var linkQuery = _repository.Query(l => l.Id == request.Id && !l.IsDeleted);
        var link = await _repository.FirstOrDefaultAsync(linkQuery, cancellationToken);

        if (link is null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = "Link not found."
            };
        }
        _repository.Delete(link);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "Link deleted successfully."
        };
    }
}
