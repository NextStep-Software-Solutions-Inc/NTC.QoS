using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Region.Handlers.Commands;

public class DeleteRegionHandler(IRegionRepository repository) : IRequestHandler<DeleteRegionRequest, ApiResponse<bool>>
{
    private readonly IRegionRepository _repository = repository;

    public async Task<ApiResponse<bool>> Handle(DeleteRegionRequest request, CancellationToken cancellationToken)
    {
        var regionQuery = _repository.Query(r => r.Id == request.Id);
        var region = await _repository.FirstOrDefaultAsync(regionQuery, cancellationToken);

        if (region is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Region not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullRegion", ["The Region you are trying to delete does not exist."]
                    }
                }
            };
        }

        _repository.Delete(region);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = $"{region.Name} deleted successfully.",
            Data = true
        };
    }
}

