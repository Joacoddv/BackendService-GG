using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.ValueObjects;

/// <summary>
/// Identifies an employee by their internal file number (legajo).
/// Used in audit log entries and role-gated domain operations.
/// </summary>
public sealed class LegajoId : ValueObject
{
    public Guid Valor { get; }

    public LegajoId(Guid valor)
    {
        if (valor == Guid.Empty)
            throw new DomainException("LegajoId cannot be empty.");
        Valor = valor;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    public override string ToString() => Valor.ToString();
}
