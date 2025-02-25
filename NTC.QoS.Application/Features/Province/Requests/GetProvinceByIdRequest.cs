using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
namespace NTC.QoS.Application.Features.Province.Requests;

public class GetProvinceByIdRequest : IRequest<ApiResponse<ProvinceDto>>
{
    public GetProvinceByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


