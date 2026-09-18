namespace StudentPortalAPI.Services;

/// <summary>
/// Strongly-typed notification hub context abstraction.
/// This allows Infrastructure to send SignalR messages without referencing the API project.
/// The concrete implementation wraps IHubContext and is registered in the API composition root.
/// </summary>
public interface INotificationHubContext
{
    Task SendToUserAsync(string userId, string method, object? arg1 = null, CancellationToken cancellationToken = default);
    Task SendToGroupAsync(string groupName, string method, object? arg1 = null, CancellationToken cancellationToken = default);
    Task SendToAllAsync(string method, object? arg1 = null, CancellationToken cancellationToken = default);
}
