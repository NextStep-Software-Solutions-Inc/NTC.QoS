using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Province.Handlers.Commands;

public class DeleteProvinceByIdHandler(IRepository<Domain.Entities.Province> repository) : IRequestHandler<DeleteProvinceByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Province> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteProvinceByIdRequest request,
            CancellationToken cancellationToken)
    {
        var province = await _repository.FindAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);

        if (province == null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Error = new[] { "Province not found." },
                Message = "The province you are trying to delete does not exist."
            };
        }

        _repository.Delete(province);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Error = new[] { "" },
            Message = $"Province '{province.Name}' has been successfully deleted."
        };
    }
}