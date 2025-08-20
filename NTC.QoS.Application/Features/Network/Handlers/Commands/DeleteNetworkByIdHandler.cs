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
        var networkQuery = _networkRepository.Query(n => n.Id == request.Id);
        var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, cancellationToken);

        if (network is null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "NullNetwork", ["Network not found."] 
                    }
                },
                Message = $"Network with ID [{request.Id}] does not exists."
            };
        }
        _networkRepository.Delete(network);
        await _networkRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Message = "Network deleted successfully.",
            Succeeded = true
        };
    }
}
