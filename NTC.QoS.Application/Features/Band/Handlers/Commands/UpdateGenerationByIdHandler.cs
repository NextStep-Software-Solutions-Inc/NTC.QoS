using MediatR;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.Application.Features.Band.Handlers.Commands;

public class UpdateBandByIdHandler(IRepository<Domain.Entities.Band> repository) : IRequestHandler<UpdateBandByIdRequest, ApiResponse<UpdateBandDto>>
{
    private readonly IRepository<Domain.Entities.Band> _repository = repository;
    public async Task<ApiResponse<UpdateBandDto>> Handle(UpdateBandByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Band == null)
        {
            return new ApiResponse<UpdateBandDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Band", ["Band data is required."] }
                },
                Message = "The request does not contain band update data.",
            };
        }

        var bandQuery = _repository.Query(b => b.Id == request.Id && !b.IsDeleted);
        var band = await _repository.FirstOrDefaultAsync(bandQuery, cancellationToken);

        if (band == null)
        {
            return new ApiResponse<UpdateBandDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Band not found."] }
                },
                Message = "The band you are trying to edit does not exist.",
            };
        }

        band.Name = request.Band.Name;
        band.Description = request.Band.Description;

        _repository.Update(band);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdateBandDto>
        {
            Succeeded = true,
            Data = new UpdateBandDto
            {
                Name = request.Band.Name,
                Description = request.Band.Description
            },
            Message = "Request successful! Your changes have been saved.",
        };
    }
}
