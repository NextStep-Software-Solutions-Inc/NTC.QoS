using MediatR;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace NTC.QoS.Application.Features.UserRegion.Handlers.Commands;

public class UpdateUserRegionStatusByIdHandler(
    IUserRegionRepository userRegionRepository,
    INotificationService notificationService,
    ILogger<UpdateUserRegionStatusByIdHandler> logger)
    : IRequestHandler<UpdateUserRegionStatusByIdRequest, ApiResponse<UserRegionDto>>
{
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    private readonly INotificationService _notificationService = notificationService;
    private readonly ILogger<UpdateUserRegionStatusByIdHandler> _logger = logger;

    public async Task<ApiResponse<UserRegionDto>> Handle(UpdateUserRegionStatusByIdRequest request, CancellationToken cancellationToken)
    {
        var userRegionQuery = _userRegionRepository.Query(ur => ur.Id == request.Id);
        var userRegion = await _userRegionRepository.FirstOrDefaultAsync(userRegionQuery, cancellationToken);

        if (userRegion is null)
        {
            return new ApiResponse<UserRegionDto>("User Region with the specified ID does not exist.");
        }

        var previousStatus = userRegion.Status;

        if (!string.IsNullOrEmpty(request.UserRegion.Status))
        {
            var parsedStatus = EnumHelper<UserRegionStatus>.ParseUserInput(request.UserRegion.Status);
            userRegion.Status = parsedStatus.ToString();
        }

        await _userRegionRepository.SaveChangesAsync(cancellationToken);

        // Notify the requesting user when their access status changes meaningfully
        if (userRegion.Status != previousStatus)
        {
            try
            {
                var newStatus = EnumHelper<UserRegionStatus>.ParseUserInput(userRegion.Status);
                var regionName = userRegion.Region!.Name;

                if (newStatus == UserRegionStatus.Active)
                {
                    await _notificationService.NotifyRegionAccessGrantedAsync(
                        recipientUserId: userRegion.UserId,
                        regionName: regionName,
                        userRegionId: userRegion.Id,
                        cancellationToken: cancellationToken);
                }
                else if (newStatus == UserRegionStatus.Declined)
                {
                    await _notificationService.NotifyRegionAccessDeclinedAsync(
                        recipientUserId: userRegion.UserId,
                        regionName: regionName,
                        userRegionId: userRegion.Id,
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status change notification for UserRegionId: {UserRegionId}", userRegion.Id);
            }
        }

        return new ApiResponse<UserRegionDto>
        {
            Succeeded = true,
            Message = "User Region updated successfully",
            Data = new UserRegionDto
            {
                Id = userRegion.Id,
                Status = userRegion.Status,
                User = new()
                {
                    Id = userRegion.User.Id,
                    AuthUserId = userRegion.User.AuthUserId,
                    Email = userRegion.User.Email,
                    Role = new()
                    {
                        Id = userRegion.User.RoleId,
                        Name = userRegion.User.Role.Name,
                        Key = userRegion.User.Role.Key,
                        Description = userRegion.User.Role.Description
                    },
                    Organization = new()
                    {
                        Id = userRegion.User.OrganizationId,
                        Name = userRegion.User.Organization.Name,
                        Description = userRegion.User.Organization.Description
                    }
                },
                Region = new()
                {
                    Id = userRegion.Region.Id,
                    Name = userRegion.Region.Name,
                    Code = userRegion.Region.Code,
                    Description = userRegion.Region.Description
                }
            }
        };
    }
}
