using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class DeleteSpeedtestByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteSpeedtestByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


