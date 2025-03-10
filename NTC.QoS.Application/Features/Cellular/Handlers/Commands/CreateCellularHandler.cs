using MediatR;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Commands;

public class CreateCellularHandler(IRepository<Domain.Entities.Cellular> cellularRepository, IBarangayRepository barangayRepository, ICellRepository cellRepository, IServiceProviderRepository serviceProviderRepository, IGenerationBandRepository generationBandRepository, IFileRepository fileRepository) : IRequestHandler<CreateCellularRequest, ApiResponse<CreateCellularDto>>
{
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository = cellularRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IGenerationBandRepository _generationBandRepository = generationBandRepository;
    private readonly IFileRepository _fileRepository = fileRepository;

    public async Task<ApiResponse<CreateCellularDto>> Handle(CreateCellularRequest request, CancellationToken cancellationToken)
    {
        var existingCellular = await _cellularRepository.FindAsync(c => c.BarangayId == request.Cellular.BarangayId && c.CellId == request.Cellular.CellId && c.ServiceProviderId == request.Cellular.ServiceProviderId && c.GenerationBandId == request.Cellular.GenerationBandId && c.FileId == request.Cellular.FileId && c.RowNumber == request.Cellular.RowNumber && !c.IsDeleted, cancellationToken);
        if (existingCellular is null)
        {
            return new ApiResponse<CreateCellularDto>
            {
                Message = "Cellular already exists.",
                Succeeded = false
            };
        }
        
        var barangay = await _barangayRepository.GetByIdAsync(request.Cellular.BarangayId, cancellationToken);
        if (barangay is null)
        {
            return new ApiResponse<CreateCellularDto>
            {
                Message = "Barangay not found.",
                Succeeded = false
            };
        }

        var cell = await _cellRepository.GetByIdAsync(request.Cellular.CellId, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<CreateCellularDto>
            {
                Message = "Cell not found.",
                Succeeded = false
            };
        }

        var serviceProvider = await _serviceProviderRepository.GetByIdAsync(request.Cellular.ServiceProviderId, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<CreateCellularDto>
            {
                Message = "Service provider not found.",
                Succeeded = false
            };
        }

        var generationBand = await _generationBandRepository.GetByIdAsync(request.Cellular.GenerationBandId, cancellationToken);
        if (generationBand is null)
        {
            return new ApiResponse<CreateCellularDto>
            {
                Message = "Generation band not found.",
                Succeeded = false
            };
        }

        Domain.Entities.File? file = null;
        Guid? fileId = null;

        if (request.Cellular.FileId.HasValue)
        {
            file = await _fileRepository.GetByIdAsync(request.Cellular.FileId.Value, cancellationToken);

            if (file is null)
            {
                return new ApiResponse<CreateCellularDto>
                {
                    Message = "File not found.",
                    Succeeded = false
                };
            }
            fileId = file.Id;
        }

        var newCellular = new Domain.Entities.Cellular
        {
            BarangayId = barangay.Id,
            CellId = cell.Id,
            ServiceProviderId = serviceProvider.Id,
            GenerationBandId = generationBand.Id,
            FileId = fileId,
            RowNumber = request.Cellular.RowNumber,
        };

        _cellularRepository.Add(newCellular);
        await _cellularRepository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<CreateCellularDto>
        {
            Data = new CreateCellularDto
            {
                BarangayId = newCellular.BarangayId,
                CellId = newCellular.CellId,
                ServiceProviderId = newCellular.ServiceProviderId,
                GenerationBandId = newCellular.GenerationBandId,
                FileId = newCellular.FileId,
                RowNumber = newCellular.RowNumber,
            },
            Message = "Cellular record created successfully.",
            Succeeded = true
        };
    }
}