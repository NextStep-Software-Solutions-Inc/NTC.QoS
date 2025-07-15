using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Queries;

public class GetAllBarangaysHandler(IBarangayRepository barangayRepository): IRequestHandler<GetAllBarangaysRequest, PaginatedResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository = barangayRepository;

    public async Task<PaginatedResponse<BarangayDto>> Handle(GetAllBarangaysRequest request, CancellationToken cancellationToken)
    {
        var allBarangaysQuery = _barangayRepository.Query();

        if (request.MunicipalityIds is not null && request.MunicipalityIds.Length > 0)
        {
            allBarangaysQuery = allBarangaysQuery.Where(b => request.MunicipalityIds.Contains(b.MunicipalityId));
        }

        allBarangaysQuery = allBarangaysQuery
            .Where(b => request.Name == null || b.Name.ToLower().Contains(request.Name.ToLower()))
            .Where(b => request.Municipality == null || b.Municipality.Name.ToLower().Contains(request.Municipality.ToLower()))
            .Where(b => request.Province == null || b.Municipality.Province.Name.ToLower().Contains(request.Province.ToLower()));

        var barangayDtosQuery = allBarangaysQuery
            .OrderBy(b => b.Name)
            .Select(b => new BarangayDto {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Municipality = new ()
                {
                    Id = b.MunicipalityId,
                    Name = b.Municipality.Name,
                    IsCity = b.Municipality.IsCity,
                    Description = b.Municipality.Description,
                    Province = new ()
                    {
                        Id = b.Municipality.ProvinceId,
                        Name = b.Municipality.Province.Name,
                        Description = b.Municipality.Province.Description,
                        Region = new ()
                        {
                            Id = b.Municipality.Province.Region.Id,
                            Name = b.Municipality.Province.Region.Name,
                            Description = b.Municipality.Province.Region.Description
                        }
                    }
                },
                Code = b.Code ?? string.Empty,
                Area = b.Area,
            });

        return await _barangayRepository.PaginateAsync(barangayDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
