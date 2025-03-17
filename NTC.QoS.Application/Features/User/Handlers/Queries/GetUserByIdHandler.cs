using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetUserByIdHandler(IUserRepository repository) : IRequestHandler<GetUserByIdRequest, ApiResponse<GetUserDto>>
{
    private readonly IUserRepository _repository = repository;

    public async Task<ApiResponse<GetUserDto>> Handle(GetUserByIdRequest request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (user is null)
        {
            return new ApiResponse<GetUserDto>
            {
                Succeeded = false,
                Message = "User not found.",
                Error = new[] { "User with the specified ID does not exist." },
            };
        }

        return new ApiResponse<GetUserDto>
        {
            Succeeded = true,
            Message = "User retrieved successfully.",
            Data = new GetUserDto
            {
                Id = user.Id,
                Role = new RoleUserDto
                {
                    Id = user.RoleId,
                    Name = user.Role?.Name ?? string.Empty,
                    Key = user.Role?.Key ?? string.Empty,
                    Description = user.Role?.Description ?? string.Empty
                },
                Organization = new OrganizationUserDto
                {
                    Id = user.OrganizationId,
                    Name = user.Organization?.Name ?? string.Empty,
                    Description = user.Organization?.Description ?? string.Empty
                },
                AuthUserId = user.AuthUserId
            },
        };
    }
}
