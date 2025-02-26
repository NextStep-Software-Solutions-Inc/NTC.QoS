using MediatR;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.Technology.Handlers.Commands;

public class UpdateTechnologyByIdHandler(IRepository<Domain.Entities.Technology> repository) : IRequestHandler<UpdateTechnologyRequest, ApiResponse<UpdateTechnologyDto>>

{
    private readonly IRepository<Domain.Entities.Technology> _repository = repository;
    public async Task<ApiResponse<UpdateTechnologyDto>> Handle(UpdateTechnologyRequest request,
            CancellationToken cancellationToken)
    {
        var technology = await _repository.FindAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);


        if (string.IsNullOrEmpty(request.Technology.Standard))
        {
            return new ApiResponse<UpdateTechnologyDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Standard is required." },
                Message = request.Technology.Standard + " already exists in our system. Please try a different one."
            };
        }
        if (technology == null)
        {
            return new ApiResponse<UpdateTechnologyDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Technology not found." },
                Message = "The technology you are trying to edit does not exist."
            };
        }
        technology.Standard = request.Technology.Standard;
        technology.Description = request.Technology.Description;

         _repository.Update(technology);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdateTechnologyDto>
        {
            Succeeded = true,
            Error = new[] {""},
            Data = new UpdateTechnologyDto()
            {
                Standard = request.Technology.Standard,
                Description = request.Technology.Description,
            },
            Message = "Request successful! Your changes have been saved.."
        };
    }
}