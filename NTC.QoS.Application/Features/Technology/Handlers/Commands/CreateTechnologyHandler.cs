using MediatR;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.Technology.Handlers.Commands;

public class CreateTechnologyHandler (IRepository<Domain.Entities.Technology> repository): IRequestHandler<CreateTechnologyRequest, ApiResponse<TechnologyDto>>
{
    private readonly IRepository<Domain.Entities.Technology> _repository = repository;

    public async Task<ApiResponse<TechnologyDto>> Handle(CreateTechnologyRequest request, CancellationToken cancellationToken)
    {
        var technologyQuery = _repository.Query(t => t.Standard == request.Technology.Standard);
        var technology = await _repository.FirstOrDefaultAsync(technologyQuery, cancellationToken);

        if (technology is not null)
        {
            return new ApiResponse<TechnologyDto>
            {
                Message = "Technology standard already exists.",
                Succeeded = false
            };
        }

        var newTechnology = new Domain.Entities.Technology
        {
            GenerationId = request.Technology.GenerationId,
            Standard = request.Technology.Standard,
            Description = request.Technology.Description,
        };

        _repository.Add(newTechnology);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<TechnologyDto>
        {
            Data = new TechnologyDto
            {
                GenerationId = newTechnology.GenerationId,
                Standard = newTechnology.Standard,
                Description = newTechnology.Description,
            },
            Succeeded = true,
            Message = "Technology standard created successfully.",
        };
    }
}