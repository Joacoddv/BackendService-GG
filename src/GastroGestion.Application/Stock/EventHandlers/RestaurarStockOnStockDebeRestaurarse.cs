using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos.Events;
using GastroGestion.Domain.Stock;

namespace GastroGestion.Application.Stock.EventHandlers;

/// <summary>
/// When a still-reserved (Creada) OT is cancelled or its line removed, RELEASES the reservation
/// (<see cref="TipoMovimientoStock.LiberacionReserva"/>, +) so the held stock returns to available.
/// OTs already in Preparandose/Lista do not raise this event — their stock is already consumed.
/// </summary>
public sealed class RestaurarStockOnStockDebeRestaurarse : IDomainEventHandler<StockDebeRestaurarse>
{
    private readonly IMovimientoStockRepository _stock;
    private readonly IUnitOfWork                _uow;

    public RestaurarStockOnStockDebeRestaurarse(IMovimientoStockRepository stock, IUnitOfWork uow)
    {
        _stock = stock;
        _uow   = uow;
    }

    public async Task HandleAsync(StockDebeRestaurarse e, CancellationToken ct = default)
    {
        foreach (var ingrediente in e.RecetaSnapshot)
        {
            var mov = MovimientoStock.RegistrarMovimiento(
                ingrediente.IngredienteId,
                TipoMovimientoStock.LiberacionReserva,
                ingrediente.Cantidad.Valor,
                ordenTrabajoId: e.OrdenTrabajoId);
            await _stock.AddAsync(mov, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
