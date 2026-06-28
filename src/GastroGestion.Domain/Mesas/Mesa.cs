using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.Mesas;

/// <summary>
/// Aggregate root for a dine-in table. Enforces the one-open-Pedido invariant:
/// a table cannot be assigned two concurrent active orders.
/// <para>
/// <see cref="RowVersion"/> is a plain byte array property. EF Core concurrency
/// configuration (IsRowVersion / IsConcurrencyToken) is done in phase 3
/// infrastructure — no attributes here (design §9).
/// </para>
/// </summary>
public class Mesa : AggregateRoot
{
    public int Numero { get; private set; }
    public int Capacidad { get; private set; }
    public EstadoMesa Estado { get; private set; }
    public bool Activa { get; private set; }

    /// <summary>
    /// Id of the currently open Pedido on this table, or null when free.
    /// </summary>
    public Guid? PedidoActivoId { get; private set; }

    /// <summary>
    /// Optimistic concurrency token. Configured as RowVersion in EF phase 3.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Optional X coordinate for visual floor-plan layout. Null when not positioned.</summary>
    public int? PosicionX { get; private set; }

    /// <summary>Optional Y coordinate for visual floor-plan layout. Null when not positioned.</summary>
    public int? PosicionY { get; private set; }

#pragma warning disable CS8618
    private Mesa() { } // EF Core
#pragma warning restore CS8618

    private Mesa(Guid id, int numero, int capacidad) : base(id)
    {
        Numero    = numero;
        Capacidad = capacidad;
        Estado    = EstadoMesa.Libre;
        Activa    = true;
    }

    /// <summary>Creates a new active <see cref="Mesa"/>.</summary>
    /// <param name="numero">Table number — must be positive.</param>
    /// <param name="capacidad">Seating capacity — must be greater than zero.</param>
    public static Mesa Crear(int numero, int capacidad)
    {
        if (numero <= 0)
            throw new DomainException("Mesa.Numero must be greater than zero.");
        if (capacidad <= 0)
            throw new DomainException("Mesa.Capacidad must be greater than zero.");

        return new Mesa(Guid.NewGuid(), numero, capacidad);
    }

    /// <summary>
    /// Updates the table number and seating capacity. Validates both the same way
    /// <see cref="Crear"/> does: number and capacity must be greater than zero.
    /// </summary>
    public void Actualizar(int numero, int capacidad)
    {
        if (numero <= 0)
            throw new DomainException("Mesa.Numero must be greater than zero.");
        if (capacidad <= 0)
            throw new DomainException("Mesa.Capacidad must be greater than zero.");

        Numero    = numero;
        Capacidad = capacidad;
    }

    /// <summary>
    /// Assigns an active Pedido to this table and marks it as occupied.
    /// Throws if a Pedido is already assigned (one-open-Pedido invariant).
    /// </summary>
    public void AsignarPedido(Guid pedidoId)
    {
        if (PedidoActivoId is not null)
            throw new DomainException(
                $"Mesa {Numero} already has an active Pedido ({PedidoActivoId}). " +
                "It must be closed or cancelled before assigning a new one.");

        PedidoActivoId = pedidoId;
        Estado         = EstadoMesa.Ocupada;
    }

    /// <summary>
    /// Releases the active Pedido and returns the table to Libre.
    /// </summary>
    public void LiberarPedido()
    {
        PedidoActivoId = null;
        Estado         = EstadoMesa.Libre;
    }

    /// <summary>
    /// Deactivates this table. Idempotent if already inactive.
    /// Throws if there is an active Pedido (deactivation guard).
    /// </summary>
    /// <summary>
    /// Sets the visual floor-plan coordinates for this table. Both values must be non-negative.
    /// </summary>
    /// <param name="x">Horizontal position in pixels or abstract grid units.</param>
    /// <param name="y">Vertical position in pixels or abstract grid units.</param>
    public void Ubicar(int x, int y)
    {
        if (x < 0) throw new DomainException("Mesa.PosicionX cannot be negative.");
        if (y < 0) throw new DomainException("Mesa.PosicionY cannot be negative.");
        PosicionX = x;
        PosicionY = y;
    }

    public void Desactivar()
    {
        if (PedidoActivoId is not null)
            throw new DomainException(
                $"Cannot deactivate Mesa {Numero} while it has an active Pedido ({PedidoActivoId}).");

        Activa = false;
    }
}
