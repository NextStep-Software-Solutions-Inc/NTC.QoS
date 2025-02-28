using MediatR;
using NTC.QoS.Application.Features.NetworkTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.NetworkTemplate.Handlers.Commands;

public class DeleteNetworkTemplateByIdHandler(IRepository<Domain.Entities.NetworkTemplate> repository) : IRequestHandler<DeleteNetworkTemplateByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.NetworkTemplate> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteNetworkTemplateByIdRequest request, CancellationToken cancellationToken)
    {
        var networkTemplateExist = await _repository.FindAsync(nt => nt.Id == request.Id && !nt.IsDeleted, cancellationToken);
        if (networkTemplateExist == null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = $"Network Template Id [{request.Id}] not found.",
                Error = new[] { $"No Network Template found with Id [{request.Id}]. It may not exist or has been deleted." }
            };
        }
        networkTemplateExist.IsDeleted = true;
        _repository.Update(networkTemplateExist);
        await _repository.SaveChangesAsync();
        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"Network Template deleted successfully."
        };

    }
}
