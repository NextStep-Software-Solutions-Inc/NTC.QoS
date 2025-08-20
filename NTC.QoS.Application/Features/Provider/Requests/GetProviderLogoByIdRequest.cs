using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class GetProviderLogoByIdRequest(Guid id) : IRequest<ApiResponse<ProviderLogoDto>>
{
    public Guid Id { get; set; } = id;
}