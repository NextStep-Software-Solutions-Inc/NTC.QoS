using MediatR;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Handlers.Commands;

public class CreateLinkHandler(ILinkRepository linkRepository, ITechnologyBandRepository technologyBandRepository, IServiceProviderRepository serviceProviderRepository) : IRequestHandler<CreateLinkRequest, ApiResponse<LinkDto>>
{
    private readonly ILinkRepository _linkRepository = linkRepository;
    private readonly ITechnologyBandRepository _technologyBandRepository = technologyBandRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<LinkDto>> Handle(CreateLinkRequest request, CancellationToken cancellationToken)
    {
        var duplicateQuery = _linkRepository.Query(l =>
        l.TechnologyBandId == request.Link.TechnologyBandId &&
        l.ServiceProviderId == request.Link.ServiceProviderId &&
        l.Type == EnumHelper<LinkTypeEnum>.ParseUserInput(request.Link.Type) &&
        l.CenterFrequency == request.Link.CenterFrequency &&
        l.LowerFrequency == request.Link.LowerFrequency &&
        l.UpperFrequency == request.Link.UpperFrequency &&
        l.RxThreshold == request.Link.RxThreshold
        );

        var existingLink = await _linkRepository.FirstOrDefaultAsync(duplicateQuery, cancellationToken);
        if (existingLink is not null)
        {
            return new ApiResponse<LinkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", new[] { "A link with the same attributes already exists." } }
                },
                Message = "Duplicate link detected."
            };
        }

        // Validate TechnologyBand exists
        var technologyBandQuery = _technologyBandRepository.Query(tb => tb.Id == request.Link.TechnologyBandId);
        var technologyBand = await _technologyBandRepository.FirstOrDefaultAsync(technologyBandQuery, cancellationToken);

        if (technologyBand is null)
        {
            return new ApiResponse<LinkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"NullTechnologyBand",  ["Technology Band not found."] }
                },
                Message = "Technology Band not found.",
            };
        }

        // Validate ServiceProvider exists and is wireless
        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.Id == request.Link.ServiceProviderId);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);

        if (serviceProvider is null)
        {
            return new ApiResponse<LinkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"NullServiceProvider",  ["Service Provider not found."] }
                },
                Message = "Service Provider not found.",
            };
        }

        if (serviceProvider.Service?.Type != ServiceTypeEnum.Wireless)
        {
            return new ApiResponse<LinkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "InvalidServiceProvider", ["Service Provider must be of type Wireless for link creation."] }
                },
                Message = "Service Provider must be of type Wireless for link creation."
            };
        }

        var newLink = new Domain.Aggregates.Link
        {
            Type = EnumHelper<LinkTypeEnum>.ParseUserInput(request.Link.Type),
            CenterFrequency = request.Link.CenterFrequency,
            LowerFrequency = request.Link.LowerFrequency,
            UpperFrequency = request.Link.UpperFrequency,
            RxThreshold = request.Link.RxThreshold,
            TechnologyBandId = request.Link.TechnologyBandId,
            ServiceProviderId = request.Link.ServiceProviderId,
        };
        _linkRepository.Add(newLink);
        await _linkRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<LinkDto>
        {
            Succeeded = true,
            Message = "Link created successfully.",
            Data = new LinkDto
            {
                Id = newLink.Id,
                Type = newLink.Type.ToString(),
                ServiceProvider = new()
                {
                    Id = newLink.ServiceProviderId,
                    Provider = new()
                    {
                        Id = newLink.ServiceProvider.Provider.Id,
                        Name = newLink.ServiceProvider.Provider.Name,
                        Description = newLink.ServiceProvider.Provider.Description,
                        Color = newLink.ServiceProvider.Provider.Color,
                        LicenseNumber = newLink.ServiceProvider.Provider.LicenseNumber
                    },
                    Service = new()
                    {
                        Id = newLink.ServiceProvider.ServiceId,
                        Name = newLink.ServiceProvider.Service.Name,
                        Type = newLink.ServiceProvider.Service.Type.ToString(),
                    }
                },
                TechnologyBand = new()
                {
                    Id = newLink.TechnologyBand.Id,
                    Band = new()
                    {
                        Id = newLink.TechnologyBand.Band.Id,
                        Name = newLink.TechnologyBand.Band.Name,
                        Description = newLink.TechnologyBand.Band.Description
                    },
                    Technology = new()
                    {
                        Id = newLink.TechnologyBand.Technology.Id,
                        Standard = newLink.TechnologyBand.Technology.Standard,
                        Description = newLink.TechnologyBand.Technology.Description,
                        Generation = new()
                        {
                            Id = newLink.TechnologyBand.Technology.Generation.Id,
                            Name = newLink.TechnologyBand.Technology.Generation.Name
                        }
                    }
                },
                CenterFrequency = newLink.CenterFrequency,
                LowerFrequency = newLink.LowerFrequency,
                UpperFrequency = newLink.UpperFrequency,
                RxThreshold = newLink.RxThreshold
            }
        };
    }
}
