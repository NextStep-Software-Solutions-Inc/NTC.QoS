using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.AntennaPattern.Requests;

public class DeleteAntennaPatternByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteAntennaPatternByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


