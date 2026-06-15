using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Stock.RegistrarMovimientoStock;

public sealed record RegistrarMovimientoStockCommand(
    Guid IngredienteId,
    TipoMovimientoStock Tipo,
    decimal Cantidad,
    Guid? OrdenTrabajoId,
    Guid? LineaPedidoId);
