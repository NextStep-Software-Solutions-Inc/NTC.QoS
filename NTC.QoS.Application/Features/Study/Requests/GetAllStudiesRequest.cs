using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Requests;

public class GetAllStudiesRequest : IRequest<PaginatedResponse<StudyDto>>
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}
