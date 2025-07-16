using MediatR;
using NTC.QoS.Application.Features.SystemType.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Handlers.Commands;

public class UpdateSystemTypeByIdHandler(IRepository<Domain.Entities.SystemType> repository) : IRequestHandler<UpdateSystemTypeByIdRequest, ApiResponse<SystemTypeDto>>
{
    private readonly IRepository<Domain.Entities.SystemType> _repository = repository;
    public async Task<ApiResponse<SystemTypeDto>> Handle(UpdateSystemTypeByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SystemType == null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "SystemType", ["SystemType data is required."] }
                },
                Message = "The request does not contain band update data.",
            };
        }

        var systemTypeQuery = _repository.Query(st => st.Id == request.Id);
        var systemType = await _repository.FirstOrDefaultAsync(systemTypeQuery, cancellationToken);

        if (systemType == null)
        {
            return new ApiResponse<SystemTypeDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["SystemType not found."] }
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
                Technology = new()
                {
                    Id = systemType.TechnologyId,
                    Standard = systemType.Technology?.Standard ?? string.Empty,
                    Description = systemType.Technology?.Description,
                    Generation = new()
                    {
                        Id = systemType.Technology?.GenerationId ?? Guid.Empty,
                        Name = systemType.Technology?.Generation?.Name ?? string.Empty,
                        Description = systemType.Technology?.Generation?.Description,
                    }
                }
            },
            Message = "Updated successfully.",
        };
    }
}
