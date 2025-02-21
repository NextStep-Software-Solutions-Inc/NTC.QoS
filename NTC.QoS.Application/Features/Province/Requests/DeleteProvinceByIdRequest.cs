using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Province.Requests;

public class DeleteProvinceByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteProvinceByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


