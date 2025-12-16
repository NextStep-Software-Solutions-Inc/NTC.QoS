using MediatR;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.Technology.Handlers.Commands;

public class UpdateTechnologyByIdHandler(ITechnologyRepository repository) : IRequestHandler<UpdateTechnologyRequest, ApiResponse<TechnologyDto>>

{
    private readonly ITechnologyRepository _repository = repository;
    public async Task<ApiResponse<TechnologyDto>> Handle(UpdateTechnologyRequest request,
            CancellationToken cancellationToken)
    {
        var technologyQuery = _repository.Query(t => t.Id == request.Id);
        var technology = await _repository.FirstOrDefaultAsync(technologyQuery, cancellationToken);

        if (string.IsNullOrEmpty(request.Technology.Standard))
        {
            return new ApiResponse<TechnologyDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Standard", ["Standard is required."]}
                },
                Message = request.Technology.Standard + " already exists in our system. Please try a different one."
            };
        }
        if (technology is null)
        {
            return new ApiResponse<TechnologyDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", ["Technology not found."] }
                },
                Message = "The technology you are trying to edit does not exist."
            };
        }

        technology.Standard = request.Technology.Standard ?? technology.Standard;
        technology.Description = request.Technology.Description ?? technology.Description;

        _repository.Update(technology);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<TechnologyDto>
        {
            Succeeded = true,
            Data = new TechnologyDto()
            {
                Id = technology.Id,
                Standard = technology.Standard,
                Description = technology.Description,
                Generation = new()
                {
                    Id = technology.Generation.Id,
                    Name = technology.Generation.Name,
                    Description = technology.Generation.Description
                }
            },
            Message = "Technology updated successfully."
        };
    }
}