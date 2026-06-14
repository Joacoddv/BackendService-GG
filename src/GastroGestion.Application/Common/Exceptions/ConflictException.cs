namespace GastroGestion.Application.Common.Exceptions;

/// <summary>
/// Signals a business-rule conflict at the application boundary.
/// The API layer should map this to HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
