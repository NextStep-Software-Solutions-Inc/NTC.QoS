using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.Application.Features.UserRegion.Requests;

public record UpdateUserRegionByIdRequest(Guid Id, UpdateUserRegionDto UserRegion) : IRequest<ApiResponse<UserRegionDto>>
{
    public Guid Id { get; set; } = Id;
    public UpdateUserRegionDto UserRegion { get; set; } = UserRegion;
}
