using GastroGestion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GastroGestion.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="Email"/> as a normalised lowercase <c>nvarchar(320)</c> string.
/// Re-validates via <see cref="Email"/> constructor on read (defence in depth).
/// </summary>
public sealed class EmailConverter : ValueConverter<Email, string>
{
    public static readonly EmailConverter Instance = new();

    public EmailConverter() : base(
        email => email.Valor,
        s     => new Email(s))
    { }
}
