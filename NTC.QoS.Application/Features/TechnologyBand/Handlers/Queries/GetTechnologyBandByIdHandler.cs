using MediatR;
using NTC.QoS.Application.Features.TechnologyBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.TechnologyBand;
using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.TechnologyBand.Handlers.Queries;

public class GetTechnologyBandByIdHandler(ITechnologyBandRepository repository) : IRequestHandler<GetTechnologyBandByIdRequest, ApiResponse<TechnologyBandDto>>
{
    private readonly ITechnologyBandRepository _repository = repository;

    public async Task<ApiResponse<TechnologyBandDto>> Handle(GetTechnologyBandByIdRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query(tb => tb.Id == request.Id)
            .Select(tb => new TechnologyBandDto
            {
                Id = tb.Id,
                Band = new BandDto
                {
                    Id = tb.Band.Id,
                    Name = tb.Band.Name,
                    Description = tb.Band.Description
                },
                Technology = new TechnologyDto
                {
                    Id = tb.Technology.Id,
                    Standard = tb.Technology.Standard,
                    Description = tb.Technology.Description,
                    Generation = new()
                    {
                        Id = tb.Technology.Generation.Id,
                        Name = tb.Technology.Generation.Name,
                        Description = tb.Technology.Generation.Description
                    }
                }
            });

        var technologyBand = await _repository.FirstOrDefaultAsync(query, cancellationToken);

        if (technologyBand is null)
        {
            return new ApiResponse<TechnologyBandDto>
            {
                Succeeded = false,
                Message = "Technology Band not found."
            };
        }

        return new ApiResponse<TechnologyBandDto>
        {
            Data = technologyBand,
            Succeeded = true,
            Message = "Technology Band retrieved successfully."
        };
    }
}