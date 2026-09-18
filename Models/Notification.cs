using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StudentPortalAPI.Models;

public class Notification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";

    /// <summary>
    /// When the notification was created.
    /// Serialized as "timestamp" for backward compatibility with the Angular frontend.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool Read { get; set; } = false;
    public string? Link { get; set; }
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Optional reference to the Notice that triggered this notification.
    /// Allows the frontend to link back to the notice detail page.
    /// </summary>
    public int? NoticeId { get; set; }
    [ForeignKey("NoticeId")]
    public Notice? Notice { get; set; }
}
