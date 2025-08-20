using MediatR;
using NTC.QoS.Application.Features.SystemType.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Handlers.Commands;

public class UpdateSystemTypeByIdHandler(ISystemTypeRepository repository) : IRequestHandler<UpdateSystemTypeByIdRequest, ApiResponse<SystemTypeDto>>
{
    private readonly ISystemTypeRepository _repository = repository;
    public async Task<ApiResponse<SystemTypeDto>> Handle(UpdateSystemTypeByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SystemType is null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "SystemType", ["System Type data is required."] }
                },
                Message = "The request does not contain system type update data.",
            };
        }

        var systemTypeQuery = _repository.Query(st => st.Id == request.Id);
        var systemType = await _repository.FirstOrDefaultAsync(systemTypeQuery, cancellationToken);

        if (systemType is null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["System Type not found."] }
                },
                Message = "The system type you are trying to edit does not exist.",
            };
        }

        systemType.Name = request.SystemType.Name ?? systemType.Name;

        _repository.Update(systemType);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<SystemTypeDto>
        {
            Succeeded = true,
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
                } : null
            },
            Message = "System type updated successfully.",
        };
    }
}
