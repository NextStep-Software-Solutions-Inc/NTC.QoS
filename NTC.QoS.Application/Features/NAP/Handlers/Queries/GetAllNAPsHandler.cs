using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries
{
    public class GetAllNAPsHandler : IRequestHandler<GetAllNAPsRequest, PaginatedResponse<NAPDto>>
    {
        private readonly INAPRepository _NAPRepository;

        public GetAllNAPsHandler(INAPRepository NAPRepository)
        {
            _NAPRepository = NAPRepository;
        }

        public async Task<PaginatedResponse<NAPDto>> Handle(GetAllNAPsRequest request, CancellationToken cancellationToken)
        {
            var allNAPs = await _NAPRepository.ListAsync(n => !n.IsDeleted, cancellationToken);

            if (request.ServiceProviderId.HasValue)
            {
                allNAPs = allNAPs.Where(n => n.ServiceProviderId == request.ServiceProviderId.Value).ToList();
            }

            if (request.ZoneId.HasValue)
            {
                allNAPs = allNAPs.Where(n => n.ZoneId == request.ZoneId.Value).ToList();
            }

            if (request.FileId.HasValue)
            {
                allNAPs = allNAPs.Where(n => n.FileId == request.FileId.Value).ToList();
            }

            var NAPs = allNAPs.Select(n => new NAPDto
            {
                Id = n.Id,
                FileId = n.FileId,
                ServiceProvider = new ServiceProviderNAPDto
                {
                    Id = n.ServiceProviderId,
                    Service = new ServiceNAPDto
                    {
                        Id = n.ServiceProvider?.ServiceId ?? Guid.Empty,
                        Name = n.ServiceProvider?.Service?.Name ?? "Unknown",
                        Type = n.ServiceProvider?.Service?.Type ?? ServiceTypeEnum.Unknown
                    },
                    Provider = new ProviderNAPDto
                    {
                        Id = n.ServiceProvider?.ProviderId ?? Guid.Empty,
                        Name = n.ServiceProvider?.Provider?.Name ?? "Unknown",
                    }
                },
                Zone = new ZoneNAPDto
                {
                    Id = n.ZoneId,
                    Name = n.Zone?.Name ?? "Unknown",
                },
                CoorLatitude = n.CoorLatitude,
                CoorLongitude = n.CoorLongitude,
            }).ToList();

            return new PaginatedResponse<NAPDto>(NAPs, request.PageNumber, request.PageSize);
        }
    }
}
