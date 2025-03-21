using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class UpdateBarangayByIdHandler(IBarangayRepository barangayRepository) : IRequestHandler<UpdateBarangayByIdRequest, ApiResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    public async Task<ApiResponse<BarangayDto>> Handle(UpdateBarangayByIdRequest request, CancellationToken cancellationToken)
    {
        var barangay = await _barangayRepository.FindAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);
        if (barangay is null)
        {
            return new ApiResponse<BarangayDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Barangay not found." },
                Message = "The barangay you are trying to edit does not exist."
            };
        }
        barangay.Name = request.Barangay.Name;
        barangay.Description = request.Barangay.Description;

        _barangayRepository.Update(barangay);
        await _barangayRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<BarangayDto>
        {
            Succeeded = true,
            Error = new[] { "" },
            Message = "Request successful! Your changes have been saved.",
            Data = new BarangayDto()
            {
                Id = barangay.Id,
                Name = barangay.Name,
                Description = barangay.Description,
                Municipality = new MunicipalityDto
                {
                    Id = barangay.MunicipalityId,
                    Name = barangay.Municipality?.Name ?? "Unknown",
                }
            }
        };
    }
}