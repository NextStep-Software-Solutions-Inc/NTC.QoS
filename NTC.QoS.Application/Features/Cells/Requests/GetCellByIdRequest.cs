using MediatR;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetCellByIdRequest : IRequest<ApiResponse<Cell>>
{
    public Guid Id { get; set; }
    public GetCellByIdRequest(Guid id)
    {
        Id = id;
    }
}