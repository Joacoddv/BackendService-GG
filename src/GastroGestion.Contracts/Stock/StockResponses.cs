using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Stock;

public sealed record MovimientoStockResponse(
    Guid Id,
    Guid IngredienteId,
    TipoMovimientoStock Tipo,
    decimal Cantidad,
    DateTime FechaMovimiento);

public sealed record BalanceStockResponse(
    Guid IngredienteId,
    decimal Balance);
