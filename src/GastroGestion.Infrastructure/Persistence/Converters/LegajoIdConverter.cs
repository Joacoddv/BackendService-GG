using GastroGestion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GastroGestion.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="LegajoId"/> as a <c>uniqueidentifier</c> column.
/// Re-validates via <see cref="LegajoId"/> constructor on read (defence in depth).
/// </summary>
public sealed class LegajoIdConverter : ValueConverter<LegajoId, Guid>
{
    public static readonly LegajoIdConverter Instance = new();

    public LegajoIdConverter() : base(
        legajo => legajo.Valor,
        g      => new LegajoId(g))
    { }
}
