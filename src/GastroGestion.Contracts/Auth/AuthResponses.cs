using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Auth;

/// <summary>Response body for a successful POST /auth/login.</summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UsuarioId,
    RolUsuario Rol);
