using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Auth.Login;

/// <summary>Successful login result returned by LoginHandler.</summary>
public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UsuarioId,
    RolUsuario Rol);
