using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.Clientes;

/// <summary>
/// A physical address owned by a <see cref="Cliente"/>.
/// This is an entity (has identity) — distinct from <see cref="GastroGestion.Domain.ValueObjects.DireccionEntrega"/>,
/// which is a frozen VO snapshot on a Pedido (design §4 dual-nature resolution).
/// </summary>
public class Direccion : Entity
{
    public string Calle { get; private set; }
    public string Numero { get; private set; }
    public string? Piso { get; private set; }
    public string? Departamento { get; private set; }
    public string Ciudad { get; private set; }
    public string Provincia { get; private set; }
    public string CodigoPostal { get; private set; }

    /// <summary>Optional delivery zone or neighborhood descriptor.</summary>
    public string? Zona { get; private set; }

    public Direccion(
        Guid id,
        string calle,
        string numero,
        string ciudad,
        string provincia,
        string codigoPostal,
        string? piso = null,
        string? departamento = null,
        string? zona = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(calle))
            throw new DomainException("Direccion.Calle cannot be empty.");
        if (string.IsNullOrWhiteSpace(ciudad))
            throw new DomainException("Direccion.Ciudad cannot be empty.");
        if (string.IsNullOrWhiteSpace(provincia))
            throw new DomainException("Direccion.Provincia cannot be empty.");
        if (string.IsNullOrWhiteSpace(codigoPostal))
            throw new DomainException("Direccion.CodigoPostal cannot be empty.");

        Calle        = calle;
        // Numero is optional — street numbers like "S/N" are valid in Argentine addresses.
        Numero       = numero;
        Ciudad       = ciudad;
        Provincia    = provincia;
        CodigoPostal = codigoPostal;
        // Piso, Departamento, and Zona are genuinely optional (not all addresses have them).
        Piso         = piso;
        Departamento = departamento;
        Zona         = zona;
    }

    // EF Core parameterless ctor.
#pragma warning disable CS8618
    protected Direccion() { }
#pragma warning restore CS8618
}
