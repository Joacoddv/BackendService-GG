using Microsoft.AspNetCore.SignalR;

namespace GastroGestion.Api.Hubs;

public sealed class KitchenHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "kitchen");
        await base.OnConnectedAsync();
    }
}
