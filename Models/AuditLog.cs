using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentPortalAPI.Models;

public class AuditLog
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public JsonDocument? OldValues { get; set; }
    public JsonDocument? NewValues { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IPAddress { get; set; }
}
