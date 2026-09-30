using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.User.Requests;

public record RevokeInvitationRequest(string InvitationId) : IRequest<ApiResponse<bool>>;
