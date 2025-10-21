using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Province.Handlers.Queries;

public class GetAllProvincesHandler(IProvinceRepository provinceRepository, ICellRepository cellRepository, INAPRepository napRepository) : IRequestHandler<GetAllProvincesRequest, PaginatedResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly INAPRepository _napRepository = napRepository;

    public async Task<PaginatedResponse<ProvinceDto>> Handle(GetAllProvincesRequest request, CancellationToken cancellationToken)
    {
        // Step 1: Get base query
        var provincesQuery = _provinceRepository.Query();

        if (request.RegionId.HasValue)
        {
            provincesQuery = provincesQuery.Where(p => p.RegionId == request.RegionId.Value);
        }

        // Step 2: Get total count
        var totalCount = await provincesQuery.CountAsync(cancellationToken);

        // Step 3: Get ONLY the paginated provinces
        var pagedProvinces = await provincesQuery
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
                p.Boundaries,
                Region = new
                {
                    p.Region.Id,
                    p.Region.Name,
                    p.Region.Description
                },
                Municipalities = p.Municipalities.Select(m => new
                {
                    BarangayCount = m.Barangays.Count
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        // Step 4: Pre-calculate ISP counts for this page ONLY
        var provinceIds = pagedProvinces.Select(p => p.Id).ToList();

        // Get all cells and NAPs intersecting with ANY of the provinces on this page (batch query)
        var provinceBoundaries = pagedProvinces
            .Where(p => p.Boundaries != null)
            .Select(p => new { p.Id, p.Boundaries })
            .ToList();

        var ispCounts = new Dictionary<Guid, int>();

        foreach (var province in provinceBoundaries)
        {
            try
            {
                // Add timeout protection
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var cellProviders = await _cellRepository.Query()
                    .Where(cell => province.Boundaries!.Intersects(cell.Location))
                    .Select(cell => cell.ServiceProviderId)
                    .Distinct()
                    .ToListAsync(linkedCts.Token);

                var napProviders = await _napRepository.Query()
                    .Where(nap => province.Boundaries!.Intersects(nap.Location))
                    .Select(nap => nap.ServiceProviderId)
                    .Distinct()
                    .ToListAsync(linkedCts.Token);

                ispCounts[province.Id] = cellProviders.Union(napProviders).Distinct().Count();
            }
            catch (OperationCanceledException)
            {
                // Timeout - skip this province or set to 0
                ispCounts[province.Id] = 0;
                // TODO: Log which province timed out
            }
        }

        // Step 5: Map to DTOs
        var provinceDtos = pagedProvinces.Select(p => new ProvinceDto
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
            MunicipalityCount = p.Municipalities.Count,
            BarangayCount = p.Municipalities.Sum(m => m.BarangayCount),
            ISPCount = ispCounts.GetValueOrDefault(p.Id, 0)
        }).ToList();

        // Step 6: Return paginated response
        return new PaginatedResponse<ProvinceDto>(
            provinceDtos,
            request.PageNumber,
            request.PageSize,
            totalCount
        );
    }
}