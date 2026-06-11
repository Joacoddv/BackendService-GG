namespace GastroGestion.Domain.Common;

/// <summary>
/// Represents a domain rule violation. Thrown when an aggregate or value object
/// invariant is broken. Callers should not catch this for control flow —
/// it signals a programming error or invalid input that should have been
/// validated at the application boundary.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
