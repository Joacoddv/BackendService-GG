using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos.Events;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Pedidos;

/// <summary>
/// Aggregate root for a customer order. Owns <see cref="LineaPedido"/> lines and
/// <see cref="OrdenTrabajo"/> OTs as v1 consistency boundary (design §2).
/// <para>
/// <b>Type invariants:</b><br/>
/// - Salon: requires <see cref="MesaId"/>; initial state Abierto; no DireccionEntrega.<br/>
/// - TakeAway/Delivery: no MesaId; initial state Creado.<br/>
/// - Delivery: requires <see cref="DireccionEntrega"/>.<br/>
/// </para>
/// <para>
/// <b>State machine:</b> transitions validated by <see cref="PedidoTransicionRegistry"/>,
/// role-gated, terminal-state locked. Auto-advances to ListoParaEntregar when all OTs
/// are Lista (counter/delivery orders only).
/// </para>
/// <para>
/// <b>Concurrency token:</b> <see cref="RowVersion"/> is a plain byte[] configured
/// as a row-version token by EF Core in phase 3 (no domain attributes, design §9).
/// </para>
/// </summary>
public class Pedido : AggregateRoot
{
    private readonly List<LineaPedido>   _lineas         = [];
    private readonly List<OrdenTrabajo>  _ordenesTrabajo = [];

    // ── Identity and classification ───────────────────────────────────────────

    public TipoPedido    Tipo            { get; private set; }
    public EstadoPedido  Estado          { get; private set; }

    /// <summary>Cross-boundary ref to the Mesa (Salon only).</summary>
    public Guid?         MesaId          { get; private set; }

    /// <summary>Cross-boundary ref to the Cliente.</summary>
    public Guid?         ClienteId       { get; private set; }

    /// <summary>
    /// Frozen delivery address snapshot (Delivery orders only).
    /// Null for Salon and TakeAway.
    /// </summary>
    public DireccionEntrega? DireccionEntrega { get; private set; }

    /// <summary>UTC timestamp when this Pedido was opened/created.</summary>
    public DateTime CreadoEnUtc { get; private set; }

    /// <summary>
    /// Optimistic concurrency token. Configured as RowVersion in EF phase 3.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    // ── Collections ───────────────────────────────────────────────────────────

    public IReadOnlyList<LineaPedido>  Lineas          => _lineas.AsReadOnly();
    public IReadOnlyList<OrdenTrabajo> OrdenesTrabajo  => _ordenesTrabajo.AsReadOnly();

    // ── EF Core / private ─────────────────────────────────────────────────────

#pragma warning disable CS8618
    private Pedido() { }
#pragma warning restore CS8618

    private Pedido(
        Guid id,
        TipoPedido tipo,
        Guid? mesaId,
        Guid? clienteId,
        DireccionEntrega? direccionEntrega,
        DateTime creadoEnUtc) : base(id)
    {
        Tipo             = tipo;
        MesaId           = mesaId;
        ClienteId        = clienteId;
        DireccionEntrega = direccionEntrega;
        CreadoEnUtc      = creadoEnUtc;

        Estado = tipo == TipoPedido.Salon
            ? EstadoPedido.Abierto
            : EstadoPedido.Creado;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new Pedido, enforcing type invariants.
    /// </summary>
    /// <param name="tipo">Order channel.</param>
    /// <param name="mesaId">Required for Salon; null for counter/delivery.</param>
    /// <param name="clienteId">Optional client link.</param>
    /// <param name="direccionEntrega">Required for Delivery; null otherwise.</param>
    /// <param name="creadoEnUtc">Creation timestamp (inject from app layer for testability).</param>
    public static Pedido Crear(
        TipoPedido tipo,
        Guid? mesaId,
        Guid? clienteId,
        DireccionEntrega? direccionEntrega,
        DateTime creadoEnUtc)
    {
        // Type invariants
        if (tipo == TipoPedido.Salon && mesaId is null)
            throw new DomainException("A Salon Pedido requires a MesaId.");
        if (tipo != TipoPedido.Salon && mesaId is not null)
            throw new DomainException("MesaId must be null for non-Salon orders.");
        if (tipo == TipoPedido.Delivery && direccionEntrega is null)
            throw new DomainException("A Delivery Pedido requires a DireccionEntrega.");
        if (tipo != TipoPedido.Delivery && direccionEntrega is not null)
            throw new DomainException("DireccionEntrega is only valid for Delivery orders.");

        return new Pedido(Guid.NewGuid(), tipo, mesaId, clienteId, direccionEntrega, creadoEnUtc);
    }

    // ── Lines ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a dish line to the order.
    /// Only valid while the order is in an editable state (not terminal).
    /// </summary>
    public LineaPedido AgregarLinea(Guid platoId, int cantidad, string? observaciones = null)
    {
        GuardarEstadoEditable("add a line");

        var linea = LineaPedido.Crear(platoId, cantidad, observaciones);
        _lineas.Add(linea);
        return linea;
    }

    /// <summary>
    /// Returns whether a given line is editable (has no OT or its OT is still Creada).
    /// </summary>
    public bool LineaEsEditable(Guid lineaId)
    {
        var ot = _ordenesTrabajo.FirstOrDefault(o => _lineas.Any(l => l.Id == lineaId && l.PlatoId == o.PlatoId));
        return ot is null || ot.Estado == EstadoOT.Creada;
    }

    /// <summary>
    /// Updates the quantity on an editable line.
    /// Throws if the line's OT has advanced beyond Creada.
    /// </summary>
    public void ActualizarLinea(Guid lineaId, int nuevaCantidad, string? observaciones)
    {
        GuardarEstadoEditable("update a line");

        var linea = GetLineaOrThrow(lineaId);

        if (!LineaEsEditable(lineaId))
            throw new DomainException(
                $"LineaPedido {lineaId} cannot be edited because its OT has advanced beyond Creada.");

        linea.ActualizarCantidad(nuevaCantidad);
        linea.ActualizarObservaciones(observaciones);
    }

    // ── State machine ─────────────────────────────────────────────────────────

    /// <summary>
    /// Transitions the Pedido to <paramref name="estadoNuevo"/>, validated against
    /// <see cref="PedidoTransicionRegistry"/> with role gate.
    /// Raises <see cref="PedidoEstadoCambiado"/> on success.
    /// If the new state is Cancelado also raises <see cref="PedidoCancelado"/> and
    /// cascades cancellation to all OTs (emitting <see cref="StockDebeRestaurarse"/>
    /// for Creada OTs).
    /// </summary>
    public void TransicionarEstado(EstadoPedido estadoNuevo, RolUsuario rol)
    {
        // Terminal-state guard
        if (Estado == EstadoPedido.Cancelado || Estado == EstadoPedido.Cerrado || Estado == EstadoPedido.Entregado)
            throw new DomainException(
                $"Cannot transition a Pedido in terminal state {Estado}.");

        var transicion = PedidoTransicionRegistry.Buscar(Tipo, Estado, estadoNuevo)
            ?? throw new DomainException(
                $"Transition from {Estado} to {estadoNuevo} is not valid for TipoPedido.{Tipo}.");

        // Role gate
        if (transicion.RolesPermitidos.Count > 0 && !transicion.RolesPermitidos.Contains(rol))
            throw new DomainException(
                $"Role {rol} is not authorised to transition a {Tipo} Pedido from {Estado} to {estadoNuevo}. " +
                $"Required: {string.Join(", ", transicion.RolesPermitidos)}.");

        var estadoAnterior = Estado;
        Estado = estadoNuevo;

        AddDomainEvent(new PedidoEstadoCambiado(
            Id, estadoAnterior, estadoNuevo, rol, DateTime.UtcNow));

        if (estadoNuevo == EstadoPedido.Cancelado)
            EjecutarCancelacionCascada(estadoAnterior, rol);
    }

    // ── OT generation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Generates one <see cref="OrdenTrabajo"/> per line in an all-or-nothing batch.
    /// Validates that every line has a confirmed price (snapshot must exist before
    /// stock moves are calculated) and blocks duplicate OT per Plato.
    /// Raises <see cref="OrdenTrabajoCreada"/> per new OT.
    /// </summary>
    /// <param name="lineasConReceta">
    /// Map from LineaPedido.PlatoId → the recipe snapshot to embed in the OT.
    /// The app layer resolves the recipe via a repository before calling this.
    /// </param>
    public void GenerarOrdenesTrabajo(
        IReadOnlyDictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>> lineasConReceta)
    {
        if (lineasConReceta is null || lineasConReceta.Count == 0)
            throw new DomainException("At least one recipe snapshot must be provided to generate OTs.");

        // All-or-nothing: validate all lines before creating any OT.
        foreach (var linea in _lineas)
        {
            if (!lineasConReceta.ContainsKey(linea.PlatoId))
                throw new DomainException(
                    $"No recipe snapshot provided for PlatoId {linea.PlatoId}. " +
                    "All order lines must have a recipe to generate OTs.");

            // Duplicate block
            if (_ordenesTrabajo.Any(ot => ot.PlatoId == linea.PlatoId))
                throw new DomainException(
                    $"An OrdenTrabajo for PlatoId {linea.PlatoId} already exists on this Pedido.");
        }

        // All checks passed — create all OTs.
        foreach (var linea in _lineas)
        {
            var snapshot = lineasConReceta[linea.PlatoId];
            var ot = OrdenTrabajo.Crear(linea.PlatoId, snapshot);
            _ordenesTrabajo.Add(ot);

            AddDomainEvent(new OrdenTrabajoCreada(Id, ot.Id, linea.PlatoId, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Marks a specific OT as Lista (cook finished).
    /// If all OTs are now Lista, auto-advances a counter/delivery Pedido to
    /// ListoParaEntregar via the normal transition path (role-gated with Cocinero).
    /// </summary>
    public void MarcarOrdenTrabajoLista(Guid ordenTrabajoId, RolUsuario rolCocinero)
    {
        var ot = GetOrdenTrabajoOrThrow(ordenTrabajoId);
        ot.MarcarLista();

        // Auto-advance for counter / delivery orders (not Salon)
        if (Tipo != TipoPedido.Salon &&
            Estado == EstadoPedido.Preparandose &&
            _ordenesTrabajo.All(o => o.Estado == EstadoOT.Lista || o.Estado == EstadoOT.Cancelada))
        {
            // Use the normal transition path so PedidoEstadoCambiado is raised.
            TransicionarEstado(EstadoPedido.ListoParaEntregar, rolCocinero);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void GuardarEstadoEditable(string accion)
    {
        if (Estado == EstadoPedido.Cancelado || Estado == EstadoPedido.Cerrado || Estado == EstadoPedido.Entregado)
            throw new DomainException(
                $"Cannot {accion} on a Pedido in terminal state {Estado}.");
    }

    private LineaPedido GetLineaOrThrow(Guid lineaId)
    {
        return _lineas.FirstOrDefault(l => l.Id == lineaId)
            ?? throw new DomainException($"LineaPedido {lineaId} not found on this Pedido.");
    }

    private OrdenTrabajo GetOrdenTrabajoOrThrow(Guid otId)
    {
        return _ordenesTrabajo.FirstOrDefault(o => o.Id == otId)
            ?? throw new DomainException($"OrdenTrabajo {otId} not found on this Pedido.");
    }

    private void EjecutarCancelacionCascada(EstadoPedido estadoAnterior, RolUsuario rol)
    {
        AddDomainEvent(new PedidoCancelado(Id, estadoAnterior, rol, DateTime.UtcNow));

        foreach (var ot in _ordenesTrabajo)
        {
            var estadoOtAntes = ot.Estado;
            ot.Cancelar();

            // Stock restoration only for OTs that were still Creada at the time of cancellation.
            if (estadoOtAntes == EstadoOT.Creada)
            {
                AddDomainEvent(new StockDebeRestaurarse(
                    Id, ot.Id, ot.RecetaSnapshot, DateTime.UtcNow));
            }
            // OTs in Preparandose or Lista: stock already consumed — no restoration (design §10 rule 2).
        }
    }
}
