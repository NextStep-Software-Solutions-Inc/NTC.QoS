using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class DeleteBarangayHandler(IRepository<Domain.Entities.Barangay> repository) : IRequestHandler<DeleteBarangayByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Barangay> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteBarangayByIdRequest request, CancellationToken cancellationToken)
    {
        var barangay = await _repository.FindAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);
        if (barangay == null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = "Barangay not found."
            };
        }
        _repository.Delete(barangay);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "Barangay deleted successfully."
        };
    }
}
