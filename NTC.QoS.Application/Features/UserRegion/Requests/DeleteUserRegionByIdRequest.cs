using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.UserRegion.Requests;

public record DeleteUserRegionByIdRequest(Guid Id) : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; } = Id;
}
