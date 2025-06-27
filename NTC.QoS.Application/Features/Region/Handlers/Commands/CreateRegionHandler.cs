using System.Text.Json;
using MediatR;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Commands;

public class CreateRegionRequestHandler : IRequestHandler<CreateRegionRequest, ApiResponse<RegionDto>>
{
    private readonly IRegionRepository _repository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    public CreateRegionRequestHandler(IRegionRepository repository)
    {
        _repository = repository;
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
    }

    public async Task<ApiResponse<RegionDto>> Handle(CreateRegionRequest request, CancellationToken cancellationToken)
    {
        var existingRegionQuery = _repository.Query(p => p.Name == request.Region.Name && !p.IsDeleted);
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

        if (request.Region.Boundaries.HasValue && request.Region.Boundaries.Value.ValueKind != JsonValueKind.Null)
        {
            try
            {
                Geometry? receivedGeometry = request.Region.Boundaries.Value.Deserialize<Geometry>(_jsonSerializerOptions);

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
                    boundariesGeometry = GeometryHelper.CreateMultiPolygon((IEnumerable<Polygon>)polygon);
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
        else
        {
            newRegion.Boundaries = GeometryHelper.CreateMultiPolygon(Array.Empty<Polygon>());
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