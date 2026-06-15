using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Stock;

namespace GastroGestion.Application.Stock.RegistrarMovimientoStock;

public sealed class RegistrarMovimientoStockHandler
{
    private readonly IMovimientoStockRepository _stock;
    private readonly IUnitOfWork               _uow;

    public RegistrarMovimientoStockHandler(IMovimientoStockRepository stock, IUnitOfWork uow)
    {
        _stock = stock;
        _uow   = uow;
    }

    public async Task<Guid> Handle(RegistrarMovimientoStockCommand cmd, CancellationToken ct = default)
    {
        // NOTE: use RegistrarMovimiento (general factory), not RegistrarCompra
        var mov = MovimientoStock.RegistrarMovimiento(
            cmd.IngredienteId,
            cmd.Tipo,
            cmd.Cantidad,
            cmd.OrdenTrabajoId,
            cmd.LineaPedidoId);

        await _stock.AddAsync(mov, ct);
        await _uow.SaveChangesAsync(ct);

        return mov.Id;
    }
}
