using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class DeleteNetworkByIdHandler(INetworkRepository networkRepository) : IRequestHandler<DeleteNetworkByIdRequest, ApiResponse<bool>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    public async Task<ApiResponse<bool>> Handle(DeleteNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var network = await _networkRepository.FindAsync(n => n.Id == request.Id && !n.IsDeleted, cancellationToken);
        if (network is null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Error = new[] { $"Network with ID [{request.Id}] not found." },
                Message = $"Network with ID [{request.Id}] does not exist."
            };
        }
        _networkRepository.Delete(network);
        await _networkRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Message = "Network deleted successfully.",
            Succeeded = true
        };
    }
}
