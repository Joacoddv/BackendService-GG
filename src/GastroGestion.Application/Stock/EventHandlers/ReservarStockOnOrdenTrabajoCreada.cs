using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos.Events;
using GastroGestion.Domain.Stock;

namespace GastroGestion.Application.Stock.EventHandlers;

/// <summary>
/// When an OT is created, RESERVES its recipe ingredients on the stock ledger (one negative
/// <see cref="TipoMovimientoStock.Reserva"/> movement per ingredient, linked to the OT).
/// </summary>
public sealed class ReservarStockOnOrdenTrabajoCreada : IDomainEventHandler<OrdenTrabajoCreada>
{
    private readonly IMovimientoStockRepository _stock;
    private readonly IUnitOfWork                _uow;

    public ReservarStockOnOrdenTrabajoCreada(IMovimientoStockRepository stock, IUnitOfWork uow)
    {
        _stock = stock;
        _uow   = uow;
    }

    public async Task HandleAsync(OrdenTrabajoCreada e, CancellationToken ct = default)
    {
        foreach (var ingrediente in e.RecetaSnapshot)
        {
            var mov = MovimientoStock.RegistrarMovimiento(
                ingrediente.IngredienteId,
                TipoMovimientoStock.Reserva,
                ingrediente.Cantidad.Valor,
                ordenTrabajoId: e.OrdenTrabajoId);
            await _stock.AddAsync(mov, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
