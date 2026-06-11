using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.ValueObjects;

/// <summary>
/// Wraps an <see cref="AlicuotaIVA"/> enum value and exposes the corresponding
/// decimal rate. The set is closed: {0, 0.105, 0.21, 0.27}.
/// </summary>
public sealed class PorcentajeIVA : ValueObject
{
    private static readonly Dictionary<AlicuotaIVA, decimal> Tasas = new()
    {
        { AlicuotaIVA.Exento,      0m     },
        { AlicuotaIVA.ReducidoA,   0.105m },
        { AlicuotaIVA.General,     0.21m  },
        { AlicuotaIVA.Diferencial, 0.27m  }
    };

    public AlicuotaIVA Alicuota { get; }

    /// <summary>Decimal rate: 0 / 0.105 / 0.21 / 0.27.</summary>
    public decimal Tasa => Tasas[Alicuota];

    public PorcentajeIVA(AlicuotaIVA alicuota)
    {
        if (!Tasas.ContainsKey(alicuota))
            throw new DomainException($"AlicuotaIVA value '{alicuota}' is not in the closed set.");
        Alicuota = alicuota;
    }

    /// <summary>Returns a <see cref="PorcentajeIVA"/> with zero rate (Exento).</summary>
    public static PorcentajeIVA Cero => new(AlicuotaIVA.Exento);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Alicuota;
    }

    public override string ToString() => $"{Tasa:P1}";
}
