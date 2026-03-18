using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Represents an in-app notification delivered to a specific user.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// The user who receives this notification.
    /// </summary>
    public Guid RecipientUserId { get; set; }
    public virtual User Recipient { get; set; } = null!;

    /// <summary>
    /// The type of notification (e.g. RegionAccessRequest).
    /// </summary>
    public string Type { get; set; } = NotificationType.Unknown.ToString();

    /// <summary>
    /// Short notification title shown in the notification list.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed notification message body.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference to the related entity (e.g. UserRegion ID for access requests).
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Whether the recipient has read this notification.
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// When the notification was marked as read.
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
