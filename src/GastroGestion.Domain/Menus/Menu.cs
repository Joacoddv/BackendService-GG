using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Menus;

/// <summary>
/// Aggregate root for a daily menu. Binds a set of dishes to a future date
/// and optionally overrides their prices.
/// <para>
/// Rule: <see cref="FechaVigencia"/> must be in the future at the moment of creation.
/// This keeps the guard simple and testable; past-date enforcement is domain-level.
/// </para>
/// </summary>
public class Menu : AggregateRoot
{
    private readonly List<MenuItem> _items = [];

    public string Nombre { get; private set; }
    public DateOnly FechaVigencia { get; private set; }
    public bool Activo { get; private set; }

    public IReadOnlyList<MenuItem> Items => _items.AsReadOnly();

#pragma warning disable CS8618
    private Menu() { } // EF Core
#pragma warning restore CS8618

    private Menu(Guid id, string nombre, DateOnly fechaVigencia) : base(id)
    {
        Nombre       = nombre;
        FechaVigencia = fechaVigencia;
        Activo       = true;
    }

    /// <summary>
    /// Creates a new active <see cref="Menu"/>.
    /// </summary>
    /// <param name="nombre">Display name for the menu.</param>
    /// <param name="fechaVigencia">
    /// Must be strictly in the future (after today in UTC).
    /// </param>
    public static Menu Crear(string nombre, DateOnly fechaVigencia)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Menu.Nombre cannot be null or empty.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (fechaVigencia <= today)
            throw new DomainException("FechaVigencia must be a future date.");

        return new Menu(Guid.NewGuid(), nombre, fechaVigencia);
    }

    /// <summary>
    /// Adds a dish to this menu with an optional price override.
    /// </summary>
    /// <param name="platoId">Reference to the dish.</param>
    /// <param name="precioOverride">
    /// If provided, must be non-negative (enforced by <see cref="Dinero"/> ctor).
    /// If null, the dish's PrecioBase is used at order time.
    /// </param>
    public void AgregarItem(Guid platoId, Dinero? precioOverride = null)
    {
        // precioOverride.Monto >= 0 is already enforced by Dinero ctor when non-null.
        _items.Add(new MenuItem(Guid.NewGuid(), platoId, precioOverride));
    }

    /// <summary>Removes a menu item by Id. No-op if not found.</summary>
    public void EliminarItem(Guid menuItemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == menuItemId);
        if (item is not null)
            _items.Remove(item);
    }

    /// <summary>Renames this menu. Validates the new name the same way <see cref="Crear"/> does.</summary>
    public void Renombrar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Menu.Nombre cannot be null or empty.");

        Nombre = nombre;
    }

    /// <summary>
    /// Changes the effective date. Validates it the same way <see cref="Crear"/> does:
    /// the date must be strictly in the future (after today in UTC).
    /// </summary>
    public void CambiarFechaVigencia(DateOnly fecha)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (fecha <= today)
            throw new DomainException("FechaVigencia must be a future date.");

        FechaVigencia = fecha;
    }

    /// <summary>Soft-deletes this menu. Idempotent.</summary>
    public void Desactivar()
    {
        Activo = false;
    }
}
