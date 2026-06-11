namespace GastroGestion.Domain.Pedidos;

/// <summary>
/// State machine for a <see cref="OrdenTrabajo"/>.
/// This is a separate enum from any factura states — OTs have their own lifecycle.
/// </summary>
public enum EstadoOT
{
    Creada       = 0,
    Preparandose = 1,
    Lista        = 2,
    Cancelada    = 3
}
