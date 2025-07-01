using MediatR;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.CellSector.Handlers.Commands;

public class DeleteCellSectorByIdHandler(IRepository<Domain.Entities.CellSector> cellSectorRepository) : IRequestHandler<DeleteCellSectorByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository = cellSectorRepository;

    public async Task<ApiResponse<bool>> Handle(DeleteCellSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var cellSectorQuery = _cellSectorRepository.Query(cs => cs.Id == request.Id);
        var cellSector = await _cellSectorRepository.FirstOrDefaultAsync(cellSectorQuery, cancellationToken);
        if (cellSector is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Cell sector not found"
            };
        }

        _cellSectorRepository.Delete(cellSector);
        await _cellSectorRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = "Cell sector deleted successfully."
        };
    }
}
