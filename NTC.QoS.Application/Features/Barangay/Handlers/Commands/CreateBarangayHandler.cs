using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class CreateBarangayHandler(IRepository<Domain.Entities.Barangay> repository) : IRequestHandler<CreateBarangayRequest, ApiResponse<CreateBarangayDto>>
{
    private readonly IRepository<Domain.Entities.Barangay> _repository = repository;

    public async Task<ApiResponse<CreateBarangayDto>> Handle(CreateBarangayRequest request, CancellationToken cancellationToken)
    {
        var existingBarangay = await _repository.FindAsync(b => b.Name == request.Barangay.Name && b.MunicipalityId == request.Barangay.MunicipalityId && !b.IsDeleted, cancellationToken);

        if (existingBarangay != null)
        {
            return new ApiResponse<CreateBarangayDto>
            {
                Message = "Barangay already exists.",
                Succeeded = false
            };
        }

        var newBarangay = new Domain.Entities.Barangay
        {
            MunicipalityId = request.Barangay.MunicipalityId,
            Name = request.Barangay.Name,
            Description = request.Barangay.Description,
        };

        _repository.Add(newBarangay);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateBarangayDto>
        {
            Data = new CreateBarangayDto
            {
                MunicipalityId = newBarangay.MunicipalityId,
                Name = newBarangay.Name,
                Description = newBarangay.Description,
            },

            Message = "Barangay created successfully.",
            Succeeded = true
        };
    }
}