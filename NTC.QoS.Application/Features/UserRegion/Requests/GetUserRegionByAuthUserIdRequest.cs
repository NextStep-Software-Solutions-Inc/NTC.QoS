using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.Application.Features.UserRegion.Requests;

public class GetUserRegionByAuthUserIdRequest : IRequest<ApiResponse<UserRegionDto>>
{
    public string AuthUserId { get; set; }

    public GetUserRegionByAuthUserIdRequest(string authUserId)
    {
        AuthUserId = authUserId;
    }
}
