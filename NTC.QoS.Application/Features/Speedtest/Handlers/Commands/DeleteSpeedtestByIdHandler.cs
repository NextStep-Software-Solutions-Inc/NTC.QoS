using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

public class DeleteSpeedtestByIdHandler(IRepository<Domain.Entities.Speedtest> repository) : IRequestHandler<DeleteSpeedtestByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Speedtest> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteSpeedtestByIdRequest request,
            CancellationToken cancellationToken)
    {
        var speedtest = await _repository.FindAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

        if (speedtest is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Error = new[] { "Speedtest not found." },
                Message = "The speedtest you are trying to delete does not exist."
            };
        }  

        _repository.Delete(speedtest);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Error = new[] { "" },
            Message = $"Speedtest has been successfully deleted."
        };
    }
}