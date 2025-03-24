using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Handlers.Queries;

public class GetAntennaPatternByIdHandler(IRepository<Domain.Entities.AntennaPattern> repository) : IRequestHandler<GetAntennaPatternByIdRequest, ApiResponse<AntennaPatternDto>>
{
    private readonly IRepository<Domain.Entities.AntennaPattern> _repository = repository;

    public async Task<ApiResponse<AntennaPatternDto>> Handle(GetAntennaPatternByIdRequest request, CancellationToken cancellationToken)
    {
        var antennaPattern = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (antennaPattern is null)
        {
            return new ApiResponse<AntennaPatternDto>
            {
                Succeeded = false,
                Message = "Antenna pattern not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "AntennaPattern", ["Antenna pattern with the specified ID does not exist."]}
                },
            };
        }

        return new ApiResponse<AntennaPatternDto>
        {
            Succeeded = true,
            Message = "Antenna pattern retrieved successfully",
            Data = new AntennaPatternDto
            {
                Id = antennaPattern.Id,
                Name = antennaPattern.Name,
                URL = antennaPattern.URL
            },
        };
    }
}