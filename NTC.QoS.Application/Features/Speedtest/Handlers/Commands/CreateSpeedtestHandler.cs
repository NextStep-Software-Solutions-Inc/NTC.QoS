using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

public class CreateSpeedtestHandler(IRepository<Domain.Entities.Speedtest> speedtestRepository,
                                    IRepository<Domain.Entities.File> fileRepository,
                                    IBarangayRepository barangayRepository,
                                    IServiceProviderRepository serviceProviderRepository,
                                    IGenerationRepository generationRepository,
                                    ICellRepository cellRepository) : IRequestHandler<CreateSpeedtestRequest, ApiResponse<CreateSpeedtestDto>>
{
    private readonly IRepository<Domain.Entities.Speedtest> _speedtestRepository = speedtestRepository;
    private readonly IRepository<Domain.Entities.File> _fileRepository = fileRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IGenerationRepository _generationRepository = generationRepository;
    private readonly ICellRepository _cellRepository = cellRepository;

    public async Task<ApiResponse<CreateSpeedtestDto>> Handle(CreateSpeedtestRequest request, CancellationToken cancellationToken)
    {
        var barangay = await _barangayRepository.GetByIdAsync(request.Speedtest.BarangayId, cancellationToken);
        if (barangay is null)
        {
            return new ApiResponse<CreateSpeedtestDto>
            {
                Message = "Barangay does not exist.",
                Succeeded = false,
            };
        }

        var serviceProvider = await _serviceProviderRepository.GetByIdAsync(request.Speedtest.ServiceProviderId, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<CreateSpeedtestDto>
            {
                Message = "Service provider does not exist.",
                Succeeded = false
            };
        }

        var generation = await _generationRepository.GetByIdAsync(request.Speedtest.GenerationId, cancellationToken);
        if (generation is null)
        {
            return new ApiResponse<CreateSpeedtestDto>
            {
                Message = "Generation does not exist.",
                Succeeded = false
            };
        }

        var cell = await _cellRepository.GetByIdAsync(request.Speedtest.CellId, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<CreateSpeedtestDto>
            {
                Message = "Cell does not exist.",
                Succeeded = false
            };
        }

        var existingSpeedtest = await _speedtestRepository.FindAsync(
            s => s.BarangayId == request.Speedtest.BarangayId &&
                 s.ServiceProviderId == request.Speedtest.ServiceProviderId &&
                 s.CellId == request.Speedtest.CellId &&
                 s.GenerationId == request.Speedtest.GenerationId &&
                 s.FileId == request.Speedtest.FileId &&
                 s.DateTime == request.Speedtest.DateTime &&
                 !s.IsDeleted, cancellationToken);

        if (existingSpeedtest is not null)
        {
            return new ApiResponse<CreateSpeedtestDto>
            {
                Message = "A Speedtest entry with the same DateTime already exists for this combination.",
                Succeeded = false
            };
        }

        var newSpeedtest = new Domain.Entities.Speedtest
        {
            BarangayId = request.Speedtest.BarangayId,
            ServiceProviderId = request.Speedtest.ServiceProviderId,
            GenerationId = request.Speedtest.GenerationId,
            CellId = request.Speedtest.CellId,
            FileId = request.Speedtest.FileId,
            DateTime = request.Speedtest.DateTime,
            Ping = request.Speedtest.Ping,
            Jitter = request.Speedtest.Jitter,
            RSSI = request.Speedtest.RSSI,
            UploadSpeed = request.Speedtest.UploadSpeed,
            DownloadSpeed = request.Speedtest.DownloadSpeed,
            CoorLatitude = request.Speedtest.CoorLatitude,
            CoorLongitude = request.Speedtest.CoorLongitude,
            PhoneModel = request.Speedtest.PhoneModel,
            MCC = request.Speedtest.MCC,
            MNC = request.Speedtest.MNC,
            TAC = request.Speedtest.TAC,
            SignalQuality = request.Speedtest.SignalQuality
        };

        _speedtestRepository.Add(newSpeedtest);
        await _speedtestRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateSpeedtestDto>
        {
            Data = new CreateSpeedtestDto
            {
                Id = newSpeedtest.Id,
                BarangayId = newSpeedtest.BarangayId,
                ServiceProviderId = newSpeedtest.ServiceProviderId,
                GenerationId = newSpeedtest.GenerationId,
                CellId = newSpeedtest.CellId,
                FileId = newSpeedtest.FileId,
                DateTime = newSpeedtest.DateTime,
                Ping = newSpeedtest.Ping,
                Jitter = newSpeedtest.Jitter,
                RSSI = newSpeedtest.RSSI,
                UploadSpeed = newSpeedtest.UploadSpeed,
                DownloadSpeed = newSpeedtest.DownloadSpeed,
                CoorLatitude = newSpeedtest.CoorLatitude,
                CoorLongitude = newSpeedtest.CoorLongitude,
                PhoneModel = newSpeedtest.PhoneModel,
                MCC = newSpeedtest.MCC,
                MNC = newSpeedtest.MNC,
                TAC = newSpeedtest.TAC,
                SignalQuality = newSpeedtest.SignalQuality
            },
            Message = "Speedtest created successfully",
            Succeeded = true
        };
    }
}
