using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Clientes;

/// <summary>
/// Aggregate root for a customer. Owns a collection of addresses.
/// CUIT uniqueness across clients is an infrastructure concern (unique index in phase 3).
/// </summary>
public class Cliente : AggregateRoot
{
    private readonly List<Direccion> _direcciones = [];

    public string Nombre { get; private set; }
    public CondicionIVA CondicionIVA { get; private set; }
    public Cuit? Cuit { get; private set; }
    public Email? Email { get; private set; }

    /// <summary>
    /// Immutable surrogate client number. Assigned at creation; never editable.
    /// Uniqueness is enforced at the infrastructure layer.
    /// </summary>
    public Guid NumeroCliente { get; }

    public bool Activo { get; private set; }

    public IReadOnlyList<Direccion> Direcciones => _direcciones.AsReadOnly();

#pragma warning disable CS8618
    private Cliente() { } // EF Core
#pragma warning restore CS8618

    private Cliente(
        Guid id,
        string nombre,
        CondicionIVA condicionIVA,
        Cuit? cuit,
        Email? email) : base(id)
    {
        Nombre       = nombre;
        CondicionIVA = condicionIVA;
        Cuit         = cuit;
        Email        = email;
        NumeroCliente = id; // v1: surrogate = same as aggregate Id
        Activo       = true;
    }

    /// <summary>
    /// Creates a new active <see cref="Cliente"/>.
    /// </summary>
    /// <param name="nombre">Required display name.</param>
    /// <param name="condicionIVA">Fiscal condition — drives comprobante type.</param>
    /// <param name="cuit">Required when <paramref name="condicionIVA"/> is <see cref="CondicionIVA.ResponsableInscripto"/>.</param>
    /// <param name="email">Optional contact email.</param>
    public static Cliente Crear(
        string nombre,
        CondicionIVA condicionIVA,
        Cuit? cuit,
        Email? email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Cliente.Nombre cannot be null or empty.");

        if (condicionIVA == CondicionIVA.ResponsableInscripto && cuit is null)
            throw new DomainException(
                "CUIT is required for a ResponsableInscripto client.");

        return new Cliente(Guid.NewGuid(), nombre, condicionIVA, cuit, email);
    }

    /// <summary>
    /// Adds a delivery address to this client's address book.
    /// </summary>
    public void AgregarDireccion(Direccion direccion)
    {
        ArgumentNullException.ThrowIfNull(direccion);
        _direcciones.Add(direccion);
    }

    /// <summary>
    /// Removes an address by its Id.
    /// </summary>
    public void EliminarDireccion(Guid direccionId)
    {
        var direccion = _direcciones.FirstOrDefault(d => d.Id == direccionId);
        if (direccion is not null)
            _direcciones.Remove(direccion);
    }

    /// <summary>
    /// Updates mutable profile data. NumeroCliente and Activo are intentionally left unchanged.
    /// Enforces the same RI-requires-CUIT invariant as <see cref="Crear"/>.
    /// </summary>
    /// <param name="nombre">Required display name — must not be null or whitespace.</param>
    /// <param name="condicionIVA">New fiscal condition.</param>
    /// <param name="cuit">Required when <paramref name="condicionIVA"/> is <see cref="CondicionIVA.ResponsableInscripto"/>.</param>
    /// <param name="email">Optional contact email.</param>
    public void ActualizarDatos(string nombre, CondicionIVA condicionIVA, Cuit? cuit, Email? email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Cliente.Nombre cannot be null or empty.");

        if (condicionIVA == CondicionIVA.ResponsableInscripto && cuit is null)
            throw new DomainException("CUIT is required for a ResponsableInscripto client.");

        Nombre       = nombre;
        CondicionIVA = condicionIVA;
        Cuit         = cuit;
        Email        = email;
    }

    /// <summary>
    /// Soft-deletes this client. Idempotent: calling on an already-inactive client
    /// is a no-op (does not throw).
    /// </summary>
    public void Desactivar()
    {
        Activo = false;
    }
}
