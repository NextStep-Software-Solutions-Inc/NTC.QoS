using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class GetProviderByIdRequest(Guid id, bool? includeLogoData = null) : IRequest<ApiResponse<ProviderDto>>
{
    public Guid Id { get; set; } = id;
    public bool? IncludeLogoData { get; set; } = includeLogoData;
}
