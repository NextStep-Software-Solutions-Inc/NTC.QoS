using MediatR;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.Application.Features.Band.Handlers.Commands;

public class CreateBandHandler (IRepository<Domain.Entities.Band> repository): IRequestHandler<CreateBandRequest, ApiResponse<CreateBandDto>>
{
    private readonly IRepository<Domain.Entities.Band> _repository = repository;

    public async Task<ApiResponse<CreateBandDto>> Handle(CreateBandRequest request, CancellationToken cancellationToken)
    {
        var band = await _repository.FindAsync(b => b.Name == request.Band.Name && !b.IsDeleted, cancellationToken);

        if (band != null)
        {
            return new ApiResponse<CreateBandDto>
            {
                Succeeded = false,
                Message = "Band already exists."
            };
        }
                
        var newBand = new Domain.Entities.Band
        {
            Name = request.Band.Name,
            Description = request.Band.Description,
        };

        _repository.Add(newBand);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateBandDto>
        {
            Data = new CreateBandDto
            {
                Name = newBand.Name,
                Description = newBand.Description,
            },
            Succeeded = true,
            Message = "Band created successfully.",
        };
    }
}