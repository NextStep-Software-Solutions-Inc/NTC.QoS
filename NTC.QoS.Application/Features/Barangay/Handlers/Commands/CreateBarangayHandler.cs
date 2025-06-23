using MediatR;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class CreateBarangayHandler : IRequestHandler<CreateBarangayRequest, ApiResponse<BarangayDto>>
{
    private readonly IBarangayRepository _repository;
    private readonly IMunicipalityRepository _municipalityRepository;

    public CreateBarangayHandler(IBarangayRepository repository, IMunicipalityRepository municipalityRepository)
    {
        _repository = repository;
        _municipalityRepository = municipalityRepository;
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

        // Process Boundaries from IFormFile
        if (request.Barangay.BoundariesFile != null && request.Barangay.BoundariesFile.Length > 0)
        {
            try
            {
                using var streamReader = new StreamReader(request.Barangay.BoundariesFile.OpenReadStream());
                string geoJsonString = await streamReader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(geoJsonString))
                {
                    throw new ArgumentException("Provided boundaries GeoJSON file is empty or contains only whitespace.");
                }

                var geoJsonReader = new GeoJsonReader();
                Geometry? receivedGeometry = null;

                try
                {
                    var featureCollection = geoJsonReader.Read<FeatureCollection>(geoJsonString);
                    // Find the first Polygon or MultiPolygon geometry within the features
                    receivedGeometry = featureCollection?
                        .FirstOrDefault(f => f.Geometry is Polygon || f.Geometry is MultiPolygon)?
                        .Geometry;
                }
                catch (Newtonsoft.Json.JsonSerializationException)
                {
                    // If it failed as FeatureCollection, try to parse as a direct Geometry
                    // This will handle cases where the root object is a Polygon or MultiPolygon directly
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
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                return new ApiResponse<BarangayDto>
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
                return new ApiResponse<BarangayDto>
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
                return new ApiResponse<BarangayDto>
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