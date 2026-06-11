using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.ValueObjects;

/// <summary>
/// Validated and normalized email address value object.
/// Format validation: must contain '@' with non-empty local and domain parts.
/// Stored lowercase.
/// </summary>
public sealed class Email : ValueObject
{
    public string Valor { get; }

    public Email(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("Email cannot be null or empty.");

        var normalized = valor.Trim().ToLowerInvariant();

        // Exactly one '@' is required; multiple '@' signs are invalid.
        if (normalized.Count(c => c == '@') != 1)
            throw new DomainException($"Email format is invalid: '{valor}'.");

        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 0 || atIndex == normalized.Length - 1)
            throw new DomainException($"Email format is invalid: '{valor}'.");

        var domain = normalized[(atIndex + 1)..];
        if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.'))
            throw new DomainException($"Email domain part is invalid: '{valor}'.");

        Valor = normalized;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    public override string ToString() => Valor;
}
