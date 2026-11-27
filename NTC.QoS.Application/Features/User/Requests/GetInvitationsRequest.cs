using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public record GetInvitationsRequest(string? Status = null, int? Limit = null, int? Offset = null) : IRequest<ApiResponse<List<InvitationDto>>>;
