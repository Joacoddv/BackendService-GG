namespace GastroGestion.Application.Common.Exceptions;

/// <summary>
/// Signals a generic authentication failure (unknown email, wrong password, or inactive account).
/// Deliberately non-specific so callers cannot distinguish between the three failure types (AUTH-03-E).
/// The API exception handler maps this to HTTP 401 Unauthorized.
/// </summary>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message) { }
}
