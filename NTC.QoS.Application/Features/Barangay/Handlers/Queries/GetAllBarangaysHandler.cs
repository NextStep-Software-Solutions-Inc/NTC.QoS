using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;

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
            MunicipalityId = b.MunicipalityId,
            Municipality = b.Municipality != null ? new MunicipalityDto
            {
                Id = b.Municipality.Id,
                IsCity = b.Municipality.IsCity,
                ProvinceId = b.Municipality.ProvinceId,
                Name = b.Municipality.Name
            } : null
        }).ToList();

        return new PaginatedResponse<BarangayDto>(barangayDtos, request.PageNumber, request.PageSize);
    }
}
