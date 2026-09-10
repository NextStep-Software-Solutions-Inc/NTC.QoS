using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Notification.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Notification;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Manages in-app notifications for the authenticated user.
/// Super-admins receive notifications for region access requests; all users can
/// view and acknowledge their own notifications.
/// </summary>
[ApiController]
[Route("notifications", Name = "notifications")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Returns a paginated list of notifications for the currently authenticated user.
    /// </summary>
    /// <param name="pageSize">Number of records per page (default: 10).</param>
    /// <param name="pageNumber">Page number to retrieve (default: 1).</param>
    /// <param name="isRead">Optional filter: <c>true</c> returns only read notifications, <c>false</c> returns only unread, omit for all.</param>
    /// <returns>Paginated list of <see cref="NotificationDto"/>.</returns>
    [HttpGet]
    [RequirePermission(Permissions.ViewNotifications)]
    public async Task<ActionResult<PaginatedResponse<NotificationDto>>> GetAllNotifications(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageNumber = 1,
        [FromQuery] bool? isRead = null)
    {
        var result = await _mediator.Send(new GetAllNotificationsRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            IsRead = isRead
        });
        return Ok(result);
    }

    /// <summary>
    /// Returns a single notification by its ID.
    /// The notification must belong to the currently authenticated user.
    /// </summary>
    /// <param name="id">The unique identifier of the notification.</param>
    /// <returns>The <see cref="NotificationDto"/> if found; 404 otherwise.</returns>
    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewNotifications)]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> GetNotificationById(Guid id)
    {
        var result = await _mediator.Send(new GetNotificationByIdRequest(id));
        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Marks a single notification as read.
    /// The notification must belong to the currently authenticated user.
    /// </summary>
    /// <param name="id">The unique identifier of the notification.</param>
    /// <returns>The updated <see cref="NotificationDto"/>.</returns>
    [HttpPatch("{id}/read")]
    [RequirePermission(Permissions.UpdateNotifications)]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkAsRead(Guid id)
    {
        var result = await _mediator.Send(new MarkNotificationAsReadRequest(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Marks all unread notifications for the currently authenticated user as read.
    /// </summary>
    /// <returns>The number of notifications that were marked as read.</returns>
    [HttpPatch("read-all")]
    [RequirePermission(Permissions.UpdateNotifications)]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllAsRead()
    {
        var result = await _mediator.Send(new MarkAllNotificationsAsReadRequest());
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
