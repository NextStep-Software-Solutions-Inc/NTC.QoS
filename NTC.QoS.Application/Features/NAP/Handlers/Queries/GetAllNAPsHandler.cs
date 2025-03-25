using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetAllNAPsHandler(INAPRepository NAPRepository) : IRequestHandler<GetAllNAPsRequest, PaginatedResponse<NAPDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<PaginatedResponse<NAPDto>> Handle(GetAllNAPsRequest request, CancellationToken cancellationToken)
    {
        var allNAPs = await _NAPRepository.ListAsync(n => !n.IsDeleted, cancellationToken);

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
            Zone = new ZoneDto
            {
                Id = n.ZoneId ?? Guid.Empty,
                Name = n.Zone?.Name ?? string.Empty,
                Barangay = new BarangayDto
                {
                    Id = n.Zone?.BarangayId ?? Guid.Empty,
                    Name = n.Zone?.Barangay?.Name ?? string.Empty,
                    Description = n.Zone?.Barangay?.Description,
                    Municipality = new MunicipalityDto
                    {
                        Id = n.Zone?.Barangay?.MunicipalityId ?? Guid.Empty,
                        Name = n.Zone?.Barangay?.Municipality?.Name ?? string.Empty,
                        Description = n.Zone?.Barangay?.Description,
                        IsCity = n.Zone?.Barangay?.Municipality?.IsCity ?? false,
                        Province = new ProvinceDto
                        {
                            Id = n.Zone?.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                            Name = n.Zone?.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                            Description = n.Zone?.Barangay?.Municipality?.Province?.Description,
                            Region = new RegionDto
                            {
                                Id = n.Zone?.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                                Name = n.Zone?.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                Description = n.Zone?.Barangay?.Municipality?.Province?.Region?.Description
                            }
                        }
                    }
                }
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

        NAPs = [.. NAPs
            .Where(n => !request.ServiceProviderId.HasValue || n.ServiceProvider?.Id == request.ServiceProviderId)
            .Where(n => !request.ZoneId.HasValue || n.Zone?.Id == request.ZoneId)
            .Where(n => !request.FileId.HasValue || n.File?.Id == request.FileId)];

        if (NAPs.Count is 0)
        {
            return new PaginatedResponse<NAPDto>(NAPs, request.PageNumber, request.PageSize)
            {
                Data = NAPs,
                Message = "No records found.",
                Succeeded = true
            };
        }

        return new PaginatedResponse<NAPDto>(NAPs, request.PageNumber, request.PageSize)
        {

            Data = NAPs,
            Message = "NAPs retrieved successfully.",
            Succeeded = true
        };
    }
}
