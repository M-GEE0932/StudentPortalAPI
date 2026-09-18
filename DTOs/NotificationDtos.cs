using System.Text.Json.Serialization;

namespace StudentPortalAPI.DTOs;

/// <summary>
/// Filter parameters for querying notifications.
/// </summary>
public class NotificationFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool? Read { get; set; }
    public string? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Paged result for notifications. Alias for PagedResult&lt;NotificationDto&gt;.
/// Kept for backward compatibility — new code should use PagedResult&lt;T&gt; directly.
/// </summary>
public class NotificationPagedResult : PagedResult<NotificationDto> { }

/// <summary>
/// Notification DTO returned to the client.
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";

    /// <summary>
    /// When the notification was created.
    /// Serialized as "timestamp" for backward compatibility with the Angular frontend.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime CreatedAt { get; set; }

    public bool Read { get; set; }
    public string? Link { get; set; }

    /// <summary>
    /// If this notification was triggered by a notice, the notice ID.
    /// Allows the frontend to link to the notice detail page.
    /// </summary>
    public int? NoticeId { get; set; }
}

/// <summary>
/// Lightweight DTO for creating notifications from the client.
/// </summary>
public class CreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Link { get; set; }
}
