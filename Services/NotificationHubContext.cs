using Microsoft.AspNetCore.SignalR;
using StudentPortalAPI.Hubs;

namespace StudentPortalAPI.Services;

public class NotificationHubContext : INotificationHubContext
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubContext(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(string userId, string method, object? arg1 = null, CancellationToken cancellationToken = default)
        => await _hubContext.Clients.User(userId).SendAsync(method, arg1, cancellationToken);

    public async Task SendToGroupAsync(string groupName, string method, object? arg1 = null, CancellationToken cancellationToken = default)
        => await _hubContext.Clients.Group(groupName).SendAsync(method, arg1, cancellationToken);

    public async Task SendToAllAsync(string method, object? arg1 = null, CancellationToken cancellationToken = default)
        => await _hubContext.Clients.All.SendAsync(method, arg1, cancellationToken);
}
