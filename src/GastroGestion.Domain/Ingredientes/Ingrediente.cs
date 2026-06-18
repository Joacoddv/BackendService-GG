using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.Ingredientes;

/// <summary>
/// Aggregate root for a raw material / stock item.
/// Name uniqueness is an infrastructure concern (unique index in phase 3).
/// Stock quantity is NOT tracked here — it lives in the MovimientoStock ledger (Slice 3).
/// </summary>
public class Ingrediente : AggregateRoot
{
    public string Nombre { get; private set; }
    public UnidadDeMedida UnidadBase { get; private set; }
    public bool Activo { get; private set; }

#pragma warning disable CS8618
    private Ingrediente() { } // EF Core
#pragma warning restore CS8618

    private Ingrediente(Guid id, string nombre, UnidadDeMedida unidadBase) : base(id)
    {
        Nombre    = nombre;
        UnidadBase = unidadBase;
        Activo    = true;
    }

    /// <summary>Creates a new active <see cref="Ingrediente"/>.</summary>
    public static Ingrediente Crear(string nombre, UnidadDeMedida unidadBase)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Ingrediente.Nombre cannot be null or empty.");

        return new Ingrediente(Guid.NewGuid(), nombre, unidadBase);
    }

    /// <summary>
    /// Updates the ingredient's display name.
    /// UnidadBase is intentionally not a parameter — it is immutable after creation (ADR-CCC-1).
    /// </summary>
    /// <param name="nombre">New name — must be non-empty.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="nombre"/> is null or whitespace.</exception>
    public void ActualizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Ingrediente.Nombre cannot be null or empty.");

        Nombre = nombre;
    }

    /// <summary>Soft-deletes this ingredient. Idempotent.</summary>
    public void Desactivar()
    {
        Activo = false;
    }
}
