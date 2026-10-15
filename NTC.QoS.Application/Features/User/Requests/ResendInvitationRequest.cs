using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public record ResendInvitationRequest(string InvitationId) : IRequest<ApiResponse<UserDto>>;
