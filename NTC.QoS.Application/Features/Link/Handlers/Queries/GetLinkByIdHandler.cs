using MediatR;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Handlers.Queries;

public class GetLinkByIdHandler(ILinkRepository linkRepository) : IRequestHandler<GetLinkByIdRequest, ApiResponse<LinkDto>>
{
    private readonly ILinkRepository _linkRepository = linkRepository;

    public async Task<ApiResponse<LinkDto>> Handle(GetLinkByIdRequest request, CancellationToken cancellationToken)
    {
        var linkDtoQuery = _linkRepository.Query(l => l.Id == request.Id)
            .Select(link => new LinkDto
            {
                Id = link.Id,
                Type = link.Type.ToString(),
                ServiceProvider = new()
                {
                    Id = link.ServiceProviderId,
                    Provider = new()
                    {
                        Id = link.ServiceProvider.Provider.Id,
                        Name = link.ServiceProvider.Provider.Name,
                        Description = link.ServiceProvider.Provider.Description,
                        Color = link.ServiceProvider.Provider.Color,
                        LicenseNumber = link.ServiceProvider.Provider.LicenseNumber
                    },
                    Service = new()
                    {
                        Id = link.ServiceProvider.ServiceId,
                        Name = link.ServiceProvider.Service.Name,
                        Type = link.ServiceProvider.Service.Type.ToString(),
                    }
                },
                Band = new()
                {
                    Id = link.Band.Id,
                    Name = link.Band.Name,
                    Description = link.Band.Description
                },
                CenterFrequency = link.CenterFrequency,
                LowerFrequency = link.LowerFrequency,
                UpperFrequency = link.UpperFrequency,
                RxThreshold = link.RxThreshold,
            });

        var link = await _linkRepository.FirstOrDefaultAsync(linkDtoQuery, cancellationToken);

        if (link is null)
        {
            return new ApiResponse<LinkDto>("Link not found");
        }
        return new ApiResponse<LinkDto>
        {
            Succeeded = true,
            Message = "Link retrieved successfully",
            Data = link
        };
    }
}
