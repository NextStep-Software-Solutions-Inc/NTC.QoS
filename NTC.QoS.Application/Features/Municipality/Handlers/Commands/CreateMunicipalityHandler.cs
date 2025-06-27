using MediatR;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using System.Text.Json;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class CreateMunicipalityHandler : IRequestHandler<CreateMunicipalityRequest, ApiResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _repository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CreateMunicipalityHandler(IMunicipalityRepository repository, IProvinceRepository provinceRepository)
    {
        _repository = repository;
        _provinceRepository = provinceRepository;
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
    }

    public async Task<ApiResponse<MunicipalityDto>> Handle(CreateMunicipalityRequest request, CancellationToken cancellationToken)
    {
        var municipalityQuery = _repository.Query(m => m.Name == request.Municipality.Name && m.ProvinceId == request.Municipality.ProvinceId && !m.IsDeleted);
        var municipality = await _repository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

        if (municipality is not null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Message = "Municipality already exists.",
                Succeeded = false, 
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["A municipality with the specified name already exists."]
                    }
                }
            };
        }

        var provinceQuery = _provinceRepository.Query(p => p.Id == request.Municipality.ProvinceId && !p.IsDeleted);
        var province = await _provinceRepository.FirstOrDefaultAsync(provinceQuery, cancellationToken);

        if (province is null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProvince",["Province not found."]
                    }
                },
                Message = "Province not found."
            };
        }

        var newMunicipality = new Domain.Entities.Municipality
        {
            ProvinceId = province.Id,
            Name = request.Municipality.Name,
            Description = request.Municipality.Description,
            IsCity = request.Municipality.IsCity,
            Code = request.Municipality.Code ?? string.Empty,
            Area = request.Municipality.Area ?? 0f,
        };

        MultiPolygon? boundariesGeometry = null;

        // Process Boundaries
        if (request.Municipality.Boundaries.HasValue && request.Municipality.Boundaries.Value.ValueKind != JsonValueKind.Null)
        {
            try
            {
                // Correctly deserialize JsonElement to NetTopologySuite.Geometries.Geometry
                // The GeoJsonConverterFactory will handle mapping GeoJSON types to NTS types.
                Geometry? receivedGeometry = request.Municipality.Boundaries.Value.Deserialize<Geometry>(_jsonSerializerOptions);

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
                    // If an unexpected geometry type is provided (e.g., Point, LineString)
                    return new ApiResponse<MunicipalityDto>
                    {
                        Succeeded = false,
                        Errors = new Dictionary<string, string[]>
                        {
                            { "InvalidBoundariesType", new[] { $"Boundaries must be a Polygon or MultiPolygon GeoJSON. Received: {receivedGeometry.GeometryType}." } }
                        },
                        Message = "Invalid boundaries provided."
                    };
                }

                newMunicipality.Boundaries = boundariesGeometry;
                newMunicipality.Center = newMunicipality.Boundaries.Centroid;
            }
            catch (JsonException ex)
            {
                return new ApiResponse<MunicipalityDto>
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
                return new ApiResponse<MunicipalityDto>
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
            newMunicipality.Boundaries = GeometryHelper.CreateMultiPolygon(Array.Empty<Polygon>());
            newMunicipality.Center = GeometryHelper.CreatePoint(0, 0);
        }

        _repository.Add(newMunicipality);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<MunicipalityDto>
        {
            Data = new MunicipalityDto
            {
                Id = newMunicipality.Id,
                Name = newMunicipality.Name,
                Description = newMunicipality.Description,
                IsCity = newMunicipality.IsCity,
                Code = newMunicipality.Code,
                Area = newMunicipality.Area,
                Province = new ProvinceDto
                {
                    Id = newMunicipality.ProvinceId,
                    Name = newMunicipality.Province?.Name ?? string.Empty,
                    Description = newMunicipality.Province?.Description,
                    Region = new RegionDto
                    {
                        Id = newMunicipality.Province?.RegionId ?? Guid.Empty,
                        Name = newMunicipality.Province?.Region?.Name ?? string.Empty,
                        Description = newMunicipality.Province?.Region?.Description
                    }
                }
            },
            Message = "Municipality created successfully.",
            Succeeded = true
        };
    }
}
