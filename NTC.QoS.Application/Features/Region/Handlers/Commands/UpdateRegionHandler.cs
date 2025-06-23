using MediatR;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Commands;

public class UpdateRegionHandler : IRequestHandler<UpdateRegionRequest, ApiResponse<RegionDto>>
{
    private readonly IRegionRepository _regionRepository;

    public UpdateRegionHandler(IRegionRepository regionRepository)
    {
        _regionRepository = regionRepository;
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

        MultiPolygon? boundariesGeometry = null;

        if (request.Region.BoundariesFile != null && request.Region.BoundariesFile.Length > 0)
        {
            try
            {
                using var streamReader = new StreamReader(request.Region.BoundariesFile.OpenReadStream());
                string geoJsonString = await streamReader.ReadToEndAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(geoJsonString))
                {
                    throw new ArgumentException("Provided boundaries GeoJSON file is empty or contains only whitespace.");
                }

                var geoJsonReader = new GeoJsonReader();
                Geometry? receivedGeometry = null;

                try
                {
                    var featureCollection = geoJsonReader.Read<FeatureCollection>(geoJsonString);
                    receivedGeometry = featureCollection?
                        .FirstOrDefault(f => f.Geometry is Polygon || f.Geometry is MultiPolygon)?
                        .Geometry;
                }
                catch (Newtonsoft.Json.JsonSerializationException)
                {
                    receivedGeometry = geoJsonReader.Read<Geometry>(geoJsonString);
                }

                if (receivedGeometry == null)
                {
                    throw new ArgumentException("Provided boundaries GeoJSON file content could not be parsed into a valid Polygon or MultiPolygon geometry.");
                }

                if (receivedGeometry is MultiPolygon multiPolygon)
                {
                    boundariesGeometry = multiPolygon;
                }
                else if (receivedGeometry is Polygon polygon)
                {
                    boundariesGeometry = GeometryHelper.CreateMultiPolygon([polygon]);
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

                region.Boundaries = boundariesGeometry;
                region.Center = region.Boundaries.Centroid;
            }
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                return new ApiResponse<RegionDto>
                {
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "InvalidGeoJSONFile", new[] { $"Failed to parse boundaries GeoJSON file: {ex.Message}" } }
                    },
                    Message = "Invalid boundaries GeoJSON file format. Check GeoJSON syntax."
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
                    Message = "Error processing boundaries from file."
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<RegionDto>
                {
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "ProcessingError", new[] { $"An unexpected error occurred during GeoJSON processing: {ex.Message}" } }
                    },
                    Message = "An unexpected error occurred while processing boundaries."
                };
            }
        }
        else
        {
            region.Boundaries = region.Boundaries;
            region.Center = region.Center;
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
