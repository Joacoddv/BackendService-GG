using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Stock;

public sealed record RegistrarMovimientoStockRequest(
    Guid IngredienteId,
    TipoMovimientoStock Tipo,
    decimal Cantidad,
    Guid? OrdenTrabajoId,
    Guid? LineaPedidoId,
    Guid? ProveedorId = null);
