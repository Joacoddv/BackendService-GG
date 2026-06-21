namespace GastroGestion.Application.Auth.CerrarSesion;

/// <summary>Command for POST /auth/logout — carries the raw refresh token to revoke.</summary>
public sealed record CerrarSesionCommand(string RefreshToken);
