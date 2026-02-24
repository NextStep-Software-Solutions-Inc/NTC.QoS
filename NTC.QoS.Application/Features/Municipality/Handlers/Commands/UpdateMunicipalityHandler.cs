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

public class UpdateMunicipalityByIdHandler : IRequestHandler<UpdateMunicipalityByIdRequest, ApiResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _repository;

    public UpdateMunicipalityByIdHandler(IMunicipalityRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<MunicipalityDto>> Handle(UpdateMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        // Use GetByIdForUpdateAsync to load the full entity (including Boundaries and Description),
        // bypassing the read-optimized Query() projection which omits those fields.
        var municipality = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (municipality is null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", ["Municipality not found."] }
                },
                Message = "Municipality not found."
            };
        }

        municipality.Name = request.Municipality.Name ?? municipality.Name;
        municipality.Description = request.Municipality.Description ?? municipality.Description;
        municipality.Code = request.Municipality.Code ?? municipality.Code;
        municipality.IsCity = request.Municipality.IsCity ?? municipality.IsCity;
        municipality.Area = request.Municipality.Area ?? municipality.Area;

        MultiPolygon? boundariesGeometry = null;

        if (request.Municipality.BoundariesFile != null && request.Municipality.BoundariesFile.Length > 0)
        {
            try
            {
                using var streamReader = new StreamReader(request.Municipality.BoundariesFile.OpenReadStream());
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
                municipality.Boundaries = boundariesGeometry;
                municipality.Center = municipality.Boundaries.Centroid;
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
            // No boundaries file provided; keep existing boundaries and center.
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
                Province = new()
                {
                    Id = municipality.ProvinceId,
                    Name = municipality.Province?.Name ?? string.Empty,
                    Description = municipality.Province?.Description,
                    Region = new()
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
