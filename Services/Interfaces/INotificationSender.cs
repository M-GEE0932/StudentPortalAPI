namespace StudentPortalAPI.Services;

/// <summary>
/// Abstraction for sending real-time notifications.
/// Implemented in Infrastructure, consumed by Application services.
/// This avoids direct dependency on SignalR hubs from business services.
/// </summary>
public interface INotificationSender
{
    Task SendNotificationAsync(int userId, string message, string? type = null, CancellationToken cancellationToken = default);
    Task SendToAllAsync(string message, string? type = null, CancellationToken cancellationToken = default);
    Task SendToGroupAsync(string groupName, string message, string? type = null, CancellationToken cancellationToken = default);
}
