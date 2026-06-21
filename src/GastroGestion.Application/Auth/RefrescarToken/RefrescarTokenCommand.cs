namespace GastroGestion.Application.Auth.RefrescarToken;

/// <summary>Command for POST /auth/refresh — carries the raw refresh token to exchange.</summary>
public sealed record RefrescarTokenCommand(string RefreshToken);
