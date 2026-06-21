using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos.Events;
using GastroGestion.Domain.Stock;

namespace GastroGestion.Application.Stock.EventHandlers;

/// <summary>
/// When an OT starts preparation, turns its RESERVA into CONSUMO: releases the reservation
/// (<see cref="TipoMovimientoStock.LiberacionReserva"/>, +) and records the actual consumption
/// (<see cref="TipoMovimientoStock.Consumo"/>, −) for the same ingredients. Net available balance
/// is unchanged by the transition — the stock was already held by the reservation.
/// </summary>
public sealed class ConsumirStockOnOrdenTrabajoIniciada : IDomainEventHandler<OrdenTrabajoIniciada>
{
    private readonly IMovimientoStockRepository _stock;
    private readonly IUnitOfWork                _uow;

    public ConsumirStockOnOrdenTrabajoIniciada(IMovimientoStockRepository stock, IUnitOfWork uow)
    {
        _stock = stock;
        _uow   = uow;
    }

    public async Task HandleAsync(OrdenTrabajoIniciada e, CancellationToken ct = default)
    {
        foreach (var ingrediente in e.RecetaSnapshot)
        {
            await _stock.AddAsync(MovimientoStock.RegistrarMovimiento(
                ingrediente.IngredienteId,
                TipoMovimientoStock.LiberacionReserva,
                ingrediente.Cantidad.Valor,
                ordenTrabajoId: e.OrdenTrabajoId), ct);

            await _stock.AddAsync(MovimientoStock.RegistrarMovimiento(
                ingrediente.IngredienteId,
                TipoMovimientoStock.Consumo,
                ingrediente.Cantidad.Valor,
                ordenTrabajoId: e.OrdenTrabajoId), ct);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
