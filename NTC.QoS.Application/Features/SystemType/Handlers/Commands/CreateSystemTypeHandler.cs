using MediatR;
using NTC.QoS.Application.Features.SystemType.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Handlers.Commands;

public class CreateSystemTypeHandler(ISystemTypeRepository systemTypeRepository, ITechnologyRepository technologyRepository) : IRequestHandler<CreateSystemTypeRequest, ApiResponse<SystemTypeDto>>
{
    private readonly ISystemTypeRepository _systemTypeRepository = systemTypeRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;

    public async Task<ApiResponse<SystemTypeDto>> Handle(CreateSystemTypeRequest request, CancellationToken cancellationToken)
    {
        var systemTypeQuery = _systemTypeRepository.Query(st => !st.IsDeleted && st.Name == request.SystemType.Name && st.TechnologyId == request.SystemType.TechnologyId);
        var systemType = await _systemTypeRepository.FirstOrDefaultAsync(systemTypeQuery, cancellationToken);
        if (systemType is not null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Message = "System type already exists.",
                Succeeded = false,
            };
        }

        var technologyQuery = _technologyRepository.Query(t => !t.IsDeleted && t.Id == request.SystemType.TechnologyId);
        var technology = await _technologyRepository.FirstOrDefaultAsync(technologyQuery, cancellationToken);
        if (technology is null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Message = "Technology does not exist.",
                Succeeded = false,
            };
        }

        var newSystemType = new Domain.Entities.SystemType 
        {
            TechnologyId = technology.Id,
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
                Technology = new() 
                { 
                    Id = technology.Id,
                    Standard = technology.Standard,
                    Description = technology.Description ?? string.Empty,
                    Generation = new() 
                    {
                        Id = technology.GenerationId,
                        Name = technology.Generation?.Name ?? string.Empty
                    }
                }
            },
            Message = "System type created successfully.",
            Succeeded = true
        };
    }
}
