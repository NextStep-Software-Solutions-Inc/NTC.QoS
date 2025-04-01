using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class DeleteCellByIdRequest(Guid id) : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; } = id;
}
