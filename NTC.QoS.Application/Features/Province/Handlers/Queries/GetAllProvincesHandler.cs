using MediatR;
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
        var provincesQuery = _provinceRepository.QueryAsNoTracking();

        if (request.RegionId.HasValue)
        {
            provincesQuery = provincesQuery.Where(p => p.RegionId == request.RegionId.Value);
        }

        provincesQuery = provincesQuery.OrderBy(p => p.Name);

        var paginatedProvincesQuery = provincesQuery
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
            });

        var paginatedResult = await _provinceRepository.PaginateAsync(paginatedProvincesQuery, request.PageNumber, request.PageSize, cancellationToken);

        // Calculate ISP counts - simple per-province approach that always works
        var ispCountsByProvince = new Dictionary<Guid, int>();
        
        var provincesWithBoundaries = paginatedResult.Data?
            .Where(p => p.Boundaries != null)
            .ToList();

        if (provincesWithBoundaries != null && provincesWithBoundaries.Count != 0)
        {
            // Process each province individually
            foreach (var province in provincesWithBoundaries)
            {
                var boundary = province.Boundaries;
                if (boundary == null)
                    continue;

                // Get unique service provider IDs from cells for this province
                var cellProviderQuery = _cellRepository.QueryAsNoTracking()
                    .Where(cell => boundary.IsWithinDistance(cell.Location, 0))
                    .Select(cell => cell.ServiceProviderId)
                    .Distinct();

                var cellProviderIds = await _cellRepository.ToListAsync(cellProviderQuery, cancellationToken);

                // Get unique service provider IDs from NAPs for this province
                var napProviderQuery = _nAPRepository.QueryAsNoTracking()
                    .Where(nap => boundary.IsWithinDistance(nap.Location, 0))
                    .Select(nap => nap.ServiceProviderId)
                    .Distinct();

                var napProviderIds = await _nAPRepository.ToListAsync(napProviderQuery, cancellationToken);

                // Combine and count unique service providers
                var allProviderIds = new HashSet<Guid>(cellProviderIds);
                allProviderIds.UnionWith(napProviderIds);
                
                ispCountsByProvince[province.Id] = allProviderIds.Count;
            }
        }

        var provinceDtos = paginatedResult.Data?.Select(p => new ProvinceDto
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
        }).ToList() ?? new List<ProvinceDto>();

        return new PaginatedResponse<ProvinceDto>(provinceDtos, paginatedResult.PageNumber, paginatedResult.PageSize, paginatedResult.TotalRows);
    }
}