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

public class CreateRegionRequestHandler : IRequestHandler<CreateRegionRequest, ApiResponse<RegionDto>>
{
    private readonly IRegionRepository _repository;
    public CreateRegionRequestHandler(IRegionRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<RegionDto>> Handle(CreateRegionRequest request, CancellationToken cancellationToken)
    {
        var existingRegionQuery = _repository.Query(r => r.Name == request.Region.Name);
        var existingRegion = await _repository.FirstOrDefaultAsync(existingRegionQuery, cancellationToken);

        if (existingRegion is not null)
        {
            return new ApiResponse<RegionDto>
            {
                Message = "Region already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Name", ["A region with the specified name already exists."]
                    }
                }
            };
        }

        var newRegion = new Domain.Entities.Region
        {
            Name = request.Region.Name.Trim(),
            Description = request.Region.Description,
            Code = request.Region.Code ?? string.Empty,
            Area = request.Region.Area ?? 0f,
        };

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

                newRegion.Boundaries = boundariesGeometry;
                newRegion.Center = newRegion.Boundaries.Centroid;
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
            newRegion.Boundaries = GeometryHelper.CreateMultiPolygon([]);
            newRegion.Center = GeometryHelper.CreatePoint(0, 0);
        }

        _repository.Add(newRegion);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RegionDto>
        {
            Data = new RegionDto
            {
                Id = newRegion.Id,
                Name = newRegion.Name,
                Description = newRegion.Description,
                Area = newRegion.Area,
                Code = newRegion.Code,
            },

            Message = "Region created successfully.",
            Succeeded = true
        };
    }
}
