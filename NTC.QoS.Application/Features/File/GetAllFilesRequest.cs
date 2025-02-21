using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.File;

public class GetAllFilesRequest : IRequest<ApiResponse<Domain.Entities.File>>
{

}
