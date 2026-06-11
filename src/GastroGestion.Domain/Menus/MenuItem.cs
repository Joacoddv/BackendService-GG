using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Menus;

/// <summary>
/// An item in a <see cref="Menu"/> that binds a dish to the menu with an optional price override.
/// When <see cref="PrecioOverride"/> is null, the effective price defaults to the dish's base price.
/// Effective price resolution is the application layer's responsibility
/// (IEfectivoPrecioService, defined in Slice 2).
/// </summary>
public class MenuItem : Entity
{
    /// <summary>Cross-boundary reference to the dish (by Id only).</summary>
    public Guid PlatoId { get; private set; }

    /// <summary>
    /// Optional price override for this menu entry.
    /// Null means "use the Plato's PrecioBase".
    /// </summary>
    public Dinero? PrecioOverride { get; private set; }

    public MenuItem(Guid id, Guid platoId, Dinero? precioOverride = null) : base(id)
    {
        PlatoId       = platoId;
        PrecioOverride = precioOverride;
    }

    // EF Core parameterless ctor.
#pragma warning disable CS8618
    protected MenuItem() { }
#pragma warning restore CS8618
}
