using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Queries;

public class GetBarangayByIdHandler(IBarangayRepository barangayRepository) : IRequestHandler<GetBarangayByIdRequest, ApiResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository = barangayRepository;

    public async Task<ApiResponse<BarangayDto>> Handle(GetBarangayByIdRequest request, CancellationToken cancellationToken)
    {
        var barangay = await _barangayRepository.GetByIdAsync(request.Id, cancellationToken);

        if (barangay == null)
        {
            return new ApiResponse<BarangayDto>
            {
                Succeeded = false,
                Message = "Barangay not found."
            };
        }

        return new ApiResponse<BarangayDto>
        {
            Succeeded = true,
            Message = "Barangay retrieved successfully.",
            Data = new BarangayDto()
            {
                Id = barangay.Id,
                Name = barangay.Name,
                Description = barangay.Description,
                MunicipalityId = barangay.MunicipalityId,
                Municipality = barangay.Municipality != null ? new MunicipalityDto
                {
                    Id = barangay.Municipality.Id,
                    IsCity = barangay.Municipality.IsCity,
                    ProvinceId = barangay.Municipality.ProvinceId,
                    Name = barangay.Municipality.Name
                } : null
            }
        };
    }
}
