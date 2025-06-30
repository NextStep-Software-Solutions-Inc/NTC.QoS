using MediatR;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Province.Handlers.Commands;

public class CreateProvinceRequestHandler : IRequestHandler<CreateProvinceRequest, ApiResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository;

    public CreateProvinceRequestHandler(IProvinceRepository provinceRepository, IRegionRepository regionRepository)
    {
        _provinceRepository = provinceRepository;
    }

    public async Task<ApiResponse<ProvinceDto>> Handle(CreateProvinceRequest request, CancellationToken cancellationToken)
    {
        var existingProvinceQuery = _provinceRepository.Query(p => p.Name == request.Province.Name);
        var existingProvince = await _provinceRepository.FirstOrDefaultAsync(existingProvinceQuery, cancellationToken);

        if (existingProvince is not null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Message = "Province already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Name", ["A province with the specified name already exists."] 
                    }
                }
            };
        }

        var existsQuery = _provinceRepository.Query(p => p.Name == request.Province.Name && p.RegionId == request.Province.RegionId);
        var exists = await _provinceRepository.FirstOrDefaultAsync(existsQuery, cancellationToken);

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

        var newProvince = new Domain.Entities.Province
        {
            RegionId = request.Province.RegionId,
            Name = request.Province.Name,
            Description = request.Province.Description,
            Code = request.Province.Code ?? string.Empty,
            Area = request.Province.Area ?? 0f
        };

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

                newProvince.Boundaries = boundariesGeometry;
                newProvince.Center = newProvince.Boundaries.Centroid;
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
            newProvince.Boundaries = GeometryHelper.CreateMultiPolygon([]);
            newProvince.Center = GeometryHelper.CreatePoint(0, 0);
        }

        _provinceRepository.Add(newProvince);
        await _provinceRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ProvinceDto>
        {
            Data = new ProvinceDto
            {
                Id = newProvince.Id,
                Name = newProvince.Name,
                Description = newProvince.Description,
                Code = newProvince.Code,
                Area = newProvince.Area,
                Region = new RegionDto
                {
                    Id = newProvince.RegionId,
                    Name = newProvince.Region?.Name ?? string.Empty,
                    Description = newProvince.Region?.Description
                }
            },
            Message = "Province created successfully.",
            Succeeded = true
        };
    }
}
