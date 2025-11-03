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
        var provincesQuery = _provinceRepository.Query();

        if (request.RegionId.HasValue)
        {
            provincesQuery = provincesQuery.Where(p => p.RegionId == request.RegionId.Value);
        }

        // Get paginated provinces first (without ISP counts)
        var paginatedProvinces = await _provinceRepository.PaginateAsync(
            provincesQuery
                .OrderBy(p => p.Name)
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
                        p.Region.Description,
                    },
                    MunicipalityCount = p.Municipalities.Count(),
                    BarangayCount = p.Municipalities.Sum(m => m.Barangays.Count),
                    p.Boundaries
                }),
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        // Get province IDs for the current page
        var provinceIds = paginatedProvinces.Data?.Select(p => p.Id).ToList() ?? new List<Guid>();

        // Compute ISP counts efficiently using spatial joins
        var ispCounts = await GetIspCountsForProvincesAsync(provinceIds, cancellationToken);

        // Combine results
        var provinceDtos = new List<ProvinceDto>();
        if (paginatedProvinces.Data != null)
        {
            foreach (var p in paginatedProvinces.Data)
            {
                provinceDtos.Add(new ProvinceDto
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
                    ISPCount = ispCounts.GetValueOrDefault(p.Id, 0)
                });
            }
        }

        return new PaginatedResponse<ProvinceDto>(
            provinceDtos,
            paginatedProvinces.PageNumber,
            paginatedProvinces.PageSize,
            paginatedProvinces.TotalRows);
    }

    private async Task<Dictionary<Guid, int>> GetIspCountsForProvincesAsync(List<Guid> provinceIds, CancellationToken cancellationToken)
    {
        if (provinceIds.Count == 0)
            return [];

        // Get all cells with their service providers that intersect with any of the provinces
        var cellsWithProviders = await _cellRepository.ToListAsync(
            _cellRepository.Query()
                .Select(c => new { c.Location, c.ServiceProviderId }),
            cancellationToken);

        // Get all NAPs with their service providers
        var napsWithProviders = await _nAPRepository.ToListAsync(
            _nAPRepository.Query()
                .Select(n => new { n.Location, n.ServiceProviderId }),
            cancellationToken);

        // Get provinces with boundaries for the current page
        var provincesWithBoundaries = await _provinceRepository.ToListAsync(
            _provinceRepository.Query()
                .Where(p => provinceIds.Contains(p.Id) && p.Boundaries != null)
                .Select(p => new { p.Id, p.Boundaries }),
            cancellationToken);

        // Compute ISP counts using spatial operations
        var ispCounts = new Dictionary<Guid, int>();

        foreach (var province in provincesWithBoundaries)
        {
            var provinceCells = cellsWithProviders
                .Where(c => province.Boundaries!.Intersects(c.Location))
                .Select(c => c.ServiceProviderId);

            var provinceNaps = napsWithProviders
                .Where(n => province.Boundaries!.Intersects(n.Location))
                .Select(n => n.ServiceProviderId);

            var distinctProviders = provinceCells
                .Concat(provinceNaps)
                .Distinct()
                .Count();

            ispCounts[province.Id] = distinctProviders;
        }

        return ispCounts;
    }
}