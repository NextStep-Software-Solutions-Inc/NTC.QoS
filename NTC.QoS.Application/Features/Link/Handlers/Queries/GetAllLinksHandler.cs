using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Models.DTOs.Link;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Technology.Handlers.Queries;

public class GetAllLinksHandler(ILinkRepository linkRepository) : IRequestHandler<GetAllLinksRequest, PaginatedResponse<LinkDto>>
{
    private readonly ILinkRepository _linkRepository = linkRepository;

    public async Task<PaginatedResponse<LinkDto>> Handle(GetAllLinksRequest request, CancellationToken cancellationToken)
    {
        var allLinks = await _linkRepository.ListAsync(l => !l.IsDeleted, cancellationToken);

        if (request.ServiceProviderId.HasValue)
        {
            allLinks = [.. allLinks.Where(l => l.ServiceProviderId == request.ServiceProviderId.Value)];
        }

        if (request.BandId.HasValue)
        {
            allLinks = [.. allLinks.Where(l => l.BandId == request.BandId.Value)];
        }

        var linkDtos = allLinks.Select(l =>
        {
            return new LinkDto
            {
                Id = l.Id,
                Type = EnumHelper<LinkTypeEnum>.GetDescription(l.Type),
                ServiceProvider = new ServiceProviderLinkDto
                {
                    Id = l.ServiceProviderId,
                    Provider = new ProviderLinkDto
                    {
                        Id = l.ServiceProvider?.ProviderId ?? Guid.Empty,
                        Name = l.ServiceProvider?.Provider?.Name ?? "Unknown",
                        Description = l.ServiceProvider?.Provider?.Description ?? string.Empty
                    },
                    Service = new ServiceLinkDto
                    {
                        Id = l.ServiceProvider?.ServiceId ?? Guid.Empty,
                        Name = l.ServiceProvider?.Service?.Name ?? "Unknown",
                        Type = l.ServiceProvider?.Service?.Type.ToString() ?? "Unknown"
                    }
                },
                Band = new BandLinkDto
                {
                    Id = l.BandId,
                    Name = l.Band?.Name ?? "Unknown",
                    Description = l.Band?.Description ?? string.Empty
                },
                CenterFrequency = l.CenterFrequency,
                LowerFrequency = l.LowerFrequency,
                UpperFrequency = l.UpperFrequency,
                RxThreshold = l.RxThreshold
            };
        }).ToList();

        return new PaginatedResponse<LinkDto>(linkDtos, request.PageNumber, request.PageSize);
    }
}
