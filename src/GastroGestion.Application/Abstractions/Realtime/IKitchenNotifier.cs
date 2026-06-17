using GastroGestion.Application.Pedidos.GetOrdenesByEstado;

namespace GastroGestion.Application.Abstractions.Realtime;

public interface IKitchenNotifier
{
    Task NotifyOtChangedAsync(OrdenTrabajoBoardItem item, CancellationToken ct = default);
}
