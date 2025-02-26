using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class UpdateBarangayByIdHandler(IRepository<Domain.Entities.Barangay> repository) : IRequestHandler<UpdateBarangayByIdRequest, ApiResponse<Domain.Entities.Barangay>>
{
    private readonly IRepository<Domain.Entities.Barangay> _repository = repository;
    public async Task<ApiResponse<Domain.Entities.Barangay>> Handle(UpdateBarangayByIdRequest request, CancellationToken cancellationToken)
    {
        var barangay = await _repository.FindAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);
        if (barangay == null)
        {
            return new ApiResponse<Domain.Entities.Barangay>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Barangay Not found." },
                Message = "The barangay you are trying to edit does not exist."
            };
        }
        barangay.Name = request.Barangay.Name;
        barangay.Description = request.Barangay.Description;

        _repository.Update(barangay);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<Domain.Entities.Barangay>
        {
            Succeeded = true,
            Error = new[] { "" },
            Data = barangay,
            Message = "Request successful! Your changes have been saved."
        };
    }
}