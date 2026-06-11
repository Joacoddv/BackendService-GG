namespace GastroGestion.Domain.Enums;

/// <summary>
/// Argentine IVA rate codes. Maps to decimal rates in <see cref="GastroGestion.Domain.ValueObjects.PorcentajeIVA"/>.
/// </summary>
/// <remarks>
/// Exento    = 0%   (rate 0)
/// ReducidoA = 10.5% (rate 0.105)
/// General   = 21%  (rate 0.21)
/// Diferencial = 27% (rate 0.27)
/// </remarks>
public enum AlicuotaIVA
{
    Exento      = 0,
    ReducidoA   = 1,
    General     = 2,
    Diferencial = 3
}
