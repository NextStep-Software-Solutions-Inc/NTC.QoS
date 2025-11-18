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
        var networkTemplateExistQuery = _repository.Query(nt => nt.Id == request.Id);
        var networkTemplateExist = await _repository.FirstOrDefaultAsync(networkTemplateExistQuery, cancellationToken);

        if (networkTemplateExist is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = $"Network Template Id [{request.Id}] not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "NetworkTemplate", new[] { $"No Network Template found with Id [{request.Id}]. It may not exist or has been deleted." } }
                }
            };
        }
        _repository.Delete(networkTemplateExist);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = "Network template deleted successfully."
        };
    }
}
