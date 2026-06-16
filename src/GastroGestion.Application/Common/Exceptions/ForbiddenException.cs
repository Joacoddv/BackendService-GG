namespace GastroGestion.Application.Common.Exceptions;

/// <summary>
/// Signals that the caller's role is not allowed to perform the requested operation.
/// The API layer maps this to HTTP 403 Forbidden via GastroGestionExceptionHandler.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
