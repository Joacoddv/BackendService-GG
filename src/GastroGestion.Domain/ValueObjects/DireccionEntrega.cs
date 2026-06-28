using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.ValueObjects;

/// <summary>
/// Frozen snapshot of a delivery address captured at order creation.
/// Distinct from <see cref="GastroGestion.Domain.Clientes.Direccion"/> (which is an
/// entity owned by Cliente with identity). DireccionEntrega has no identity — it is
/// a pure value object. Once set on a Pedido it is immutable (design §4).
/// </summary>
public sealed class DireccionEntrega : ValueObject
{
    public string Calle { get; }
    public string Numero { get; }
    public string? Piso { get; }
    public string? Departamento { get; }
    public string Ciudad { get; }
    public string Provincia { get; }
    public string CodigoPostal { get; }

    /// <summary>Optional delivery zone or neighborhood descriptor. Part of equality comparison.</summary>
    public string? Zona { get; }

    public DireccionEntrega(
        string calle,
        string numero,
        string ciudad,
        string provincia,
        string codigoPostal,
        string? piso = null,
        string? departamento = null,
        string? zona = null)
    {
        if (string.IsNullOrWhiteSpace(calle))
            throw new DomainException("DireccionEntrega.Calle cannot be empty.");
        if (string.IsNullOrWhiteSpace(numero))
            throw new DomainException("DireccionEntrega.Numero cannot be empty. Use \"S/N\" when there is no street number.");
        if (string.IsNullOrWhiteSpace(ciudad))
            throw new DomainException("DireccionEntrega.Ciudad cannot be empty.");
        if (string.IsNullOrWhiteSpace(provincia))
            throw new DomainException("DireccionEntrega.Provincia cannot be empty.");
        if (string.IsNullOrWhiteSpace(codigoPostal))
            throw new DomainException("DireccionEntrega.CodigoPostal cannot be empty.");

        Calle        = calle;
        Numero       = numero;
        Ciudad       = ciudad;
        Provincia    = provincia;
        CodigoPostal = codigoPostal;
        Piso         = piso;
        Departamento = departamento;
        Zona         = zona;
    }

    // EF Core owned-entity materialisation.
#pragma warning disable CS8618
    private DireccionEntrega() { }
#pragma warning restore CS8618

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Calle;
        yield return Numero;
        yield return Piso;
        yield return Departamento;
        yield return Ciudad;
        yield return Provincia;
        yield return CodigoPostal;
        yield return Zona;
    }

    public override string ToString() =>
        $"{Calle} {Numero}{(Piso is null ? "" : $", Piso {Piso}")}{(Departamento is null ? "" : $" Depto {Departamento}")}, {Ciudad}, {Provincia} {CodigoPostal}";
}
