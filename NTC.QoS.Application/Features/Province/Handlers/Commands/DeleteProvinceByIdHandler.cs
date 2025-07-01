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
        var provinceQuery = _repository.Query(p => p.Id == request.Id);
        var province = await _repository.FirstOrDefaultAsync(provinceQuery, cancellationToken);

        if (province is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProvince", [ "The province you are trying to delete does not exist."] 
                    }
                },
                Message = "Province not found."
            };
        }

        _repository.Delete(province);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"Province '{province.Name}' deleted successfully."
        };
    }
}
// This handler deletes a province by its ID. It first checks if the province exists, and if it does, it deletes it and returns a success message.