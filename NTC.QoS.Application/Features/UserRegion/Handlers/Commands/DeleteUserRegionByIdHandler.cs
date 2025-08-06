using MediatR;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.UserRegion.Handlers.Commands;

public class DeleteUserRegionByIdHandler(IUserRegionRepository userRegionRepository) : IRequestHandler<DeleteUserRegionByIdRequest, ApiResponse<bool>>
{
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;

    public async Task<ApiResponse<bool>> Handle(DeleteUserRegionByIdRequest request, CancellationToken cancellationToken)
    {
        var userRegionQuery = _userRegionRepository.Query(ur => ur.Id == request.Id);
        var userRegion = await _userRegionRepository.FirstOrDefaultAsync(userRegionQuery, cancellationToken);

        if (userRegion is null)
        {
            return new ApiResponse<bool>("User region not found.");
        }

        _userRegionRepository.Delete(userRegion);
        await _userRegionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "User region deleted successfully."
        };
    }
}
