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

    /// <summary>Adds a recipe line to this dish.</summary>
    public void AgregarLineaReceta(Guid ingredienteId, Cantidad cantidad)
    {
        ArgumentNullException.ThrowIfNull(cantidad);
        _lineasReceta.Add(new LineaReceta(Guid.NewGuid(), ingredienteId, cantidad));
    }

    /// <summary>Removes a recipe line by Id. No-op if not found.</summary>
    public void EliminarLineaReceta(Guid lineaRecetaId)
    {
        var linea = _lineasReceta.FirstOrDefault(l => l.Id == lineaRecetaId);
        if (linea is not null)
            _lineasReceta.Remove(linea);
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
}
