using MediatR;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetCellByIdRequest : IRequest<ApiResponse<CellDto>>
{
    public Guid Id { get; set; }
    public GetCellByIdRequest(Guid id)
    {
        Id = id;
    }
}