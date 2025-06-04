using MediatR;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Handlers.Commands;

public class CreateLinkHandler(IRepository<Domain.Aggregates.Link> linkRepository, IBandRepository bandRepository, IServiceProviderRepository serviceProviderRepository) : IRequestHandler<CreateLinkRequest, ApiResponse<CreateLinkDto>>
{
    private readonly IRepository<Domain.Aggregates.Link> _linkRepository = linkRepository;
    private readonly IBandRepository _bandRepository = bandRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<CreateLinkDto>> Handle(CreateLinkRequest request, CancellationToken cancellationToken)
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

        var bandQuery = _bandRepository.Query(b => b.Id == request.Link.BandId && !b.IsDeleted);
        var band = await _bandRepository.FirstOrDefaultAsync(bandQuery, cancellationToken);

        if (band is null)
        {
            return new ApiResponse<CreateLinkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error",  ["Band not found."] }
                },
                Message = "Band not found.",
            };
        }

        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.Id == request.Link.ServiceProviderId && !sp.IsDeleted);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);

        if (serviceProvider is null)
        {
            return new ApiResponse<CreateLinkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error",  ["Service Provider not found."] }
                },
                Message = "Service Provider not found.",
            };
        }

        return new ApiResponse<CreateLinkDto>
        {
            Succeeded = true,
            Message = "Link created successfully.",
            Data = new CreateLinkDto
            {
                Type = EnumHelper<LinkTypeEnum>.GetDescription(newLink.Type),
                ServiceProviderId = serviceProvider.Id,
                BandId = band.Id,
                CenterFrequency = newLink.CenterFrequency,
                LowerFrequency = newLink.LowerFrequency,
                UpperFrequency = newLink.UpperFrequency,
                RxThreshold = newLink.RxThreshold
            }
        };
    }
}
