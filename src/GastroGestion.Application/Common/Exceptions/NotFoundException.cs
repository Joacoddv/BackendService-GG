namespace GastroGestion.Application.Common.Exceptions;

/// <summary>
/// Signals that a required aggregate or entity was not found.
/// Used by write-path handlers when a target aggregate is missing.
/// The API layer maps this to HTTP 404 Not Found via GastroGestionExceptionHandler.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
