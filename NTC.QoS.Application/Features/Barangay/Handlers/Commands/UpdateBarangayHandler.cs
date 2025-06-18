using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class UpdateBarangayByIdHandler(IBarangayRepository barangayRepository) : IRequestHandler<UpdateBarangayByIdRequest, ApiResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    public async Task<ApiResponse<BarangayDto>> Handle(UpdateBarangayByIdRequest request, CancellationToken cancellationToken)
    {
        var barangayQuery = _barangayRepository.Query(b => b.Id == request.Id && !b.IsDeleted);
        var barangay = await _barangayRepository.FirstOrDefaultAsync(barangayQuery, cancellationToken);

        if (barangay is null)
        {
            return new ApiResponse<BarangayDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]> 
                { 
                    { 
                        "NullBarangay", ["Barangay not found."]
                    } 
                },
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
            Message = "Barangay updated successfully.",
            Data = new BarangayDto
            {
                Id = barangay.Id,
                Name = barangay.Name,
                Description = barangay.Description,
                Municipality = new MunicipalityDto
                {
                    Id = barangay.MunicipalityId,
                    Name = barangay.Municipality?.Name ?? string.Empty,
                    Description = barangay.Municipality?.Description,
                    IsCity = barangay.Municipality != null && barangay.Municipality.IsCity,
                    Province = new ProvinceDto
                    {
                        Id = barangay.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = barangay.Municipality?.Province?.Name ?? string.Empty,
                        Description = barangay.Municipality?.Province?.Description,
                        Region = new RegionDto
                        {
                            Id = barangay.Municipality?.Province?.RegionId ?? Guid.Empty,
                            Name = barangay.Municipality?.Province?.Region?.Name ?? string.Empty,
                            Description = barangay.Municipality?.Province?.Region?.Description
                        }
                    }
                },
                Code = barangay.Code,
                Area = barangay.Area,
            },
        };
    }
}
