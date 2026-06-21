using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Auth;

/// <summary>
/// Response body for a successful POST /auth/login and POST /auth/refresh.
/// Carries the access token plus the rotating refresh token and their expiries.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    Guid UsuarioId,
    RolUsuario Rol);
