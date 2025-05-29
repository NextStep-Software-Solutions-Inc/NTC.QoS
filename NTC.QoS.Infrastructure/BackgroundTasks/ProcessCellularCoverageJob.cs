using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.ExcelBindings;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class ProcessCellularCoverageJob : IProcessCellularCoverageJob
{
    private readonly ILogger<ProcessCellularCoverageJob> _logger;
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IRepository<Domain.Aggregates.Network> _networkRepository;
    private readonly IRepository<Domain.Entities.CellularCoverage> _cellularCoverageRepository;

    public ProcessCellularCoverageJob(
        ILogger<ProcessCellularCoverageJob> logger,
        IRepository<Domain.Entities.File> fileRepository,
        IRepository<Domain.Aggregates.Network> networkRepository,
        IRepository<Domain.Entities.CellularCoverage> cellularCoverageRepository
        )
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _networkRepository = networkRepository;
        _cellularCoverageRepository = cellularCoverageRepository;
    }

    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id.Equals(parameters.Id));
        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, token);

        if (file is null)
        {
            const string message = "File not found.";
            _logger.LogError("{Message}", message);
            throw new InvalidOperationException(message);
        }

        file.Status = FileStatusEnum.Processing;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        var cellularCoverageList = new List<Domain.Entities.CellularCoverage>();

        _logger.LogInformation("Pre-loading reference data.");

        var networkCache = await _networkRepository
            .Query(n => !n.IsDeleted)
            .Select(n => new { n.Id, n.Name })
            .ToDictionaryAsync(
                n => CacheKeyGenerator.GenerateNetworkCacheKey(n.Name),
                n => n.Id,
                token
            );
        _logger.LogInformation("Pre-loaded {Count} networks.", networkCache.Count);

        using var fileStream = new FileStream(parameters.Path, FileMode.Open, FileAccess.Read);
        _logger.LogInformation("Processing file: {FilePath}", parameters.Path);

        fileStream.Position = 0;
        _logger.LogInformation("Excel processing started.");

        var cellularCoverageRecords = ExcelHelper.ParseExcelToClass<CellularCoverageRecord>(fileStream);

        if (cellularCoverageRecords.Count is 0)
        {
            _logger.LogInformation("The Excel is empty.");
        }

        foreach (var record in cellularCoverageRecords)
        {
            _logger.LogInformation("Processing record: {Record}", record);

            ReportProgress($"Processing record: {record.Network}, {record.Latitude}, {record.Longitude}, {record.Strength}, {record.Address}");

            if (!networkCache.TryGetValue(record.Network, out var networkId))
            {
                var networkQuery = _networkRepository
                    .Query(n => !n.IsDeleted && n.Name.Equals(record.Network));

                var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, token);
                if (network is null)
                {
                    _logger.LogWarning("Network {Network} not found", record.Network);
                    await TerminateProcess(file, token);
                    return;
                }
                else
                {
                    networkId = network.Id;
                    networkCache[CacheKeyGenerator.GenerateNetworkCacheKey(record.Network)] = networkId;
                    _logger.LogInformation("Network {Network} found with ID {NetworkId}", record.Network, networkId);
                }
            }

            if (networkId != Guid.Empty)
            {
                var cellularCoverage = new Domain.Entities.CellularCoverage
                {
                    NetworkId = networkId,
                    Location = GeometryHelper.CreatePoint(new Coordinate(x: record.Longitude, y: record.Latitude)),
                    Strength = record.Strength,
                    Address = record.Address,
                };
                cellularCoverageList.Add(cellularCoverage);
            }
            else
            {
                _logger.LogWarning("Network ID is empty for network {Network}", record.Network);
            }
        }

        if (cellularCoverageList.Count != 0)
        {
            _cellularCoverageRepository.AddRange(cellularCoverageList);
            await _cellularCoverageRepository.SaveChangesAsync(token);
            _logger.LogInformation("{Count} cellular coverage records saved successfully (batched).", cellularCoverageList.Count);
        }
        else
        {
            _logger.LogInformation("No cellular coverage records to add.");
        }

        file.Status = FileStatusEnum.Success;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(token);

        _logger.LogInformation("File processing completed successfully.");
    }

    void ReportProgress(string message)
    {
        _logger.LogInformation(message);
    }

    private async Task TerminateProcess(Domain.Entities.File file, CancellationToken cancellationToken)
    {
        file.Status = FileStatusEnum.Failed;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Process terminated due to error in runtime.");
    }
}
