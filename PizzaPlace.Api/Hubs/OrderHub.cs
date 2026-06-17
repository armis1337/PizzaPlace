using Microsoft.AspNetCore.SignalR;

namespace PizzaPlace.Api.Hubs;

public class OrderHub : Hub
{
    public async Task JoinGroup(string group) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, group);

    public async Task LeaveGroup(string group) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
}
