using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

public class CreateSpeedtestHandler(IRepository<Domain.Entities.Speedtest> repository) : IRequestHandler<CreateSpeedtestRequest, ApiResponse<CreateSpeedtestDto>>
{
    private readonly IRepository<Domain.Entities.Speedtest> _repository = repository;

    public async Task<ApiResponse<CreateSpeedtestDto>> Handle(CreateSpeedtestRequest request, CancellationToken cancellationToken)
    {
        var existingSpeedtest = await _repository.FindAsync(
            s => s.BarangayId == request.Speedtest.BarangayId &&
                 s.ServiceProviderId == request.Speedtest.ServiceProviderId &&
                 s.DateTime == request.Speedtest.DateTime &&
                 !s.IsDeleted,
            cancellationToken);

        if (existingSpeedtest is not null)
        {
            return new ApiResponse<CreateSpeedtestDto>
            {
                Message = "Speedtest already exists",
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

        _repository.Add(newSpeedtest);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateSpeedtestDto>
        {
            Data = new CreateSpeedtestDto
            {
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
