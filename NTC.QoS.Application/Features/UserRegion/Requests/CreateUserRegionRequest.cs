using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.Application.Features.UserRegion.Requests;

public record CreateUserRegionRequest: IRequest<ApiResponse<UserRegionDto>>
{
    public required CreateUserRegionDto UserRegion { get; set; }
}
