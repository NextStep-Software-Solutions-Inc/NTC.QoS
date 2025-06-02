using MediatR;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Handlers.Commands;

public class CreateAntennaPatternHandler (IRepository<Domain.Entities.AntennaPattern> repository): IRequestHandler<CreateAntennaPatternRequest, ApiResponse<CreateAntennaPatternDto>>
{
    private readonly IRepository<Domain.Entities.AntennaPattern> _repository = repository;

    public async Task<ApiResponse<CreateAntennaPatternDto>> Handle(CreateAntennaPatternRequest request, CancellationToken cancellationToken)
    {
        var antennaPattern = _repository.Query(ap => ap.Name == request.AntennaPattern.Name && !ap.IsDeleted);
        var existingAntennaPattern = await _repository.FirstOrDefaultAsync(antennaPattern, cancellationToken);

        if (existingAntennaPattern is not null)
        {
            return new ApiResponse<CreateAntennaPatternDto>
            {
                Message = "Antenna pattern already exists.",
                Succeeded = false
            };
        }

        var newAntennaPattern = new Domain.Entities.AntennaPattern
        {
            Name = request.AntennaPattern.Name,
            URL = request.AntennaPattern.URL,
        };

        _repository.Add(newAntennaPattern);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateAntennaPatternDto>
        {
            Data = new CreateAntennaPatternDto
            {
                Name = newAntennaPattern.Name,
                URL = newAntennaPattern.URL,
            },
            Succeeded = true,
            Message = "Antenna pattern created successfully.",
        };
    }
}