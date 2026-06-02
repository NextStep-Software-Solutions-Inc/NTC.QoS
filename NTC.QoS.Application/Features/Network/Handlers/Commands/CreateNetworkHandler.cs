using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class CreateNetworkHandler(
    INetworkRepository networkRepository,
    IRepository<Domain.Entities.NetworkTemplate> networkTemplateRepository,
    IRepository<Domain.Entities.Band> bandRepository,
    IRepository<Domain.Entities.Municipality> municipalityRepository,
    IRepository<Domain.Entities.SystemType> systemTypeRepository,
    IServiceProviderRepository serviceProviderRepository) : IRequestHandler<CreateNetworkRequest, ApiResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly IRepository<Domain.Entities.NetworkTemplate> _networkTemplateRepository = networkTemplateRepository;
    private readonly IRepository<Domain.Entities.Band> _bandRepository = bandRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository = municipalityRepository;
    private readonly IRepository<Domain.Entities.SystemType> _systemTypeRepository = systemTypeRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<NetworkDto>> Handle(CreateNetworkRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Network.Name))
        {
            return new ApiResponse<NetworkDto>
            {
                Message = "Network name is required.",
                Errors = new Dictionary<string, string[]>
                {
                    { "InvalidName", ["Network name is required."] }
                },
                Succeeded = false,
            };
        }

        // Normalize the name to match the unique index on (ServiceProviderId, Name)
        // with IsDeleted = FALSE filter, preventing an unhandled unique-violation when a
        // network with the same ServiceProviderId/Name already exists.
        // Uses the shared normalization that matches the migration pattern:
        // lower(regexp_replace(btrim(Name), '\s*_\s*', '_', 'g'))
        var normalizedName = NetworkNameHelper.NormalizeStoredName(request.Network.Name);

        var duplicateNameQuery = _networkRepository.Query(n =>
            n.ServiceProviderId == request.Network.ServiceProviderId &&
            n.Name == normalizedName);
        var duplicateNetwork = await _networkRepository.FirstOrDefaultAsync(duplicateNameQuery, cancellationToken);
        if (duplicateNetwork is not null)
        {
            return new ApiResponse<NetworkDto>
            {
                Message = "A network with this name already exists for the service provider.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", ["A network with this name already exists for the service provider."] }
                },
                Succeeded = false,
            };
        }

        // NetworkTemplate is optional; only validate when an id is provided.
        Domain.Entities.NetworkTemplate? networkTemplate = null;
        if (request.Network.NetworkTemplateId.HasValue)
        {
            var networkTemplateQuery = _networkTemplateRepository.Query(nt => nt.Id == request.Network.NetworkTemplateId.Value);
            networkTemplate = await _networkTemplateRepository.FirstOrDefaultAsync(networkTemplateQuery, cancellationToken);
            if (networkTemplate is null)
            {
                return new ApiResponse<NetworkDto>
                {
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "NullNetworkTemplate", ["Network Template not found."] }
                    },
                    Message = "Network Template not found."
                };
            }
        }

        var systemTypeQuery = _systemTypeRepository.Query(st => st.Id == request.Network.SystemTypeId);
        var systemType = await _systemTypeRepository.FirstOrDefaultAsync(systemTypeQuery, cancellationToken);
        if (systemType is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullSystemType", ["System Type not found."] }
                },
                Message = "System Type not found."
            };
        }

        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.Id == request.Network.ServiceProviderId);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullServiceProvider", ["Service Provider not found."] }
                },
                Message = "Service Provider not found."
            };
        }

        if (serviceProvider.Service?.Type != ServiceTypeEnum.Wireless)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "InvalidServiceProvider", ["Service Provider must be of type Wireless for network creation."] }
                },
                Message = "Service Provider must be of type Wireless for network creation."
            };
        }

        // Validate Band
        var bandQuery = _bandRepository.Query(b => b.Id == request.Network.BandId);
        var band = await _bandRepository.FirstOrDefaultAsync(bandQuery, cancellationToken);
        if (band is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullBand", ["Band not found."] }
                },
                Message = "Band not found."
            };
        }

        // Validate Municipality (optional)
        Domain.Entities.Municipality? municipality = null;
        if (request.Network.MunicipalityId.HasValue)
        {
            var municipalityQuery = _municipalityRepository.Query(m => m.Id == request.Network.MunicipalityId);
            municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);
            if (municipality is null)
            {
                return new ApiResponse<NetworkDto>
                {
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "NullMunicipality", ["Municipality not found."] }
                    },
                    Message = "Municipality not found."
                };
            }
        }

        var newNetwork = new Domain.Aggregates.Network
        {
            Name = normalizedName,
            NetworkTemplateId = request.Network.NetworkTemplateId,
            SystemTypeId = systemType.Id,
            ServiceProviderId = serviceProvider.Id,
            BandId = band.Id,
            MunicipalityId = municipality?.Id,
            ULCenterFreq = request.Network.ULCenterFreq,
            DLCenterFreq = request.Network.DLCenterFreq,
            Bandwidth = request.Network.Bandwidth,
        };

        _networkRepository.Add(newNetwork);
        await _networkRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<NetworkDto>
        {
            Data = new NetworkDto
            {
                Id = newNetwork.Id,
                Name = newNetwork.Name,
                NetworkTemplate = networkTemplate is null ? null : new()
                {
                    Id = networkTemplate.Id,
                    Name = networkTemplate.Name,
                    Description = networkTemplate.Description,
                    URL = networkTemplate.URL
                },
                SystemTypeName = systemType.Name,
                ServiceProvider = new()
                {
                    Id = serviceProvider.Id,
                    MNC = serviceProvider.MNC,
                    MCC = serviceProvider.MCC,
                    Provider = new()
                    {
                        Id = serviceProvider.ProviderId,
                        Name = serviceProvider.Provider?.Name ?? string.Empty,
                        Description = serviceProvider.Provider?.Description,
                        Color = serviceProvider.Provider?.Color ?? string.Empty
                    },
                    Service = new()
                    {
                        Id = serviceProvider.ServiceId,
                        Name = serviceProvider.Service?.Name ?? string.Empty,
                        Type = serviceProvider.Service?.Type.ToString() ?? string.Empty
                    }
                },
                Band = new()
                {
                    Id = band.Id,
                    Name = band.Name,
                    Description = band.Description
                },
                Municipality = municipality == null ? null : new()
                {
                    Id = municipality.Id,
                    Name = municipality.Name,
                    Description = municipality.Description,
                    IsCity = municipality.IsCity,
                    Code = municipality.Code,
                    Area = municipality.Area
                },
                ULCenterFreq = newNetwork.ULCenterFreq,
                DLCenterFreq = newNetwork.DLCenterFreq,
                Bandwidth = newNetwork.Bandwidth,
            },
            Succeeded = true,
            Message = "Network created successfully."
        };
    }
}
