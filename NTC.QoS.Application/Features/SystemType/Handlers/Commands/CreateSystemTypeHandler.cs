using MediatR;
using NTC.QoS.Application.Features.SystemType.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Handlers.Commands;

public class CreateSystemTypeHandler(
    ISystemTypeRepository systemTypeRepository,
    ITechnologyRepository technologyRepository,
    IGenerationRepository generationRepository) : IRequestHandler<CreateSystemTypeRequest, ApiResponse<SystemTypeDto>>
{
    private readonly ISystemTypeRepository _systemTypeRepository = systemTypeRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly IGenerationRepository _generationRepository = generationRepository;

    public async Task<ApiResponse<SystemTypeDto>> Handle(CreateSystemTypeRequest request, CancellationToken cancellationToken)
    {
        var systemTypeQuery = _systemTypeRepository.Query(st => st.Name == request.SystemType.Name && st.TechnologyId == request.SystemType.TechnologyId && st.GenerationId == request.SystemType.GenerationId);
        var systemType = await _systemTypeRepository.FirstOrDefaultAsync(systemTypeQuery, cancellationToken);
        if (systemType is not null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Message = "System type already exists.",
                Succeeded = false,
            };
        }

        var technologyQuery = _technologyRepository.Query(t => t.Id == request.SystemType.TechnologyId);
        var technology = await _technologyRepository.FirstOrDefaultAsync(technologyQuery, cancellationToken);
        if (technology is null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Message = "Technology does not exist.",
                Succeeded = false,
            };
        }

        var generationQuery = _generationRepository.Query(g => g.Id == request.SystemType.GenerationId);
        var generation = await _generationRepository.FirstOrDefaultAsync(generationQuery, cancellationToken);
        if (generation is null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Message = "Generation does not exist.",
                Succeeded = false,
            };
        }

        var newSystemType = new Domain.Entities.SystemType
        {
            TechnologyId = technology.Id,
            GenerationId = generation.Id,
            Name = request.SystemType.Name,
        };

        _systemTypeRepository.Add(newSystemType);
        await _systemTypeRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<SystemTypeDto>
        {
            Data = new SystemTypeDto
            {
                Id = newSystemType.Id,
                Name = newSystemType.Name,
                Technology = technology != null ? new()
                {
                    Id = technology.Id,
                    Standard = technology.Standard,
                    Description = technology.Description,
                    Generation = new()
                    {
                        Id = technology.GenerationId,
                        Name = technology.Generation.Name,
                        Description = technology.Generation.Description,
                    }
                } : null,
                Generation = generation != null ? new()
                {
                    Id = generation.Id,
                    Name = generation.Name,
                    Description = generation.Description
                } : null
            },
            Message = "System type created successfully.",
            Succeeded = true
        };
    }
}
