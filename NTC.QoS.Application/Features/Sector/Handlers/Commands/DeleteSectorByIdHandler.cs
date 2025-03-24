using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Sector.Handlers.Commands;

public class DeleteSectorByIdHandler(IRepository<Domain.Entities.Sector> repository) : IRequestHandler<DeleteSectorByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Sector> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var sector = await _repository.FindAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);
        if (sector is null) return new ApiResponse<bool>("Sector does not exist.");

        _repository.Delete(sector);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Message = "Sector deleted successfully.",
            Succeeded = true
        };
    }
}
