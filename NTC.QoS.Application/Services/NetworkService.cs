using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using Npgsql;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Shared service for resolving or creating <see cref="Domain.Aggregates.Network"/> records.
/// Used by cellular creation/update flows to avoid duplicating network lookup logic.
/// </summary>
public interface INetworkService
{
    /// <summary>
    /// Returns the ID of an existing <see cref="Domain.Aggregates.Network"/> that matches the given
    /// parameters, or creates and persists a new one.
    /// Returns <see cref="Guid.Empty"/> if the required <see cref="Domain.Entities.SystemType"/> is not found.
    /// </summary>
    Task<Guid> GetOrCreateNetworkAsync(
        Guid serviceProviderId,
        Guid? municipalityId,
        string municipalityName,
        Guid technologyId,
        Guid bandId,
        string technologyStandard,
        string bandName,
        decimal dlCenterFreq,
        decimal ulCenterFreq,
        decimal bandwidth,
        CancellationToken cancellationToken);
}

public class NetworkService(
    INetworkRepository networkRepository,
    IRepository<Domain.Entities.SystemType> systemTypeRepository,
    ILogger<NetworkService> logger) : INetworkService
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly IRepository<Domain.Entities.SystemType> _systemTypeRepository = systemTypeRepository;
    private readonly ILogger<NetworkService> _logger = logger;

    /// <inheritdoc/>
    public async Task<Guid> GetOrCreateNetworkAsync(
        Guid serviceProviderId,
        Guid? municipalityId,
        string municipalityName,
        Guid technologyId,
        Guid bandId,
        string technologyStandard,
        string bandName,
        decimal dlCenterFreq,
        decimal ulCenterFreq,
        decimal bandwidth,
        CancellationToken cancellationToken)
    {
        var networkNamePattern = $"{technologyStandard}_{bandName}_{municipalityName}";

        var existingNetworkId = await _networkRepository.FirstOrDefaultAsync(
            _networkRepository
                .Query(n => n.ServiceProviderId == serviceProviderId && n.Name == networkNamePattern)
                .Select(n => (Guid?)n.Id),
            cancellationToken);

        if (existingNetworkId.HasValue)
            return existingNetworkId.Value;

        var systemTypeId = await _systemTypeRepository.FirstOrDefaultAsync(
            _systemTypeRepository
                .Query(st => st.TechnologyId == technologyId)
                .Select(st => (Guid?)st.Id),
            cancellationToken);

        if (!systemTypeId.HasValue)
        {
            _logger.LogWarning(
                "SystemType not found for TechnologyId: {TechnologyId} (Technology: {Technology})",
                technologyId, technologyStandard);
            return Guid.Empty;
        }

        var newNetwork = new Domain.Aggregates.Network
        {
            Name = networkNamePattern,
            ServiceProviderId = serviceProviderId,
            SystemTypeId = systemTypeId.Value,
            BandId = bandId,
            MunicipalityId = municipalityId,
            ULCenterFreq = ulCenterFreq,
            DLCenterFreq = dlCenterFreq,
            Bandwidth = bandwidth
        };

        _networkRepository.Add(newNetwork);

        try
        {
            await _networkRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresException &&
                                           postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var networkIdCreatedByConcurrentRequest = await _networkRepository.FirstOrDefaultAsync(
                _networkRepository
                    .Query(n => n.ServiceProviderId == serviceProviderId && n.Name == networkNamePattern)
                    .Select(n => (Guid?)n.Id),
                cancellationToken);

            if (networkIdCreatedByConcurrentRequest.HasValue)
                return networkIdCreatedByConcurrentRequest.Value;

            throw;
        }

        _logger.LogInformation(
            "Network created: {NetworkName} (ServiceProviderId={ServiceProviderId}, TechnologyId={TechnologyId}, BandId={BandId})",
            newNetwork.Name, serviceProviderId, technologyId, bandId);

        return newNetwork.Id;
    }
}
