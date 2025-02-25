using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Province.Requests;

public class CreateProvinceRequest : IRequest<ApiResponse<CreateProvinceDto>>
{
    public required CreateProvinceDto Province { get; set; }
}
