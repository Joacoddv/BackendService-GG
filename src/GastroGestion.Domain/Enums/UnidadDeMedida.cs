namespace GastroGestion.Domain.Enums;

/// <summary>
/// Controlled vocabulary for ingredient units of measure.
/// Using an enum prevents "kg"/"kilo"/"Kg" drift in the stock ledger.
/// </summary>
public enum UnidadDeMedida
{
    Gramo      = 0,
    Kilogramo  = 1,
    Mililitro  = 2,
    Litro      = 3,
    Unidad     = 4,
    Porcion    = 5
}
