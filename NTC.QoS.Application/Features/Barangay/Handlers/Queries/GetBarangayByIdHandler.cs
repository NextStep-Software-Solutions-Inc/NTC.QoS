using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Queries;

public class GetBarangayByIdHandler(IBarangayRepository barangayRepository) : IRequestHandler<GetBarangayByIdRequest, ApiResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository = barangayRepository;

    public async Task<ApiResponse<BarangayDto>> Handle(GetBarangayByIdRequest request, CancellationToken cancellationToken)
    {
        var barangayQuery = _barangayRepository.Query(b => b.Id.Equals(request.Id));
        var barangay = await _barangayRepository.FirstOrDefaultAsync(barangayQuery, cancellationToken);

        if (barangay is null)
        {
            return new ApiResponse<BarangayDto>
            {
                Succeeded = false,
                Message = "Barangay with the specified ID does not exist.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullBarangay", [ "Barangay not found." ]
                    }
                }
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
                Municipality = new MunicipalityDto
                {
                    Id = barangay.MunicipalityId,
                    Name = barangay.Municipality?.Name ?? "Unknown",
                    IsCity = barangay.Municipality?.IsCity ?? false,
                    Description = barangay.Municipality?.Description,
                    Province = new ProvinceDto
                    {
                        Id = barangay.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = barangay.Municipality?.Province?.Name ?? "Unknown",
                        Description = barangay.Municipality?.Province?.Description,
                        Region = new RegionDto
                        {
                            Id = barangay.Municipality?.Province?.Region?.Id ?? Guid.Empty,
                            Name = barangay.Municipality?.Province?.Region?.Name ?? "Unknown",
                            Description = barangay.Municipality?.Province?.Region?.Description,
                        }
                    }
                },
                Code = barangay.Code ?? string.Empty,
                Area = barangay.Area,
            }
        };
    }
}
