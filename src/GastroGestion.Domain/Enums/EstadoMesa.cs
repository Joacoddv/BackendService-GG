namespace GastroGestion.Domain.Enums;

/// <summary>
/// Operational state of a <see cref="GastroGestion.Domain.Mesas.Mesa"/>.
/// </summary>
public enum EstadoMesa
{
    Libre     = 0,
    Ocupada   = 1,
    Reservada = 2
}
