using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Template.Infrastructure.Realtime;

[Authorize]
public sealed class TrackingHub : Hub
{
    public Task SubscribeUser(Guid userId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
    }

    public Task SubscribeRegion(Guid regionId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"region:{regionId}");
    }
}
