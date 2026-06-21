using GastroGestion.Application.Stock.GetBalancesStock;
using GastroGestion.Application.Stock.RegistrarMovimientoStock;
using GastroGestion.Domain.Stock;

namespace GastroGestion.Contracts.Stock;

public static class StockMappings
{
    public static IngredienteBalanceResponse ToResponse(this IngredienteBalanceResult r)
        => new(r.IngredienteId, r.Nombre, r.Unidad, r.Activo, r.Balance, r.StockMinimo, r.EnAlerta);

    public static RegistrarMovimientoStockCommand ToCommand(this RegistrarMovimientoStockRequest request)
        => new(
            request.IngredienteId,
            request.Tipo,
            request.Cantidad,
            request.OrdenTrabajoId,
            request.LineaPedidoId,
            request.ProveedorId);

    public static MovimientoStockResponse ToResponse(this MovimientoStock mov)
        => new(mov.Id, mov.IngredienteId, mov.Tipo, mov.Cantidad, mov.FechaMovimiento, mov.ProveedorId);
}
