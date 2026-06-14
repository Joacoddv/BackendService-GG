using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GastroGestion.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="PorcentajeIVA"/> as an <c>int</c> representing the
/// <see cref="AlicuotaIVA"/> enum ordinal.
/// The derived decimal <see cref="PorcentajeIVA.Tasa"/> is never stored separately.
/// </summary>
public sealed class PorcentajeIvaConverter : ValueConverter<PorcentajeIVA, int>
{
    public static readonly PorcentajeIvaConverter Instance = new();

    public PorcentajeIvaConverter() : base(
        p => (int)p.Alicuota,
        i => new PorcentajeIVA((AlicuotaIVA)i))
    { }
}
