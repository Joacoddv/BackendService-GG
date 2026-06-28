using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Platos;

/// <summary>
/// Aggregate root for a sellable dish.
/// Owns its recipe lines (<see cref="LineaReceta"/>).
/// Price is snapshotted onto order lines at the time an order is taken —
/// changing <see cref="PrecioBase"/> here does not affect existing orders.
/// Unit-compatibility between recipe lines and their ingredient units is
/// validated at the application layer (domain cannot load Ingrediente here).
/// </summary>
public class Plato : AggregateRoot
{
    private readonly List<LineaReceta> _lineasReceta = [];

    public string Nombre { get; private set; }
    public Dinero PrecioBase { get; private set; }
    public AlicuotaIVA AlicuotaIVA { get; private set; }
    public bool Activo { get; private set; }

    public IReadOnlyList<LineaReceta> LineasReceta => _lineasReceta.AsReadOnly();

#pragma warning disable CS8618
    private Plato() { } // EF Core
#pragma warning restore CS8618

    private Plato(Guid id, string nombre, Dinero precioBase, AlicuotaIVA alicuotaIVA) : base(id)
    {
        Nombre      = nombre;
        PrecioBase  = precioBase;
        AlicuotaIVA = alicuotaIVA;
        Activo      = true;
    }

    /// <summary>Creates a new active <see cref="Plato"/>.</summary>
    public static Plato Crear(string nombre, Dinero precioBase, AlicuotaIVA alicuotaIVA)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Plato.Nombre cannot be null or empty.");

        ArgumentNullException.ThrowIfNull(precioBase);
        // precioBase.Monto >= 0 is already enforced by Dinero ctor.

        return new Plato(Guid.NewGuid(), nombre, precioBase, alicuotaIVA);
    }

    /// <summary>
    /// Adds a recipe line to this dish. The recipe line quantity MUST be expressed in the
    /// ingredient's base unit (<paramref name="unidadBaseIngrediente"/>); no unit conversion is
    /// performed when computing producible quantities, so a mismatched unit is rejected here.
    /// </summary>
    public void AgregarLineaReceta(Guid ingredienteId, UnidadDeMedida unidadBaseIngrediente, Cantidad cantidad)
    {
        ArgumentNullException.ThrowIfNull(cantidad);

        if (cantidad.Unidad != unidadBaseIngrediente)
            throw new DomainException(
                $"Recipe line unit ({cantidad.Unidad}) must match the ingredient base unit " +
                $"({unidadBaseIngrediente}).");

        _lineasReceta.Add(new LineaReceta(Guid.NewGuid(), ingredienteId, cantidad));
    }

    /// <summary>Removes a recipe line by Id. No-op if not found.</summary>
    public void EliminarLineaReceta(Guid lineaRecetaId)
    {
        var linea = _lineasReceta.FirstOrDefault(l => l.Id == lineaRecetaId);
        if (linea is not null)
            _lineasReceta.Remove(linea);
    }

    /// <summary>Renames this dish. Validates the new name the same way <see cref="Crear"/> does.</summary>
    public void Renombrar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Plato.Nombre cannot be null or empty.");

        Nombre = nombre;
    }

    /// <summary>Replaces the base price. Does not affect existing snapshotted prices on orders.</summary>
    public void ActualizarPrecio(Dinero nuevoPrecio)
    {
        ArgumentNullException.ThrowIfNull(nuevoPrecio);
        PrecioBase = nuevoPrecio;
    }

    /// <summary>Soft-deletes this dish. Idempotent.</summary>
    public void Desactivar()
    {
        Activo = false;
    }

    /// <summary>
    /// Maximum whole units of this dish producible from the given on-hand ingredient
    /// balances (already net of reservations). Pure: min over recipe lines of
    /// floor(available / required). Returns 0 if any required ingredient is missing
    /// or insufficient. Returns 0 if the dish has no recipe lines.
    /// </summary>
    /// <param name="balancesPorIngrediente">
    /// Net balances keyed by IngredienteId (SUM of all ledger movements, reservations already negative).
    /// </param>
    public int CalcularMaxProducible(IReadOnlyDictionary<Guid, decimal> balancesPorIngrediente)
    {
        if (_lineasReceta.Count == 0)
            return 0;

        int? min = null;

        foreach (var linea in _lineasReceta)
        {
            // Sub-recipe seam: if PlatoReferenciadoId is non-null we cannot compute
            // producible from ingredient balances alone — return 0 for safety.
            if (linea.PlatoReferenciadoId is not null)
                return 0;

            var required = linea.Cantidad.Valor;

            // Non-positive recipe quantity is invalid data; skip so it never inflates the result.
            if (required <= 0m)
                continue;

            var available = balancesPorIngrediente.GetValueOrDefault(linea.IngredienteId, 0m);
            var ratio     = Math.Floor(available / required);

            // Clamp the decimal ratio before casting: a very large ratio would overflow int
            // (decimal→int throws OverflowException), and a negative one means over-committed stock.
            int producible =
                ratio > int.MaxValue ? int.MaxValue :
                ratio < 0m           ? 0 :
                (int)ratio;

            min = min.HasValue ? Math.Min(min.Value, producible) : producible;
        }

        // If every line was skipped (all had non-positive required), treat as 0 — no constraint
        // could be computed, so we cannot claim anything is producible.
        return min ?? 0;
    }
}
