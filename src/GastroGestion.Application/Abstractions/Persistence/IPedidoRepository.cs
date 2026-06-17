using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IPedidoRepository
{
    Task<Pedido?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Batch load — used by CrearFactura to load multiple Pedidos at once.</summary>
    Task<IReadOnlyList<Pedido>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task AddAsync(Pedido pedido, CancellationToken ct = default);

    /// <summary>Flat board projection — never loads full Pedido aggregates (ADR-002).</summary>
    Task<IReadOnlyList<OrdenTrabajoBoardItem>> GetAllOrdenesTrabajoAsync(
        EstadoOT? estado, CancellationToken ct = default);
}
