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

public class UpdateProvinceByIdHandler : IRequestHandler<UpdateProvinceRequest, ApiResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _repository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public UpdateProvinceByIdHandler(IProvinceRepository repository)
    {
        _repository = repository;
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
    }
    public async Task<ApiResponse<ProvinceDto>> Handle(UpdateProvinceRequest request,
            CancellationToken cancellationToken)
    {
        var provinceQuery = _repository.Query(p => p.Id == request.Id && !p.IsDeleted);
        var province = await _repository.FirstOrDefaultAsync(provinceQuery, cancellationToken);

        if (province is null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProvince", new[] { "Province not found." }
                    }
                },
                Message = "The province you are trying to edit does not exist."
            };
        }

        var existsQuery = _repository.Query(p => p.Name == request.Province.Name && p.RegionId == province.RegionId && p.Id != request.Id && !p.IsDeleted);
        var exists = await _repository.FirstOrDefaultAsync(existsQuery, cancellationToken);

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

        province.Name = request.Province.Name ?? province.Name;
        province.Description = request.Province.Description ?? province.Description;
        province.Code = request.Province.Code ?? province.Code;
        province.Area = request.Province.Area ?? province.Area;

        if (request.Province.Boundaries.HasValue)
        {
            if (request.Province.Boundaries.Value.ValueKind == JsonValueKind.Null)
            {
                province.Boundaries = GeometryHelper.CreateMultiPolygon(Array.Empty<Polygon>());
                province.Center = GeometryHelper.CreatePoint(0, 0);
            }
            else
            {
                try
                {
                    Geometry? receivedGeometry = request.Province.Boundaries.Value.Deserialize<Geometry>(_jsonSerializerOptions);

                    if (receivedGeometry == null)
                    {
                        return new ApiResponse<ProvinceDto>
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
                        province.Boundaries = multiPolygon;
                    }
                    else if (receivedGeometry is Polygon polygon)
                    {
                        province.Boundaries = GeometryHelper.CreateMultiPolygon(new[] { polygon });
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

                    province.Center = province.Boundaries.Centroid;
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
        }

        _repository.Update(province);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ProvinceDto>
        {
            Succeeded = true,
            Data = new ProvinceDto
            {
                Id = province.Id,
                Name = province.Name,
                Description = province.Description,
                Region = new RegionDto
                {
                    Id = province.RegionId,
                    Name = province.Region?.Name ?? string.Empty,
                    Description = province.Region?.Description
                },
                Code = province.Code,
                Area = province.Area,
            },
            Message = "Province updated successfully."
        };
    }
}