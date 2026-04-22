using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

/// <summary>
/// Handler for creating a new Speedtest.
/// Input validation is performed by CreateSpeedtestRequestValidator via FluentValidation pipeline.
/// Business rule validation (e.g., duplicate checking) is performed within this handler.
/// </summary>
public class CreateSpeedtestHandler(ISpeedtestRepository speedtestRepository) : IRequestHandler<CreateSpeedtestRequest, ApiResponse<Guid>>
{
    private readonly ISpeedtestRepository _speedtestRepository = speedtestRepository;

    public async Task<ApiResponse<Guid>> Handle(CreateSpeedtestRequest request, CancellationToken cancellationToken)
    {
        // All validation is handled by CreateSpeedtestRequestValidator via FluentValidation pipeline
        // Create point geometry
        Point location = GeometryHelper.CreatePoint(x: request.Speedtest.Longitude, y: request.Speedtest.Latitude);

        // Validate SignalQuality enum if provided
        string? validatedSignalQuality = null;
        if (!string.IsNullOrEmpty(request.Speedtest.SignalQuality))
        {
            var signalQualityEnum = EnumHelper<SignalQualityEnum>.ParseUserInput(request.Speedtest.SignalQuality);
            validatedSignalQuality = signalQualityEnum.ToString();
        }

        // Check for duplicates (same ServiceProvider, Generation, Technology, DateTime, and Address)
        var duplicateCheck = _speedtestRepository.QueryAsNoTracking()
            .Where(s => s.Address == request.Speedtest.Address &&
                       s.ServiceProviderId == request.Speedtest.ServiceProviderId &&
                       s.GenerationId == request.Speedtest.GenerationId &&
                       s.TechnologyId == request.Speedtest.TechnologyId &&
                       s.DateTime == request.Speedtest.DateTime)
            .Select(s => s.Id);
        var duplicateId = await _speedtestRepository.FirstOrDefaultAsync(duplicateCheck, cancellationToken);

        if (duplicateId != Guid.Empty)
        {
            return new ApiResponse<Guid>
            {
                Message = "A Speedtest entry with the same DateTime already exists for this combination.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", ["Date already exists in this combination."] }
                }
            };
        }

        // Create Speedtest with IsFileUploaded = false (manual CRUD operations)
        var newSpeedtest = new Domain.Entities.Speedtest
        {
            Address = request.Speedtest.Address,
            ServiceProviderId = request.Speedtest.ServiceProviderId,
            GenerationId = request.Speedtest.GenerationId,
            TechnologyId = request.Speedtest.TechnologyId,
            DateTime = request.Speedtest.DateTime,
            Ping = request.Speedtest.Ping ?? 0,
            Jitter = request.Speedtest.Jitter ?? 0,
            RSSI = request.Speedtest.RSSI,
            UploadSpeed = request.Speedtest.UploadSpeed,
            DownloadSpeed = request.Speedtest.DownloadSpeed,
            PhoneModel = request.Speedtest.PhoneModel ?? string.Empty,
            MCC = request.Speedtest.MCC ?? string.Empty,
            MNC = request.Speedtest.MNC ?? string.Empty,
            TAC = request.Speedtest.TAC ?? string.Empty,
            SignalQuality = request.Speedtest.IsNoBroadbandCoverage == true ? null : validatedSignalQuality,
            CellIdentifier = request.Speedtest.CellIdentifier ?? string.Empty,
            IsFixedBroadband = request.Speedtest.IsFixedBroadband,
            IsNoBroadbandCoverage = request.Speedtest.IsNoBroadbandCoverage,
            IsFileUploaded = false, // Always false for manual CRUD operations
            Location = location
        };

        if (newSpeedtest.IsNoBroadbandCoverage)
        {
            newSpeedtest.UploadSpeed = 0;
            newSpeedtest.DownloadSpeed = 0;
            newSpeedtest.Ping = 0;
            newSpeedtest.Jitter = 0;
        }

        _speedtestRepository.Add(newSpeedtest);
        await _speedtestRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<Guid>
        {
            Data = newSpeedtest.Id,
            Message = "Speedtest created successfully.",
            Succeeded = true
        };
    }
}
