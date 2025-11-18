using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.File.Requests;

public sealed class DownloadWirelessValidationTemplateRequest : IRequest<ApiResponse<Stream>>
{
    public DownloadWirelessValidationTemplateRequest(Guid municipalityId)
    {
        MunicipalityId = municipalityId;
    }

    public Guid MunicipalityId { get; set; }
}