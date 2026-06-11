using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.ValueObjects;

/// <summary>
/// Argentine fiscal identification number (CUIT/CUIL).
/// Validates the 11-digit check-digit algorithm at construction time.
/// Formatted as ##-########-#.
/// </summary>
public sealed class Cuit : ValueObject
{
    /// <summary>Raw 11-digit string (no hyphens).</summary>
    public string Valor { get; }

    public Cuit(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("CUIT cannot be null or empty.");

        var digits = valor.Replace("-", "").Trim();

        if (digits.Length != 11 || !digits.All(char.IsDigit))
            throw new DomainException($"CUIT must be exactly 11 digits. Received: '{valor}'.");

        if (!IsValidCheckDigit(digits))
            throw new DomainException($"CUIT check digit is invalid for: '{valor}'.");

        Valor = digits;
    }

    private static bool IsValidCheckDigit(string digits)
    {
        // CUIT/CUIL check-digit algorithm (Argentina AFIP standard)
        int[] weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        int sum = 0;
        for (int i = 0; i < 10; i++)
            sum += (digits[i] - '0') * weights[i];

        int remainder = sum % 11;
        int expected = remainder == 0 ? 0 : 11 - remainder;

        // AFIP rule: if expected == 10, the CUIT is invalid (no valid check digit exists)
        if (expected == 10) return false;

        return (digits[10] - '0') == expected;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    /// <summary>Returns the CUIT formatted as ##-########-#.</summary>
    public override string ToString() =>
        $"{Valor[..2]}-{Valor[2..10]}-{Valor[10]}";
}
