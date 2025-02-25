using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Province.Requests;

public class UpdateProvinceRequest : IRequest<ApiResponse<UpdateProvinceDto>>
{
    public Guid Id { get; set; }
    public UpdateProvinceDto Province { get; set; } = null!;
}
