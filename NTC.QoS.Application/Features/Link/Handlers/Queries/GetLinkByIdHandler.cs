using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Models.DTOs.Link;
using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;

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
                Type = link.Type,
                ServiceProvider = new ServiceProviderDto
                {
                    Id = link.ServiceProviderId,
                    Provider = new ProviderDto
                    {
                        Name = link.ServiceProvider?.Provider?.Name ?? "Unknown"
                    },
                    Service = new ServiceDto
                    {
                        Name = link.ServiceProvider?.Service?.Name ?? "Unknown",
                        Type = link.ServiceProvider?.Service?.Type.ToString() ?? "Unknown"
                    }
                },
                Band = new BandDto
                {
                    Id = link.BandId,
                    Name = link.Band?.Name ?? "Unknown"
                },
                CenterFrequency = link.CenterFrequency,
                LowerFrequency = link.LowerFrequency,
                UpperFrequency = link.UpperFrequency
            }
        };
    }
}
