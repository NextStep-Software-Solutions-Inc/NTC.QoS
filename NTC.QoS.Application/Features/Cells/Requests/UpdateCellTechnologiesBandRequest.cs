using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

/// <summary>
/// Request to update a cell's supported technologies and bands.
/// This is independent from cellsite metadata updates (name, address, coordinates).
/// Allows admins to manage which technology-band combinations are active for a specific cell.
/// </summary>
public record UpdateCellTechnologiesBandRequest(Guid CellId, UpdateCellTechnologiesBandDto Payload) 
    : IRequest<ApiResponse<List<CellTechnologyBandDto>>>;
