using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.NAP.Handlers.Commands;

public class CreateNAPHandler(
    INAPRepository repository,
    IServiceProviderRepository serviceProviderRepository,
    IFileRepository fileRepository,
    IZoneRepository zoneRepository) : IRequestHandler<CreateNAPRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _repository = repository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IZoneRepository _zoneRepository = zoneRepository;

    public async Task<ApiResponse<NAPDto>> Handle(CreateNAPRequest request, CancellationToken cancellationToken)
    {
        var serviceProvider = await _serviceProviderRepository.GetByIdAsync(request.NAP.ServiceProviderId, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<NAPDto>
            {
                Message = "Service Provider not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "ServiceProviderId", ["Service provider with the specified Id does not exist."] 
                    }
                }

            };
        }

        if (request.NAP.FileId.HasValue)
        {
            var file = await _fileRepository.GetByIdAsync(request.NAP.FileId.Value, cancellationToken);
            if (file is null)
            {
                return new ApiResponse<NAPDto>
                {
                    Message = "FileId not found.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "FileId", ["File with the specified Id does not exist."] 
                        }
                    }
                };
            }
        }

        if (request.NAP.ZoneId.HasValue)
        {
            var zone = await _zoneRepository.GetByIdAsync(request.NAP.ZoneId.Value, cancellationToken);
            if (zone is null)
            {
                return new ApiResponse<NAPDto>
                {
                    Message = "ZoneId not found.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { 
                            "ZoneId", ["Zone with the specified Id does not exist."] 
                        }
                    }
                };
            }
        }

        var existingNAP = await _repository.FindAsync(n => n.ServiceProviderId == request.NAP.ServiceProviderId
                                                           && n.ZoneId == request.NAP.ZoneId
                                                           && n.CoorLatitude == request.NAP.CoorLatitude
                                                           && n.CoorLongitude == request.NAP.CoorLongitude
                                                           && !n.IsDeleted, cancellationToken);

        if (existingNAP is not null)
        {
            return new ApiResponse<NAPDto>
            {
                Message = "NAP already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", ["A NAP with the same Service Provider, Zone, and Coordinates already exists."] 
                    }
                }
            };
        }

        var NAP = new Domain.Aggregates.NAP
        {
            ServiceProviderId = serviceProvider.Id,
            ZoneId = request.NAP.ZoneId,
            FileId = request.NAP.FileId,
            CoorLatitude = request.NAP.CoorLatitude,
            CoorLongitude = request.NAP.CoorLongitude
        };

        _repository.Add(NAP);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<NAPDto>
        {
            Data = new NAPDto
            {
                Id = NAP.Id,
                ServiceProvider = new ()
                {
                    Id = NAP.ServiceProviderId,
                    Service = new ()
                    {
                        Id = NAP.ServiceProvider?.ServiceId ?? Guid.Empty,
                        Name = NAP.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = NAP.ServiceProvider?.Service?.Type.ToString() ?? string.Empty,
                    },
                    Provider = new ()
                    {
                        Id = NAP.ServiceProvider?.ProviderId ?? Guid.Empty,
                        Name = NAP.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Color = NAP.ServiceProvider?.Provider?.Color ?? string.Empty,
                    }
                },
                Zone = new ()
                {
                    Id = NAP.ZoneId ?? Guid.Empty,
                    Name = NAP.Zone?.Name ?? string.Empty,
                    Barangay = new ()
                    {
                        Id = NAP.Zone?.BarangayId ?? Guid.Empty,
                        Name = NAP.Zone?.Barangay?.Name ?? string.Empty,
                        Description = NAP.Zone?.Barangay?.Description,
                        Municipality = new ()
                        {
                            Id = NAP.Zone?.Barangay?.MunicipalityId ?? Guid.Empty,
                            Name = NAP.Zone?.Barangay?.Municipality?.Name ?? string.Empty,
                            Description = NAP.Zone?.Barangay?.Description,
                            IsCity = NAP.Zone?.Barangay?.Municipality?.IsCity ?? false,
                            Province = new ()
                            {
                                Id = NAP.Zone?.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                                Name = NAP.Zone?.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                                Description = NAP.Zone?.Barangay?.Municipality?.Province?.Description,
                                Region = new ()
                                {
                                    Id = NAP.Zone?.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                                    Name = NAP.Zone?.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                    Description = NAP.Zone?.Barangay?.Municipality?.Province?.Region?.Description
                                }
                            }
                        }
                    }
                },
                File = new ()
                {
                    Id = NAP.FileId ?? Guid.Empty,
                    Name = NAP.File?.Name ?? string.Empty,
                    Path = NAP.File?.Path ?? string.Empty,
                    UploadedBy = NAP.File?.UploadedBy ?? string.Empty,
                    UploadDate = NAP.File?.UploadDate ?? DateTime.MinValue,
                    Type = NAP.File?.Type.ToString() ?? string.Empty,
                    Status = NAP.File?.Status.ToString() ?? string.Empty
                },
                CoorLatitude = NAP.CoorLatitude,
                CoorLongitude = NAP.CoorLongitude,
            },
            Message = "NAP created successfully.",
            Succeeded = true
        };
    }
}
