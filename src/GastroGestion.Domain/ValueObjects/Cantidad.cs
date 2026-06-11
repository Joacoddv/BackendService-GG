using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.ValueObjects;

/// <summary>
/// A positive quantity tied to a specific unit of measure.
/// There is no silent unit conversion: arithmetic between mismatched units throws.
/// </summary>
public sealed class Cantidad : ValueObject
{
    public decimal Valor { get; }
    public UnidadDeMedida Unidad { get; }

    public Cantidad(decimal valor, UnidadDeMedida unidad)
    {
        if (valor <= 0)
            throw new DomainException($"Cantidad.Valor must be greater than zero. Received: {valor}.");
        Valor = valor;
        Unidad = unidad;
    }

    /// <summary>
    /// Returns a new <see cref="Cantidad"/> with the sum of both values.
    /// Units must match; throws <see cref="DomainException"/> otherwise.
    /// </summary>
    public Cantidad Sumar(Cantidad otra)
    {
        GuardarUnidad(otra);
        return new Cantidad(Valor + otra.Valor, Unidad);
    }

    /// <summary>
    /// Returns a new <see cref="Cantidad"/> with the difference.
    /// Units must match; result must be positive.
    /// </summary>
    public Cantidad Restar(Cantidad otra)
    {
        GuardarUnidad(otra);
        var result = Valor - otra.Valor;
        if (result <= 0)
            throw new DomainException(
                $"Restar would produce a non-positive Cantidad ({result} {Unidad}).");
        return new Cantidad(result, Unidad);
    }

    private void GuardarUnidad(Cantidad otra)
    {
        if (Unidad != otra.Unidad)
            throw new DomainException(
                $"Cannot mix units of measure: {Unidad} and {otra.Unidad}. " +
                "Unit conversion must be handled at the application layer.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
        yield return Unidad;
    }

    public override string ToString() => $"{Valor} {Unidad}";
}
