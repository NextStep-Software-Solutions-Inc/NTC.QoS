using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using System.Text.Json;
using NetTopologySuite.IO.Converters;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class UpdateBarangayByIdHandler : IRequestHandler<UpdateBarangayByIdRequest, ApiResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public UpdateBarangayByIdHandler(IBarangayRepository barangayRepository)
    {
        _barangayRepository = barangayRepository;
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
    }
    public async Task<ApiResponse<BarangayDto>> Handle(UpdateBarangayByIdRequest request, CancellationToken cancellationToken)
    {
        var barangayQuery = _barangayRepository.Query(b => b.Id == request.Id && !b.IsDeleted);
        var barangay = await _barangayRepository.FirstOrDefaultAsync(barangayQuery, cancellationToken);

        if (barangay is null)
        {
            return new ApiResponse<BarangayDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]> 
                { 
                    { 
                        "NullBarangay", ["Barangay not found."]
                    } 
                },
                Message = "The barangay you are trying to edit does not exist."
            };
        }

        // Process Boundaries
        if (request.Barangay.Boundaries.HasValue)
        {
            if (request.Barangay.Boundaries.Value.ValueKind == JsonValueKind.Null)
            {
                barangay.Boundaries = GeometryHelper.CreateMultiPolygon(Array.Empty<Polygon>());
                barangay.Center = GeometryHelper.CreatePoint(0, 0);
            }
            else
            {
                try
                {
                    Geometry? receivedGeometry = request.Barangay.Boundaries.Value.Deserialize<Geometry>(_jsonSerializerOptions);

                    if (receivedGeometry == null)
                    {
                        return new ApiResponse<BarangayDto>
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
                        barangay.Boundaries = multiPolygon;
                    }
                    else if (receivedGeometry is Polygon polygon)
                    {
                        barangay.Boundaries = GeometryHelper.CreateMultiPolygon(new[] { polygon });
                    }
                    else
                    {
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

                    barangay.Center = barangay.Boundaries.Centroid;
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
            }
        }

        barangay.Name = request.Barangay.Name ?? barangay.Name;
        barangay.Description = request.Barangay.Description ?? barangay.Description;
        barangay.Code = request.Barangay.Code ?? barangay.Code;
        barangay.Area = request.Barangay.Area ?? barangay.Area;

        _barangayRepository.Update(barangay);
        await _barangayRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<BarangayDto>
        {
            Succeeded = true,
            Message = "Barangay updated successfully.",
            Data = new BarangayDto
            {
                Id = barangay.Id,
                Name = barangay.Name,
                Description = barangay.Description,
                Code = barangay.Code,
                Area = barangay.Area,
                Municipality = new MunicipalityDto
                {
                    Id = barangay.MunicipalityId,
                    Name = barangay.Municipality?.Name ?? string.Empty,
                    IsCity = barangay.Municipality != null && barangay.Municipality.IsCity,
                    Province = new ProvinceDto
                    {
                        Id = barangay.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = barangay.Municipality?.Province?.Name ?? string.Empty,
                        Region = new RegionDto
                        {
                            Id = barangay.Municipality?.Province?.RegionId ?? Guid.Empty,
                            Name = barangay.Municipality?.Province?.Region?.Name ?? string.Empty,
                        }
                    }
                },
            },
        };
    }
}
