using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Models.DTOs.Link;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Link.Handlers.Queries;

public class GetLinkByIdHandler(ILinkRepository linkRepository) : IRequestHandler<GetLinkByIdRequest, ApiResponse<LinkDto>>
{
    private readonly ILinkRepository _linkRepository = linkRepository;

    public async Task<ApiResponse<LinkDto>> Handle(GetLinkByIdRequest request, CancellationToken cancellationToken)
    {
        var link = await _linkRepository.GetByIdAsync(request.Id, cancellationToken);

        if (link is null)
        {
            return new ApiResponse<LinkDto>
            {
                Succeeded = false,
                Message = "Link not found."
            };
        }

        return new ApiResponse<LinkDto>
        {
            Succeeded = true,
            Message = "Link retrieved successfully",
            Data = new LinkDto()
            {
                Id = link.Id,
                Type = EnumHelper<LinkTypeEnum>.GetDescription(link.Type),
                ServiceProvider = link.ServiceProvider is not null && link.ServiceProvider.Provider is not null && link.ServiceProvider.Service is not null ?
                    new ServiceProviderLinkDto
                    {
                        Id = link.ServiceProvider.Id,
                        ProviderId = link.ServiceProvider.Provider.Id,
                        ProviderName = link.ServiceProvider.Provider.Name,
                        ServiceId = link.ServiceProvider.Service.Id,
                        ServiceType = link.ServiceProvider.Service.Type.ToString()
                    } : null,
                Band = new BandLinkDto
                {
                    Id = link.BandId,
                    Name = link.Band?.Name ?? "Unknown Band"
                },
                CenterFrequency = link.CenterFrequency,
                LowerFrequency = link.LowerFrequency,
                UpperFrequency = link.UpperFrequency
            }
        };
    }
}
