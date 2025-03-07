using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Commands
{
    public class CreateNAPHandler(
        IRepository<Domain.Aggregates.NAP> repository,
        IServiceProviderRepository serviceProviderRepository,
        IRepository<Domain.Entities.File> fileRepository,
        IZoneRepository zoneRepository) : IRequestHandler<CreateNAPRequest, ApiResponse<CreateNAPDto>>
    {
        private readonly IRepository<Domain.Aggregates.NAP> _repository = repository;
        private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
        private readonly IRepository<Domain.Entities.File> _fileRepository = fileRepository;
        private readonly IZoneRepository _zoneRepository = zoneRepository;

        public async Task<ApiResponse<CreateNAPDto>> Handle(CreateNAPRequest request, CancellationToken cancellationToken)
        {
            var serviceProvider = await _serviceProviderRepository.GetByIdAsync(request.CreateNAP.ServiceProviderId, cancellationToken);
            if (serviceProvider is null)
            {
                return new ApiResponse<CreateNAPDto>
                {
                    Message = "Service Provider does not exist.",
                    Succeeded = false
                };
            }

            if (request.CreateNAP.FileId.HasValue)
            {
                var file = await _fileRepository.FindAsync(f => f.Id == request.CreateNAP.FileId.Value, cancellationToken);
                if (file is null)
                {
                    return new ApiResponse<CreateNAPDto>
                    {
                        Message = "FileId does not exist.",
                        Succeeded = false
                    };
                }
            }

            if (request.CreateNAP.ZoneId.HasValue)
            {
                var zone = await _zoneRepository.FindAsync(z => z.Id == request.CreateNAP.ZoneId.Value, cancellationToken);
                if (zone is null)
                {
                    return new ApiResponse<CreateNAPDto>
                    {
                        Message = "ZoneId does not exist.",
                        Succeeded = false
                    };
                }
            }

            var existingNAP = await _repository.FindAsync(n => n.ServiceProviderId == request.CreateNAP.ServiceProviderId
                                                               && n.ZoneId == request.CreateNAP.ZoneId
                                                               && n.CoorLatitude == request.CreateNAP.CoorLatitude
                                                               && n.CoorLongitude == request.CreateNAP.CoorLongitude
                                                               && !n.IsDeleted, cancellationToken);

            if (existingNAP is not null)
            {
                return new ApiResponse<CreateNAPDto>
                {
                    Message = "NAP already exists.",
                    Succeeded = false
                };
            }

            var newNAP = new Domain.Aggregates.NAP
            {
                ServiceProviderId = serviceProvider.Id,
                ZoneId = request.CreateNAP.ZoneId,
                FileId = request.CreateNAP.FileId,
                CoorLatitude = request.CreateNAP.CoorLatitude,
                CoorLongitude = request.CreateNAP.CoorLongitude
            };

            _repository.Add(newNAP);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<CreateNAPDto>
            {
                Data = new CreateNAPDto
                {
                    ServiceProviderId = newNAP.ServiceProviderId,
                    ZoneId = newNAP.ZoneId,
                    FileId = newNAP.FileId,
                    CoorLatitude = newNAP.CoorLatitude,
                    CoorLongitude = newNAP.CoorLongitude,
                },
                Message = "NAP created successfully.",
                Succeeded = true
            };
        }
    }
}
