using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.NAP;

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
                allNAPs = [.. allNAPs.Where(n => n.ServiceProviderId == request.ServiceProviderId.Value)];
            }

            if (request.ZoneId.HasValue)
            {
                allNAPs = [.. allNAPs.Where(n => n.ZoneId == request.ZoneId.Value)];
            }

            if (request.FileId.HasValue)
            {
                allNAPs = [.. allNAPs.Where(n => n.FileId == request.FileId.Value)];
            }

            var NAPs = allNAPs.Select(n => new NAPDto
            {
                Id = n.Id,
                ServiceProvider = new ServiceProviderNAPDto
                {
                    Id = n.ServiceProviderId,
                    Service = new ServiceNAPDto
                    {
                        Id = n.ServiceProvider?.ServiceId ?? Guid.Empty,
                        Name = n.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = n.ServiceProvider?.Service?.Type ?? ServiceTypeEnum.Unknown
                    },
                    Provider = new ProviderNAPDto
                    {
                        Id = n.ServiceProvider?.ProviderId ?? Guid.Empty,
                        Name = n.ServiceProvider?.Provider?.Name ?? string.Empty,
                    }
                },
                Zone = new ZoneNAPDto
                {
                    Id = n.ZoneId,
                    Name = n.Zone?.Name ?? string.Empty,
                },
                File = new FileDto
                {
                    Id = n.FileId ?? Guid.Empty,
                    Name = n.File?.Name ?? string.Empty,
                    Path = n.File?.Path ?? string.Empty,
                },
                CoorLatitude = n.CoorLatitude,
                CoorLongitude = n.CoorLongitude,
            }).ToList();

            return new PaginatedResponse<NAPDto>(NAPs, request.PageNumber, request.PageSize);
        }
    }
}
