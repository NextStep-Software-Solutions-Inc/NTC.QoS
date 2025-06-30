using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Commands;

public class DeleteSectorByIdHandler(IRepository<Domain.Entities.Sector> repository) : IRequestHandler<DeleteSectorByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Sector> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var sectorQuery = _repository.Query(s => s.Id == request.Id);
        var sector = await _repository.FirstOrDefaultAsync(sectorQuery, cancellationToken);
        if (sector is null)
        {
            return new ApiResponse<bool>
            {
                Message = "Sector does not exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Invalid Id", ["Sector not found."]
                    }
                },
            };
        }

        _repository.Delete(sector);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Message = "Sector deleted successfully.",
            Succeeded = true
        };
    }
}
