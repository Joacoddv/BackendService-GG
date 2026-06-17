namespace GastroGestion.Application.Common.Exceptions;

/// <summary>
/// Signals an application-layer precondition failure before delegating to the domain.
/// The API layer maps this to HTTP 422 Unprocessable Entity via GastroGestionExceptionHandler.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
