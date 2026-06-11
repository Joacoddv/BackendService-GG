using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Pedidos;

/// <summary>
/// Kitchen work order for one dish line of a <see cref="Pedido"/>.
/// Owned by <see cref="Pedido"/> for v1 (design §2, §10 — natural aggregate boundary
/// for "all OTs Lista → advance" and cancel-cascade invariants).
/// <para>
/// State machine: Creada → Preparandose → Lista | Cancelada.
/// Cook assignment moves OT to Preparandose automatically.
/// </para>
/// <para>
/// The recipe snapshot is captured at creation so stock restoration and auditing
/// are correct even if the Plato recipe is later changed (design §7).
/// </para>
/// </summary>
public class OrdenTrabajo : Entity
{
    /// <summary>Cross-boundary reference to the Plato being produced.</summary>
    public Guid PlatoId { get; private set; }

    /// <summary>Current OT state.</summary>
    public EstadoOT Estado { get; private set; }

    /// <summary>
    /// Optional cook assigned to this OT. Setting triggers state → Preparandose.
    /// Cross-boundary ref to the employee.
    /// </summary>
    public LegajoId? CocineroAsignado { get; private set; }

    /// <summary>
    /// Snapshot of the recipe as it was at OT creation time.
    /// Used for stock moves and cancellation restoration.
    /// </summary>
    public IReadOnlyList<LineaRecetaSnapshot> RecetaSnapshot { get; private set; }

    private OrdenTrabajo(
        Guid id,
        Guid platoId,
        IReadOnlyList<LineaRecetaSnapshot> recetaSnapshot) : base(id)
    {
        PlatoId        = platoId;
        Estado         = EstadoOT.Creada;
        RecetaSnapshot = recetaSnapshot;
    }

    // EF Core parameterless ctor.
#pragma warning disable CS8618
    protected OrdenTrabajo() { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new OT in state Creada with the given recipe snapshot.
    /// </summary>
    internal static OrdenTrabajo Crear(Guid platoId, IReadOnlyList<LineaRecetaSnapshot> recetaSnapshot)
    {
        if (platoId == Guid.Empty)
            throw new DomainException("OrdenTrabajo.PlatoId cannot be empty.");
        if (recetaSnapshot is null || recetaSnapshot.Count == 0)
            throw new DomainException("OrdenTrabajo must have at least one recipe snapshot line.");

        return new OrdenTrabajo(Guid.NewGuid(), platoId, recetaSnapshot);
    }

    /// <summary>
    /// Assigns a cook and transitions the OT to Preparandose.
    /// Throws if the OT is not in state Creada.
    /// </summary>
    public void AsignarCocinero(LegajoId cocinero)
    {
        if (cocinero is null)
            throw new DomainException("CocineroAsignado cannot be null.");
        if (Estado != EstadoOT.Creada)
            throw new DomainException(
                $"Cannot assign a cook to an OT in state {Estado}. Only Creada OTs accept cook assignment.");

        CocineroAsignado = cocinero;
        Estado           = EstadoOT.Preparandose;
    }

    /// <summary>
    /// Marks the OT as Lista (cook finished).
    /// Only valid when the OT is Preparandose.
    /// </summary>
    internal void MarcarLista()
    {
        if (Estado != EstadoOT.Preparandose)
            throw new DomainException(
                $"Cannot mark OT as Lista from state {Estado}. Required: Preparandose.");

        Estado = EstadoOT.Lista;
    }

    /// <summary>
    /// Cancels this OT. Only the parent Pedido's cancel cascade calls this.
    /// Valid from any non-terminal state.
    /// </summary>
    internal void Cancelar()
    {
        if (Estado == EstadoOT.Lista || Estado == EstadoOT.Cancelada)
            return; // idempotent for terminal states
        Estado = EstadoOT.Cancelada;
    }
}
