using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount in a specific currency.
/// Immutable; all arithmetic returns a new instance.
/// Mixed-currency operations throw <see cref="DomainException"/>.
/// </summary>
public sealed class Dinero : ValueObject
{
    public decimal Monto { get; }
    public Moneda Moneda { get; }

    public Dinero(decimal monto, Moneda moneda = Moneda.ARS)
    {
        if (monto < 0)
            throw new DomainException($"Monto cannot be negative. Received: {monto}.");
        Monto = monto;
        Moneda = moneda;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Monto;
        yield return Moneda;
    }

    /// <summary>Returns a new <see cref="Dinero"/> with the sum of both amounts.</summary>
    public Dinero Sumar(Dinero otro)
    {
        GuardarMoneda(otro);
        return new Dinero(Monto + otro.Monto, Moneda);
    }

    /// <summary>
    /// Returns a new <see cref="Dinero"/> with the difference.
    /// The result is clamped to 0 to satisfy the non-negative invariant;
    /// business rules that need to detect underflow should check before calling.
    /// </summary>
    public Dinero Restar(Dinero otro)
    {
        GuardarMoneda(otro);
        var result = Monto - otro.Monto;
        if (result < 0)
            throw new DomainException(
                $"Restar would produce a negative amount ({result}). " +
                "Check availability before subtracting.");
        return new Dinero(result, Moneda);
    }

    /// <summary>Returns a new <see cref="Dinero"/> multiplied by <paramref name="factor"/>.</summary>
    public Dinero Multiplicar(decimal factor)
    {
        if (factor < 0)
            throw new DomainException($"Multiplication factor cannot be negative. Received: {factor}.");
        return new Dinero(Monto * factor, Moneda);
    }

    /// <summary>
    /// Returns a new <see cref="Dinero"/> representing the IVA amount only
    /// (i.e., <c>Monto * alicuota.Tasa</c>).
    /// </summary>
    public Dinero AplicarIVA(PorcentajeIVA alicuota) =>
        new(Monto * alicuota.Tasa, Moneda);

    /// <summary>
    /// Returns a new <see cref="Dinero"/> representing the gross amount
    /// (i.e., <c>Monto + IVA</c>).
    /// </summary>
    public Dinero ConIVA(PorcentajeIVA alicuota) =>
        new(Monto + Monto * alicuota.Tasa, Moneda);

    private void GuardarMoneda(Dinero otro)
    {
        if (Moneda != otro.Moneda)
            throw new DomainException(
                $"Cannot mix currencies: {Moneda} and {otro.Moneda}.");
    }

    public override string ToString() => $"{Monto:N2} {Moneda}";
}
