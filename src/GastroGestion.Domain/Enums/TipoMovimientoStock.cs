namespace GastroGestion.Domain.Enums;

/// <summary>
/// Classifies each entry in the append-only stock ledger.
/// Sign convention: Compra / LiberacionReserva / DevolucionCancelacion are positive (inflow);
/// Consumo / Reserva are negative (outflow); Ajuste can be either (sign on Cantidad).
/// </summary>
public enum TipoMovimientoStock
{
    Compra                = 0,
    Consumo               = 1,
    Ajuste                = 2,
    Reserva               = 3,
    LiberacionReserva     = 4,
    DevolucionCancelacion = 5
}
