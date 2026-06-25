using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Auth.Login;

/// <summary>Successful login result returned by LoginHandler.</summary>
public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    Guid UsuarioId,
    RolUsuario Rol,
    string Email);
