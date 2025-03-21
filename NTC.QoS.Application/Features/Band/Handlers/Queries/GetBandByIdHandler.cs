using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Application.Features.Band.Requests;

namespace NTC.QoS.Application.Features.Band.Handlers.Queries;

public class GetBandByIdHandler(IRepository<Domain.Entities.Band> repository) : IRequestHandler<GetBandByIdRequest, ApiResponse<BandDto>>
{
    private readonly IRepository<Domain.Entities.Band> _repository = repository;

    public async Task<ApiResponse<BandDto>> Handle(GetBandByIdRequest request, CancellationToken cancellationToken)
    {
        var band = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (band == null)
        {
            return new ApiResponse<BandDto>
            {
                Succeeded = false,
                Message = "Band not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Band", [ "Band with the specified ID does not exist."] }
                },
            };
        }

        return new ApiResponse<BandDto>
        {
            Succeeded = true,
            Message = "Band retrieved successfully",
            Data = new BandDto
            {
                Id = band.Id,
                Name = band.Name,
                Description = band.Description,
                CreatedDate = band.CreatedAt,
                CreatedBy = band.CreatedBy,
                ModifiedDate = band.UpdatedAt,
                ModifiedBy = band.UpdatedBy,
            },
        };
    }
}
