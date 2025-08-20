using MediatR;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Province.Handlers.Commands;

public class UpdateProvinceByIdHandler : IRequestHandler<UpdateProvinceRequest, ApiResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _repository;

    public UpdateProvinceByIdHandler(IProvinceRepository repository)
    {
        _repository = repository;
    }
    public async Task<ApiResponse<ProvinceDto>> Handle(UpdateProvinceRequest request,
            CancellationToken cancellationToken)
    {
        var provinceQuery = _repository.Query(p => p.Id == request.Id);
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
                        "NullProvince", ["The province you are trying to edit does not exist."]
                    }
                },
                Message = "Province not found."
            };
        }

        var existsQuery = _repository.Query(p => p.Name == request.Province.Name && p.RegionId == province.RegionId && p.Id != request.Id);
        var exists = await _repository.FirstOrDefaultAsync(existsQuery, cancellationToken);

        if (exists is not null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Name", ["A province with this name already exists in the selected region."]
                    }
                },
                Message = "Province name already exists in this region. Please choose a different name."
            };
        }

        province.Name = request.Province.Name ?? province.Name;
        province.Description = request.Province.Description ?? province.Description;
        province.Code = request.Province.Code ?? province.Code;
        province.Area = request.Province.Area ?? province.Area;

        MultiPolygon? boundariesGeometry = null;

        if (request.Province.BoundariesFile != null && request.Province.BoundariesFile.Length > 0)
        {
            try
            {
                using var streamReader = new StreamReader(request.Province.BoundariesFile.OpenReadStream());
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

                province.Boundaries = boundariesGeometry;
                province.Center = province.Boundaries.Centroid;
            }
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                return new ApiResponse<ProvinceDto>
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
                return new ApiResponse<ProvinceDto>
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
                return new ApiResponse<ProvinceDto>
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
            // No boundaries file provided; keep existing boundaries and center.
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
                Region = new()
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
