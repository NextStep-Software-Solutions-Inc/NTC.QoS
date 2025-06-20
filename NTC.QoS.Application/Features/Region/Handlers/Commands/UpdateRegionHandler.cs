using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Application.Features.Region.Requests;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Helper;
using System.Text.Json;
using NetTopologySuite.IO.Converters;

namespace NTC.QoS.Application.Features.Region.Handlers.Commands;

public class UpdateRegionHandler : IRequestHandler<UpdateRegionRequest, ApiResponse<RegionDto>>
{
    private readonly IRegionRepository _regionRepository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public UpdateRegionHandler(IRegionRepository regionRepository)
    {
        _regionRepository = regionRepository;
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
    }

    public async Task<ApiResponse<RegionDto>> Handle(UpdateRegionRequest request, CancellationToken cancellationToken)
    {
        var regionQuery = _regionRepository.Query(r => r.Id == request.Id && !r.IsDeleted);
        var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);

        if (region is null)
        {
            return new ApiResponse<RegionDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", ["The region you are trying to update does not exist."]
                    }
                },
                Message = "Region not found."
            };
        }

        region.Name = request.Region.Name ?? region.Name;
        region.Description = request.Region.Description ?? region.Description;
        region.Code = request.Region.Code ?? region.Code;
        region.Area = request.Region.Area ?? region.Area;

        if (request.Region.Boundaries.HasValue)
        {
            if (request.Region.Boundaries.Value.ValueKind == JsonValueKind.Null)
            {
                region.Boundaries = GeometryHelper.CreateMultiPolygon(Array.Empty<Polygon>());
                region.Center = GeometryHelper.CreatePoint(0, 0);
            }
            else
            {
                try
                {
                    Geometry? receivedGeometry = request.Region.Boundaries.Value.Deserialize<Geometry>(_jsonSerializerOptions);

                    if (receivedGeometry == null)
                    {
                        return new ApiResponse<RegionDto>
                        {
                            Succeeded = false,
                            Errors = new Dictionary<string, string[]>
                    {
                        { "InvalidBoundaries", new[] { "Provided boundaries GeoJSON is invalid or empty after deserialization." } }
                    },
                            Message = "Invalid boundaries provided."
                        };
                    }

                    if (receivedGeometry is MultiPolygon multiPolygon)
                    {
                        region.Boundaries = multiPolygon;
                    }
                    else if (receivedGeometry is Polygon polygon)
                    {
                        region.Boundaries = GeometryHelper.CreateMultiPolygon(new[] { polygon });
                    }
                    else
                    {
                        return new ApiResponse<RegionDto>
                        {
                            Succeeded = false,
                            Errors = new Dictionary<string, string[]>
                    {
                        { "InvalidBoundariesType", new[] { $"Boundaries must be a Polygon or MultiPolygon GeoJSON. Received: {receivedGeometry.GeometryType}." } }
                    },
                            Message = "Invalid boundaries provided."
                        };
                    }

                    region.Center = region.Boundaries.Centroid;
                }
                catch (JsonException ex)
                {
                    return new ApiResponse<RegionDto>
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
                    return new ApiResponse<RegionDto>
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
        }

        var existingRegionQuery = _regionRepository.Query(
            r => r.Name == region.Name
                 && r.Id != request.Id
                 && !r.IsDeleted);
        var existingRegion = await _regionRepository.FirstOrDefaultAsync(existingRegionQuery, cancellationToken);

        if (existingRegion is not null)
        {
            return new ApiResponse<RegionDto>
            {
                Succeeded = false,
                Message = "A region with the same name already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Region with the same name already exists."]
                    }
                }
            };
        }

        _regionRepository.Update(region);
        await _regionRepository.SaveChangesAsync(cancellationToken);

        var result = new RegionDto
        {
            Id = region.Id,
            Name = region.Name,
            Description = region.Description,
            Code = region.Code,
            Area = region.Area,
        };

        return new ApiResponse<RegionDto>
        {
            Succeeded = true,
            Message = "Region updated successfully.",
            Data = result
        };
    }
}
