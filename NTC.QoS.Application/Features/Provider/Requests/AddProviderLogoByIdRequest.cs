using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class AddProviderLogoByIdRequest(Guid id, UploadProviderLogoDto logo) : IRequest<ApiResponse<ProviderDto>>
{
    public Guid Id { get; set; } = id;
    public UploadProviderLogoDto Logo { get; set; } = logo;
}
