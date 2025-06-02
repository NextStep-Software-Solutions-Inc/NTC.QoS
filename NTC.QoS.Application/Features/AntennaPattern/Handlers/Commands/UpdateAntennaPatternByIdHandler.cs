using MediatR;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Handlers.Commands;

public class UpdateAntennaPatternByIdHandler(IRepository<Domain.Entities.AntennaPattern> repository) : IRequestHandler<UpdateAntennaPatternByIdRequest, ApiResponse<UpdateAntennaPatternDto>>
{
    private readonly IRepository<Domain.Entities.AntennaPattern> _repository = repository;
    public async Task<ApiResponse<UpdateAntennaPatternDto>> Handle(UpdateAntennaPatternByIdRequest request,
        CancellationToken cancellationToken)
    {
        var antennaPatternQuery = _repository.Query(ap => ap.Id == request.Id && !ap.IsDeleted);
        var antennaPattern = await _repository.FirstOrDefaultAsync(antennaPatternQuery, cancellationToken);

        if (antennaPattern is null)
        {
            return new ApiResponse<UpdateAntennaPatternDto>
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

        antennaPattern.Name = request.AntennaPattern.Name;
        antennaPattern.URL = request.AntennaPattern.URL;

        _repository.Update(antennaPattern);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdateAntennaPatternDto>
        {
            Succeeded = true,
            Data = new UpdateAntennaPatternDto
            {
                Name = request.AntennaPattern.Name,
                URL = request.AntennaPattern.URL
            },
            Message = "Antenna Pattern updated successfully.",
        };
    }
}
