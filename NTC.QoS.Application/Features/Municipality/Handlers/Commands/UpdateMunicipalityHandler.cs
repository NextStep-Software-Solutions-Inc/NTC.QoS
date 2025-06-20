using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Helper;
using System.Text.Json;
using NetTopologySuite.IO.Converters;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class UpdateMunicipalityByIdHandler : IRequestHandler<UpdateMunicipalityByIdRequest, ApiResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _repository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public UpdateMunicipalityByIdHandler(IMunicipalityRepository repository)
    {
        _repository = repository;
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
    }
    
    public async Task<ApiResponse<MunicipalityDto>> Handle(UpdateMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var municipalityQuery = _repository.Query(m => m.Id == request.Id && !m.IsDeleted);
        var municipality = await _repository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

        if (municipality is null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", ["Municipality not found."] }
                },
                Message = "The municipality you are trying to edit does not exist."
            };
        }
        municipality.Name = request.Municipality.Name ?? municipality.Name;
        municipality.Description = request.Municipality.Description ?? municipality.Description;
        municipality.Code = request.Municipality.Code ?? municipality.Code;
        municipality.IsCity = request.Municipality.IsCity ?? municipality.IsCity;
        municipality.Area = request.Municipality.Area ?? municipality.Area;

        // Process Boundaries
        if (request.Municipality.Boundaries.HasValue)
        {
            if (request.Municipality.Boundaries.Value.ValueKind == JsonValueKind.Null)
            {
                municipality.Boundaries = GeometryHelper.CreateMultiPolygon(Array.Empty<Polygon>());
                municipality.Center = GeometryHelper.CreatePoint(0, 0);
            }
            else
            {
                try
                {
                    Geometry? receivedGeometry = request.Municipality.Boundaries.Value.Deserialize<Geometry>(_jsonSerializerOptions);

                    if (receivedGeometry == null)
                    {
                        return new ApiResponse<MunicipalityDto>
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
                        municipality.Boundaries = multiPolygon;
                    }
                    else if (receivedGeometry is Polygon polygon)
                    {
                        municipality.Boundaries = GeometryHelper.CreateMultiPolygon(new[] { polygon });
                    }
                    else
                    {
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

                    municipality.Center = municipality.Boundaries.Centroid;
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
        }

        _repository.Update(municipality);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<MunicipalityDto>
        {
            Succeeded = true,
            Data = new MunicipalityDto
            {
                Id = municipality.Id,
                Name = municipality.Name,
                Description = municipality.Description,
                IsCity = municipality.IsCity,
                Code = municipality.Code ?? string.Empty,
                Area = municipality.Area,
                Province = new ProvinceDto
                {
                    Id = municipality.ProvinceId,
                    Name = municipality.Province?.Name ?? string.Empty,
                    Description = municipality.Province?.Description,
                    Region = new RegionDto
                    {
                        Id = municipality.Province?.RegionId ?? Guid.Empty,
                        Name = municipality.Province?.Region?.Name ?? string.Empty,
                        Description = municipality.Province?.Region?.Description
                    }
                }
            },
            Message = "Municipality updated successfully."
        };
    }
}