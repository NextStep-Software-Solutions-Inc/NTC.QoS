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

        if (request.ServiceProviderId.HasValue)
        {
            allLinksQuery = allLinksQuery.Where(l => l.ServiceProviderId == request.ServiceProviderId.Value);
        }

        if (request.TechnologyBandId.HasValue)
        {
            allLinksQuery = allLinksQuery.Where(l => l.TechnologyBandId == request.TechnologyBandId.Value);
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
                TechnologyBand = new()
                {
                    Id = l.TechnologyBand.Id,
                    Band = new()
                    {
                        Id = l.TechnologyBand.Band.Id,
                        Name = l.TechnologyBand.Band.Name,
                        Description = l.TechnologyBand.Band.Description
                    },
                    Technology = new()
                    {
                        Id = l.TechnologyBand.Technology.Id,
                        Standard = l.TechnologyBand.Technology.Standard,
                        Generation = new()
                        {
                            Id = l.TechnologyBand.Technology.Generation.Id,
                            Name = l.TechnologyBand.Technology.Generation.Name
                        }
                    }
                },
                CenterFrequency = l.CenterFrequency,
                LowerFrequency = l.LowerFrequency,
                UpperFrequency = l.UpperFrequency,
                RxThreshold = l.RxThreshold
            });

        return await _linkRepository.PaginateAsync(linkDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
