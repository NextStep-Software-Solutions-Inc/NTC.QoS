using MediatR;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Handlers.Commands;

public class UpdateAntennaPatternByIdHandler(IRepository<Domain.Entities.AntennaPattern> repository) : IRequestHandler<UpdateAntennaPatternByIdRequest, ApiResponse<AntennaPatternDto>>
{
    private readonly IRepository<Domain.Entities.AntennaPattern> _repository = repository;
    public async Task<ApiResponse<AntennaPatternDto>> Handle(UpdateAntennaPatternByIdRequest request,
        CancellationToken cancellationToken)
    {
        var antennaPatternQuery = _repository.Query(ap => ap.Id == request.Id);
        var antennaPattern = await _repository.FirstOrDefaultAsync(antennaPatternQuery, cancellationToken);

        if (antennaPattern is null)
        {
            return new ApiResponse<AntennaPatternDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", new[] { "Antenna Pattern not found." } }
                },
                Message = "The antenna pattern you are trying to edit does not exist.",
            };
        }

        antennaPattern.Name = request.AntennaPattern.Name ?? antennaPattern.Name;
        antennaPattern.URL = request.AntennaPattern.URL ?? antennaPattern.URL;

        _repository.Update(antennaPattern);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<AntennaPatternDto>
        {
            Succeeded = true,
            Data = new AntennaPatternDto
            {
                Id = antennaPattern.Id,
                Name = antennaPattern.Name,
                URL = antennaPattern.URL
            },
            Message = "Antenna Pattern updated successfully.",
        };
    }
}
