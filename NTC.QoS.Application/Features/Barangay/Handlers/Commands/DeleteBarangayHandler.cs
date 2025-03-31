using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class DeleteBarangayHandler(IBarangayRepository repository) : IRequestHandler<DeleteBarangayByIdRequest, ApiResponse<bool>>
{
    private readonly IBarangayRepository _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteBarangayByIdRequest request, CancellationToken cancellationToken)
    {
        var barangay = await _repository.FindAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);

        if (barangay is null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = "The Municipality you are trying to delete does not exist.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullBarangay", ["Barangay not found."]
                    }
                }

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
