using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Queries;

public class GetAllBarangaysHandler(IBarangayRepository barangayRepository) : IRequestHandler<GetAllBarangaysRequest, PaginatedResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository = barangayRepository;

    public async Task<PaginatedResponse<BarangayDto>> Handle(GetAllBarangaysRequest request, CancellationToken cancellationToken)
    {
        var allBarangays = await _barangayRepository.ListAsync(b => !b.IsDeleted, cancellationToken);

        if (request.MunicipalityId.HasValue)
        {
            allBarangays = allBarangays.Where(b => b.MunicipalityId == request.MunicipalityId.Value).ToList();
        }

        var barangayDtos = allBarangays.Select(b => new BarangayDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            Municipality = new MunicipalityDto
            {
                Id = b.MunicipalityId,
                Name = b.Municipality?.Name ?? "Unknown",
                IsCity = b.Municipality?.IsCity ?? false,
                Description = b.Municipality?.Description,
                Province = new ProvinceDto
                {
                    Id = b.Municipality?.ProvinceId ?? Guid.Empty,
                    Name = b.Municipality?.Province?.Name ?? "Unknown",
                    Description = b.Municipality?.Province?.Description,
                    Region = new RegionDto
                    {
                        Id = b.Municipality?.Province?.Region?.Id ?? Guid.Empty,
                        Name = b.Municipality?.Province?.Region?.Name ?? "Unknown",
                        Description = b.Municipality?.Province?.Region?.Description,
                    }
                }
            }
        }).ToList();

        return new PaginatedResponse<BarangayDto>(barangayDtos, request.PageNumber, request.PageSize);
    }
}
