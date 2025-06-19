using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NetTopologySuite.IO.Converters;
using System.Text.Json; 

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class CreateBarangayHandler : IRequestHandler<CreateBarangayRequest, ApiResponse<BarangayDto>>
{
    private readonly IBarangayRepository _repository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly JsonSerializerOptions _jsonSerializerOptions; 

    public CreateBarangayHandler(IBarangayRepository repository, IMunicipalityRepository municipalityRepository)
    {
        _repository = repository;
        _municipalityRepository = municipalityRepository;
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
    }

    public async Task<ApiResponse<BarangayDto>> Handle(CreateBarangayRequest request, CancellationToken cancellationToken)
    {
        var barangayQuery = _repository.Query(b => b.Name == request.Barangay.Name && b.MunicipalityId == request.Barangay.MunicipalityId && !b.IsDeleted);
        var barangay = await _repository.FirstOrDefaultAsync(barangayQuery, cancellationToken);

        if (barangay is not null)
        {
            return new ApiResponse<BarangayDto>
            {
                Message = "Barangay already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", new[] { "A barangay with the specified name already exists." } }
                }
            };
        }

        var municipalityQuery = _municipalityRepository.Query(m => m.Id == request.Barangay.MunicipalityId && !m.IsDeleted);
        var municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

        if (municipality is null)
        {
            return new ApiResponse<BarangayDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullMunicipality", new[] { "Municipality not found." } }
                },
                Message = "Municipality not found."
            };
        }

        var newBarangay = new Domain.Entities.Barangay
        {
            MunicipalityId = request.Barangay.MunicipalityId,
            Name = request.Barangay.Name,
            Description = request.Barangay.Description,
            Code = request.Barangay.Code ?? string.Empty,
            Area = request.Barangay.Area ?? 0f,
        };

        MultiPolygon? boundariesGeometry = null;

        // Process Boundaries
        if (request.Barangay.Boundaries.HasValue && request.Barangay.Boundaries.Value.ValueKind != JsonValueKind.Null)
        {
            try
            {
                // Correctly deserialize JsonElement to NetTopologySuite.Geometries.Geometry
                // The GeoJsonConverterFactory will handle mapping GeoJSON types to NTS types.
                Geometry? receivedGeometry = request.Barangay.Boundaries.Value.Deserialize<Geometry>(_jsonSerializerOptions);

                if (receivedGeometry == null)
                {
                    throw new ArgumentException("Provided boundaries GeoJSON is invalid or empty.");
                }

                // Ensure the geometry is a MultiPolygon or can be converted to one.
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
                    // If an unexpected geometry type is provided (e.g., Point, LineString)
                    return new ApiResponse<BarangayDto>
                    {
                        Succeeded = false,
                        Errors = new Dictionary<string, string[]>
                        {
                            { "InvalidBoundariesType", new[] { $"Boundaries must be a Polygon or MultiPolygon GeoJSON. Received: {receivedGeometry.GeometryType}." } }
                        },
                        Message = "Invalid boundaries provided."
                    };
                }

                newBarangay.Boundaries = boundariesGeometry;
                newBarangay.Center = newBarangay.Boundaries.Centroid;
            }
            catch (JsonException ex)
            {
                return new ApiResponse<BarangayDto>
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
                return new ApiResponse<BarangayDto>
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
            newBarangay.Boundaries = GeometryHelper.CreateMultiPolygon(Array.Empty<Polygon>());
            newBarangay.Center = GeometryHelper.CreatePoint(0, 0);
        }

        _repository.Add(newBarangay);
        await _repository.SaveChangesAsync(cancellationToken);

        var responseBarangayDto = new BarangayDto
        {
            Id = newBarangay.Id,
            Name = newBarangay.Name,
            Description = newBarangay.Description,
            Code = newBarangay.Code,
            Area = newBarangay.Area,
            Municipality = new MunicipalityDto
            {
                Id = newBarangay.MunicipalityId,
                Name = municipality?.Name ?? string.Empty,
                Description = municipality?.Description,
                IsCity = municipality?.IsCity ?? false,
                Province = new ProvinceDto
                {
                    Id = municipality?.ProvinceId ?? Guid.Empty,
                    Name = municipality?.Province?.Name ?? string.Empty,
                    Description = municipality?.Province?.Description,
                    Region = new RegionDto
                    {
                        Id = municipality?.Province?.RegionId ?? Guid.Empty,
                        Name = municipality?.Province?.Region?.Name ?? string.Empty,
                        Description = municipality?.Province?.Region?.Description
                    }
                },

            },
        };

        return new ApiResponse<BarangayDto>
        {
            Data = responseBarangayDto,
            Message = "Barangay created successfully.",
            Succeeded = true
        };
    }
}