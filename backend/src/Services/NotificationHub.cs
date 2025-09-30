using Expense.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace Expense.Api.Services;

public class NotificationsHub : Hub
{
    public const string Path = "/hubs/notifications";
}

public class NotificationHubDispatcher
{
    private readonly IHubContext<NotificationsHub> _hub;

    public NotificationHubDispatcher(IHubContext<NotificationsHub> hub)
    {
        _hub = hub;
    }

    public Task SendToUserAsync(Guid userId, Notification notif, CancellationToken ct)
    {
        return _hub.Clients.User(userId.ToString()).SendAsync("notification", new
        {
            id = notif.Id,
            type = notif.Type,
            title = notif.Title,
            message = notif.Message,
            createdAtUtc = notif.CreatedAtUtc,
            read = notif.Read
        }, ct);
    }
}

