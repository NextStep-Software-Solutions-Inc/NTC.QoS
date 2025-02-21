using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class DeleteCellByIdRequest : IRequest<ApiResponse<Domain.Aggregates.Cell>>
{
    public Guid Id { get; set; }
    public DeleteCellByIdRequest(Guid id)
    {
        Id = id;
    }
}