using MediatR;
using NTC.QoS.Application.Features.SystemType.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Handlers.Queries;

public class GetSystemTypeByIdHandler(ISystemTypeRepository repository)
    : IRequestHandler<GetSystemTypeByIdRequest, ApiResponse<SystemTypeDto>>
{
    private readonly ISystemTypeRepository _repository = repository;

    public async Task<ApiResponse<SystemTypeDto>> Handle(GetSystemTypeByIdRequest request, CancellationToken cancellationToken)
    {
        var systemTypeQuery = _repository.Query(st => st.Id == request.Id);
        var systemType = await _repository.FirstOrDefaultAsync(systemTypeQuery, cancellationToken);

        if (systemType is null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Succeeded = false,
                Message = "System Type not found."
            };
        }

        return new ApiResponse<SystemTypeDto>
        {
            Data = new SystemTypeDto
            {
                Id = systemType.Id,
                Name = systemType.Name,
                Technology = systemType.Technology != null ? new()
                {
                    Id = systemType.Technology.Id,
                    Standard = systemType.Technology.Standard,
                    Description = systemType.Technology.Description,
                    Generation = new()
                    {
                        Id = systemType.Technology.GenerationId,
                        Name = systemType.Technology.Generation.Name,
                        Description = systemType.Technology.Generation.Description,
                    }
                } : null,
                Generation = systemType.Generation != null ? new()
                {
                    Id = systemType.Generation.Id,
                    Name = systemType.Generation.Name,
                    Description = systemType.Generation.Description
                } : null
            },
            Succeeded = true
        };
    }
}