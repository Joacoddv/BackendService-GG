using GastroGestion.Application.Abstractions.Realtime;
using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Api.Hubs;
using GastroGestion.Contracts.Pedidos;
using Microsoft.AspNetCore.SignalR;

namespace GastroGestion.Api.Realtime;

internal sealed class SignalRKitchenNotifier : IKitchenNotifier
{
    private readonly IHubContext<KitchenHub> _hub;

    public SignalRKitchenNotifier(IHubContext<KitchenHub> hub) => _hub = hub;

    public async Task NotifyOtChangedAsync(OrdenTrabajoBoardItem item, CancellationToken ct = default)
        => await _hub.Clients.Group("kitchen")
            .SendAsync("OtChanged", item.ToResponse(), ct);
}
