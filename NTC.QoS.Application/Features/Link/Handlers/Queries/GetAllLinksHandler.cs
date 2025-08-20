using MediatR;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Handlers.Queries;

public class GetAllLinksHandler(ILinkRepository linkRepository) : IRequestHandler<GetAllLinksRequest, PaginatedResponse<LinkDto>>
{
    private readonly ILinkRepository _linkRepository = linkRepository;

    public async Task<PaginatedResponse<LinkDto>> Handle(GetAllLinksRequest request, CancellationToken cancellationToken)
    {
        var allLinksQuery = _linkRepository.Query();

        if (request.ServiceProviderIds != null && request.ServiceProviderIds.Count > 0)
        {
            allLinksQuery = allLinksQuery.Where(l => request.ServiceProviderIds.Contains(l.ServiceProviderId));
        }

        if (request.BandIds != null && request.BandIds.Count > 0)
        {
            allLinksQuery = allLinksQuery.Where(l => request.BandIds.Contains(l.BandId));
        }

        var linkDtosQuery = allLinksQuery
            .OrderBy(l => l.CreatedAt)
            .Select(l => new LinkDto
            {
                Id = l.Id,
                Type = l.Type.ToString(),
                ServiceProvider = new()
                {
                    Id = l.ServiceProvider.Id,
                    MCC = l.ServiceProvider.MCC,
                    MNC = l.ServiceProvider.MNC,
                    Provider = new()
                    {
                        Id = l.ServiceProvider.Provider.Id,
                        Name = l.ServiceProvider.Provider.Name,
                        Description = l.ServiceProvider.Provider.Description,
                        Color = l.ServiceProvider.Provider.Color,
                        LicenseNumber = l.ServiceProvider.Provider.LicenseNumber
                    },
                    Service = new()
                    {
                        Id = l.ServiceProvider.ServiceId,
                        Name = l.ServiceProvider.Service.Name,
                        Type = l.ServiceProvider.Service.Type.ToString(),
                    }
                },
                Band = new()
                {
                    Id = l.Band.Id,
                    Name = l.Band.Name,
                    Description = l.Band.Description
                },
                CenterFrequency = l.CenterFrequency,
                LowerFrequency = l.LowerFrequency,
                UpperFrequency = l.UpperFrequency,
                RxThreshold = l.RxThreshold
            });

        return await _linkRepository.PaginateAsync(linkDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
