using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.Application.Features.UserRegion.Requests;

public record GetUserRegionByAuthUserIdRequest(string AuthUserId) : IRequest<ApiResponse<UserRegionDto>>
{
    public string AuthUserId { get; set; } = AuthUserId;
}
