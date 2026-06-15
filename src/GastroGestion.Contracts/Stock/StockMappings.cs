using GastroGestion.Application.Stock.RegistrarMovimientoStock;
using GastroGestion.Domain.Stock;

namespace GastroGestion.Contracts.Stock;

public static class StockMappings
{
    public static RegistrarMovimientoStockCommand ToCommand(this RegistrarMovimientoStockRequest request)
        => new(
            request.IngredienteId,
            request.Tipo,
            request.Cantidad,
            request.OrdenTrabajoId,
            request.LineaPedidoId);

    public static MovimientoStockResponse ToResponse(this MovimientoStock mov)
        => new(mov.Id, mov.IngredienteId, mov.Tipo, mov.Cantidad, mov.FechaMovimiento);
}
