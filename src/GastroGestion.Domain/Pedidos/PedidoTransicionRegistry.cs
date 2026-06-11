using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.Pedidos;

/// <summary>
/// A single valid transition row in the order state machine.
/// </summary>
/// <param name="Tipo">The order type this row applies to.</param>
/// <param name="EstadoDesde">Source state.</param>
/// <param name="EstadoHasta">Target state.</param>
/// <param name="RolesPermitidos">Roles that may trigger this transition. An empty list means any authenticated user.</param>
public record PedidoTransicion(
    TipoPedido Tipo,
    EstadoPedido EstadoDesde,
    EstadoPedido EstadoHasta,
    IReadOnlyList<RolUsuario> RolesPermitidos);

/// <summary>
/// Static registry of all valid <see cref="Pedido"/> state-machine transitions.
/// Encodes transition rules as data so that new transitions are data edits
/// and illegal transitions are rejected in one place (design §5d).
/// <para>
/// <b>Salón path:</b>  Abierto → Cerrado | Cancelado<br/>
/// <b>TakeAway path:</b>
///     Creado → Modificado | Preparandose | Cancelado<br/>
///     Modificado → Preparandose | Cancelado<br/>
///     Preparandose → ListoParaEntregar | Cancelado<br/>
///     ListoParaEntregar → Entregado<br/>
/// <b>Delivery path:</b> same logical flow as TakeAway.<br/>
/// TakeAway is its own counter channel with its own registry rows (no aliasing).
/// </para>
/// </summary>
public static class PedidoTransicionRegistry
{
    private static readonly List<PedidoTransicion> _transiciones = BuildRegistry();

    /// <summary>All valid transitions across both state machines.</summary>
    public static IReadOnlyList<PedidoTransicion> Transiciones => _transiciones.AsReadOnly();

    /// <summary>
    /// Returns the matching transition row, or null when no such transition exists.
    /// TakeAway is its own counter channel with its own rows in the registry —
    /// no aliasing to Mostrador/Delivery is applied.
    /// </summary>
    public static PedidoTransicion? Buscar(TipoPedido tipo, EstadoPedido desde, EstadoPedido hasta)
    {
        return _transiciones.FirstOrDefault(t =>
            t.Tipo == tipo && t.EstadoDesde == desde && t.EstadoHasta == hasta);
    }

    private static List<PedidoTransicion> BuildRegistry()
    {
        // Roles shorthand.
        var cajero       = new List<RolUsuario> { RolUsuario.Cajero };
        var mozo         = new List<RolUsuario> { RolUsuario.Mozo };
        var cocinero     = new List<RolUsuario> { RolUsuario.Cocinero };
        var cajeroAdmin  = new List<RolUsuario> { RolUsuario.Cajero, RolUsuario.Administrador };
        var mozoAdmin    = new List<RolUsuario> { RolUsuario.Mozo,   RolUsuario.Administrador };
        var cualquiera   = new List<RolUsuario> { RolUsuario.Cajero, RolUsuario.Mozo, RolUsuario.Cocinero, RolUsuario.Administrador };

        return
        [
            // ── Salón ─────────────────────────────────────────────────────────
            new(TipoPedido.Salon, EstadoPedido.Abierto,  EstadoPedido.Cerrado,           mozoAdmin),
            new(TipoPedido.Salon, EstadoPedido.Abierto,  EstadoPedido.Cancelado,         mozoAdmin),

            // ── Mostrador ────────────────────────────────────────────────────
            new(TipoPedido.TakeAway, EstadoPedido.Creado,            EstadoPedido.Modificado,        cajeroAdmin),
            new(TipoPedido.TakeAway, EstadoPedido.Creado,            EstadoPedido.Preparandose,      cajeroAdmin),
            new(TipoPedido.TakeAway, EstadoPedido.Creado,            EstadoPedido.Cancelado,         cajeroAdmin),
            new(TipoPedido.TakeAway, EstadoPedido.Modificado,        EstadoPedido.Preparandose,      cajeroAdmin),
            new(TipoPedido.TakeAway, EstadoPedido.Modificado,        EstadoPedido.Cancelado,         cajeroAdmin),
            new(TipoPedido.TakeAway, EstadoPedido.Preparandose,      EstadoPedido.ListoParaEntregar, cocinero),
            new(TipoPedido.TakeAway, EstadoPedido.Preparandose,      EstadoPedido.Cancelado,         cajeroAdmin),
            new(TipoPedido.TakeAway, EstadoPedido.ListoParaEntregar, EstadoPedido.Entregado,         cajeroAdmin),

            // ── Delivery ─────────────────────────────────────────────────────
            // Delivery uses the same logical flow as counter but the Entregado
            // transition is triggered by the delivery person (Cajero role covers this).
            new(TipoPedido.Delivery, EstadoPedido.Creado,            EstadoPedido.Modificado,        cajeroAdmin),
            new(TipoPedido.Delivery, EstadoPedido.Creado,            EstadoPedido.Preparandose,      cajeroAdmin),
            new(TipoPedido.Delivery, EstadoPedido.Creado,            EstadoPedido.Cancelado,         cajeroAdmin),
            new(TipoPedido.Delivery, EstadoPedido.Modificado,        EstadoPedido.Preparandose,      cajeroAdmin),
            new(TipoPedido.Delivery, EstadoPedido.Modificado,        EstadoPedido.Cancelado,         cajeroAdmin),
            new(TipoPedido.Delivery, EstadoPedido.Preparandose,      EstadoPedido.ListoParaEntregar, cocinero),
            new(TipoPedido.Delivery, EstadoPedido.Preparandose,      EstadoPedido.Cancelado,         cajeroAdmin),
            new(TipoPedido.Delivery, EstadoPedido.ListoParaEntregar, EstadoPedido.Entregado,         cajeroAdmin),
        ];
    }
}
