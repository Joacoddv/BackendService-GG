using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Proveedores;

/// <summary>
/// Aggregate root for a supplier (proveedor). CUIT uniqueness across suppliers is an
/// infrastructure concern (filtered unique index).
/// </summary>
public class Proveedor : AggregateRoot
{
    public string Nombre { get; private set; }
    public Cuit? Cuit { get; private set; }
    public Email? Email { get; private set; }
    public string? Telefono { get; private set; }
    public bool Activo { get; private set; }

#pragma warning disable CS8618
    private Proveedor() { } // EF Core
#pragma warning restore CS8618

    private Proveedor(Guid id, string nombre, Cuit? cuit, Email? email, string? telefono) : base(id)
    {
        Nombre   = nombre;
        Cuit     = cuit;
        Email    = email;
        Telefono = telefono;
        Activo   = true;
    }

    /// <summary>Creates a new active supplier. Only Nombre is required.</summary>
    public static Proveedor Crear(string nombre, Cuit? cuit, Email? email, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Proveedor.Nombre cannot be null or empty.");

        return new Proveedor(Guid.NewGuid(), nombre, cuit, email, NormalizarTelefono(telefono));
    }

    /// <summary>Updates mutable profile data. Activo is left unchanged.</summary>
    public void ActualizarDatos(string nombre, Cuit? cuit, Email? email, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Proveedor.Nombre cannot be null or empty.");

        Nombre   = nombre;
        Cuit     = cuit;
        Email    = email;
        Telefono = NormalizarTelefono(telefono);
    }

    /// <summary>Soft-deletes this supplier. Idempotent.</summary>
    public void Desactivar() => Activo = false;

    private static string? NormalizarTelefono(string? telefono)
        => string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
}
