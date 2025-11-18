using MediatR;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class CreateMunicipalityHandler : IRequestHandler<CreateMunicipalityRequest, ApiResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _repository;
    private readonly IProvinceRepository _provinceRepository;

    public CreateMunicipalityHandler(IMunicipalityRepository repository, IProvinceRepository provinceRepository)
    {
        _repository = repository;
        _provinceRepository = provinceRepository;
    }

    public async Task<ApiResponse<MunicipalityDto>> Handle(CreateMunicipalityRequest request, CancellationToken cancellationToken)
    {
        var municipalityQuery = _repository.Query(m => m.Name == request.Municipality.Name && m.ProvinceId == request.Municipality.ProvinceId);
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

        var provinceQuery = _provinceRepository.Query(p => p.Id == request.Municipality.ProvinceId);
        var province = await _provinceRepository.FirstOrDefaultAsync(provinceQuery, cancellationToken);

        if (province is null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProvince", ["Province not found."]
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

        if (request.Municipality.BoundariesFile != null && request.Municipality.BoundariesFile.Length > 0)
        {
            try
            {
                using var streamReader = new StreamReader(request.Municipality.BoundariesFile.OpenReadStream());
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
                    boundariesGeometry = GeometryHelper.CreateMultiPolygon(new[] { polygon });
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

                newMunicipality.Boundaries = boundariesGeometry;
                newMunicipality.Center = newMunicipality.Boundaries.Centroid;
            }
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                return new ApiResponse<MunicipalityDto>
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
                return new ApiResponse<MunicipalityDto>
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
                return new ApiResponse<MunicipalityDto>
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
            newMunicipality.Boundaries = GeometryHelper.CreateMultiPolygon([]);
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
                Province = new()
                {
                    Id = newMunicipality.ProvinceId,
                    Name = newMunicipality.Province?.Name ?? string.Empty,
                    Description = newMunicipality.Province?.Description,
                    Region = new()
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
