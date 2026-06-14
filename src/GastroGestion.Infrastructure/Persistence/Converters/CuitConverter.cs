using GastroGestion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GastroGestion.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="Cuit"/> as a raw 11-digit <c>nvarchar(11)</c> string.
/// Re-validates via <see cref="Cuit"/> constructor on read (defence in depth).
/// </summary>
public sealed class CuitConverter : ValueConverter<Cuit, string>
{
    public static readonly CuitConverter Instance = new();

    public CuitConverter() : base(
        cuit => cuit.Valor,
        s    => new Cuit(s))
    { }
}
