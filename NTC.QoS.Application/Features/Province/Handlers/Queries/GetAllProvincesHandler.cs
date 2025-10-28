using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Province.Handlers.Queries;

public class GetAllProvincesHandler(IProvinceRepository provinceRepository, ICellRepository cellRepository, INAPRepository nAPRepository) : IRequestHandler<GetAllProvincesRequest, PaginatedResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly INAPRepository _nAPRepository = nAPRepository;

    public async Task<PaginatedResponse<ProvinceDto>> Handle(GetAllProvincesRequest request, CancellationToken cancellationToken)
    {
        var provincesQuery = _provinceRepository.Query();

        if (request.RegionId.HasValue)
        {
            provincesQuery = provincesQuery.Where(p => p.RegionId == request.RegionId.Value);
        }

        // Get total count before pagination
        var totalCount = await provincesQuery.CountAsync(cancellationToken);

        // Apply pagination early and use AsNoTracking
        var paginatedProvinces = await provincesQuery
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Code,
                p.Area,
                Region = new
                {
                    p.Region.Id,
                    p.Region.Name,
                    p.Region.Description
                },
                MunicipalityCount = p.Municipalities.Count(),
                BarangayCount = p.Municipalities.Sum(m => m.Barangays.Count),
                p.Boundaries
            })
            .ToListAsync(cancellationToken);

        // Calculate ISP counts - simple per-province approach that always works
        var ispCountsByProvince = new Dictionary<Guid, int>();
        
        var provincesWithBoundaries = paginatedProvinces
            .Where(p => p.Boundaries != null)
            .ToList();
        
        if (provincesWithBoundaries.Count != 0)
        {
            // Process each province individually
            foreach (var province in provincesWithBoundaries)
            {
                var boundary = province.Boundaries!;
                
                // Get unique service provider IDs from cells for this province
                var cellProviderIds = await _cellRepository.Query()
                    .AsNoTracking()
                    .Where(cell => boundary.IsWithinDistance(cell.Location, 0))
                    .Select(cell => cell.ServiceProviderId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Get unique service provider IDs from NAPs for this province
                var napProviderIds = await _nAPRepository.Query()
                    .AsNoTracking()
                    .Where(nap => boundary.IsWithinDistance(nap.Location, 0))
                    .Select(nap => nap.ServiceProviderId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Combine and count unique service providers
                var allProviderIds = new HashSet<Guid>(cellProviderIds);
                allProviderIds.UnionWith(napProviderIds);
                
                ispCountsByProvince[province.Id] = allProviderIds.Count;
            }
        }

        // Map to DTOs
        var provinceDtos = paginatedProvinces.Select(p => new ProvinceDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Code = p.Code,
            Area = p.Area,
            Region = new()
            {
                Id = p.Region.Id,
                Name = p.Region.Name,
                Description = p.Region.Description,
            },
            MunicipalityCount = p.MunicipalityCount,
            BarangayCount = p.BarangayCount,
            ISPCount = ispCountsByProvince.GetValueOrDefault(p.Id, 0)
        }).ToList();

        return new PaginatedResponse<ProvinceDto>(provinceDtos, request.PageNumber, request.PageSize, totalCount);
    }
}