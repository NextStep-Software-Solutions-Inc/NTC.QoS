using MediatR;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Handlers.Commands;

public class CreateLinkHandler(IRepository<Domain.Aggregates.Link> linkRepository, IBandRepository bandRepository, IServiceProviderRepository serviceProviderRepository) : IRequestHandler<CreateLinkRequest, ApiResponse<LinkDto>>
{
    private readonly IRepository<Domain.Aggregates.Link> _linkRepository = linkRepository;
    private readonly IBandRepository _bandRepository = bandRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<LinkDto>> Handle(CreateLinkRequest request, CancellationToken cancellationToken)
    {
        var newLink = new Domain.Aggregates.Link
        {
            Type = EnumHelper<LinkTypeEnum>.ParseUserInput(request.Link.Type),
            CenterFrequency = request.Link.CenterFrequency,
            LowerFrequency = request.Link.LowerFrequency,
            UpperFrequency = request.Link.UpperFrequency,
            RxThreshold = request.Link.RxThreshold,
            BandId = request.Link.BandId,
            ServiceProviderId = request.Link.ServiceProviderId,
        };
        _linkRepository.Add(newLink);
        await _linkRepository.SaveChangesAsync(cancellationToken);

        var bandQuery = _bandRepository.Query(b => b.Id == request.Link.BandId);
        var band = await _bandRepository.FirstOrDefaultAsync(bandQuery, cancellationToken);

        if (band is null)
        {
            return new ApiResponse<LinkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error",  ["Band not found."] }
                },
                Message = "Band not found.",
            };
        }

        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.Id == request.Link.ServiceProviderId);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);

        if (serviceProvider is null)
        {
            return new ApiResponse<LinkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error",  ["Service Provider not found."] }
                },
                Message = "Service Provider not found.",
            };
        }

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
                Band = new()
                {
                    Id = newLink.Band.Id,
                    Name = newLink.Band.Name,
                    Description = newLink.Band.Description,
                },
                CenterFrequency = newLink.CenterFrequency,
                LowerFrequency = newLink.LowerFrequency,
                UpperFrequency = newLink.UpperFrequency,
                RxThreshold = newLink.RxThreshold
            }
        };
    }
}
