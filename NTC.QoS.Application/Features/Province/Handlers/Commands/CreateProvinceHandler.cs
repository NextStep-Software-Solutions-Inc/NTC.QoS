using System.Text.Json;
using MediatR;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Province.Handlers.Commands;

public class CreateProvinceRequestHandler : IRequestHandler<CreateProvinceRequest, ApiResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository;
    private readonly IRegionRepository _regionRepository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CreateProvinceRequestHandler(IProvinceRepository provinceRepository, IRegionRepository regionRepository)
    {
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
    }

    public async Task<ApiResponse<ProvinceDto>> Handle(CreateProvinceRequest request, CancellationToken cancellationToken)
    {
        var existingProvinceQuery = _provinceRepository.Query(
            p => p.Name == request.Province.Name && !p.IsDeleted);
        var existingProvince = await _provinceRepository.FirstOrDefaultAsync(existingProvinceQuery, cancellationToken);

        if (existingProvince is not null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Message = "Province already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Name", ["A province with the specified name already exists."] 
                    }
                }
            };
        }

        var existsQuery = _provinceRepository.Query(p => p.Name == request.Province.Name && p.RegionId == request.Province.RegionId && !p.IsDeleted);
        var exists = await _provinceRepository.FirstOrDefaultAsync(existsQuery, cancellationToken);

        if (exists is not null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Name", new[] { "A province with this name already exists in the selected region." }
                    }
                },
                Message = "Province name already exists in this region. Please choose a different name."
            };
        }

        var newProvince = new Domain.Entities.Province
        {
            RegionId = request.Province.RegionId,
            Name = request.Province.Name,
            Description = request.Province.Description,
            Code = request.Province.Code ?? string.Empty,
            Area = request.Province.Area ?? 0f
        };

        MultiPolygon? boundariesGeometry = null;

        if(request.Province.Boundaries.HasValue && request.Province.Boundaries.Value.ValueKind != JsonValueKind.Null)
        {
            try
            {
                Geometry? receivedGeometry = request.Province.Boundaries.Value.Deserialize<Geometry>(_jsonSerializerOptions);

                if (receivedGeometry == null)
                {
                    throw new ArgumentException("Provided boundaries GeoJSON is invalid or empty.");
                }

                if (receivedGeometry is MultiPolygon multiPolygon)
                {
                    boundariesGeometry = multiPolygon;
                }
                else if (receivedGeometry is Polygon polygon)
                {
                    // If it's a single Polygon, use GeometryHelper to wrap it in a MultiPolygon.
                    boundariesGeometry = GeometryHelper.CreateMultiPolygon((IEnumerable<Polygon>)polygon);
                }
                else
                {
                    return new ApiResponse<ProvinceDto>
                    {
                        Succeeded = false,
                        Errors = new Dictionary<string, string[]>
                        {
                            { "InvalidBoundariesType", new[] { $"Boundaries must be a Polygon or MultiPolygon GeoJSON. Received: {receivedGeometry.GeometryType}." } }
                        },
                        Message = "Invalid boundaries provided."
                    };
                }

                newProvince.Boundaries = boundariesGeometry;
                newProvince.Center = newProvince.Boundaries.Centroid;
            }
            catch (JsonException ex)
            {
                return new ApiResponse<ProvinceDto>
                {
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "InvalidGeoJSON", new[] { $"Failed to parse boundaries GeoJSON: {ex.Message}" } }
                    },
                    Message = "Invalid boundaries GeoJSON format."
                };
            }
            catch (ArgumentException ex)
            {
                return new ApiResponse<ProvinceDto>
                {
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "BoundariesError", new[] { ex.Message } }
                    },
                    Message = "Error processing boundaries."
                };
            }
        }
        else
        {
            newProvince.Boundaries = GeometryHelper.CreateMultiPolygon(Array.Empty<Polygon>());
            newProvince.Center = GeometryHelper.CreatePoint(0, 0);
        }

        _provinceRepository.Add(newProvince);
        await _provinceRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ProvinceDto>
        {
            Data = new ProvinceDto
            {
                Id = newProvince.Id,
                Name = newProvince.Name,
                Description = newProvince.Description,
                Code = newProvince.Code,
                Area = newProvince.Area,
                Region = new RegionDto
                {
                    Id = newProvince.RegionId,
                    Name = newProvince.Region?.Name ?? string.Empty,
                    Description = newProvince.Region?.Description
                }
            },
            Message = "Province created successfully.",
            Succeeded = true
        };
    }
}
