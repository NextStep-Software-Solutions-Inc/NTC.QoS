using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.GenerationBand.Requests;

public class DeleteGenerationBandByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteGenerationBandByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


