using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.SignalLevel.Requests;

public class DeleteSignalLevelByIdRequest(Guid id) : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; } = id;
}