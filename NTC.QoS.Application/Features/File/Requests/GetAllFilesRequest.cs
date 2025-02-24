using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.File.Requests;

public class GetAllFilesRequest : IRequest<ApiResponse<Domain.Entities.File>>
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}
