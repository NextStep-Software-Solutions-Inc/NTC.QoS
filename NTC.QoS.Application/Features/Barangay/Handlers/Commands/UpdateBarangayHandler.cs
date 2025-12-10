using MediatR;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class UpdateBarangayByIdHandler : IRequestHandler<UpdateBarangayByIdRequest, ApiResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository;

    public UpdateBarangayByIdHandler(IBarangayRepository barangayRepository)
    {
        _barangayRepository = barangayRepository;
    }
    public async Task<ApiResponse<BarangayDto>> Handle(UpdateBarangayByIdRequest request, CancellationToken cancellationToken)
    {
        var barangayQuery = _barangayRepository.Query(b => b.Id == request.Id);
        var barangay = await _barangayRepository.FirstOrDefaultAsync(barangayQuery, cancellationToken);

        if (barangay is null)
        {
            return new ApiResponse<BarangayDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullBarangay", [ "Barangay not found." ]
                    }
                },
                Message = "The barangay you are trying to edit does not exist."
            };
        }

        MultiPolygon? boundariesGeometry = null;

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

                barangay.Boundaries = boundariesGeometry;
                barangay.Center = barangay.Boundaries.Centroid;
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
            barangay.Boundaries = barangay.Boundaries;
            barangay.Center = barangay.Center;
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
                Municipality = new()
                {
                    Id = barangay.MunicipalityId,
                    Name = barangay.Municipality?.Name ?? string.Empty,
                    IsCity = barangay.Municipality != null && barangay.Municipality.IsCity,
                    Province = new()
                    {
                        Id = barangay.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = barangay.Municipality?.Province?.Name ?? string.Empty,
                        Region = new()
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
