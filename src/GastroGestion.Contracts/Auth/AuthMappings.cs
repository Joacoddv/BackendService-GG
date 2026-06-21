using GastroGestion.Application.Auth.CerrarSesion;
using GastroGestion.Application.Auth.Login;
using GastroGestion.Application.Auth.RefrescarToken;

namespace GastroGestion.Contracts.Auth;

/// <summary>Mapping extensions between Auth contracts and Application types.</summary>
public static class AuthMappings
{
    public static LoginCommand ToCommand(this LoginRequest request)
        => new(request.Email, request.Password);

    public static RefrescarTokenCommand ToCommand(this RefrescarTokenRequest request)
        => new(request.RefreshToken);

    public static CerrarSesionCommand ToCommand(this CerrarSesionRequest request)
        => new(request.RefreshToken);

    public static LoginResponse ToResponse(this LoginResult result)
        => new(
            result.AccessToken,
            result.ExpiresAtUtc,
            result.RefreshToken,
            result.RefreshTokenExpiresAtUtc,
            result.UsuarioId,
            result.Rol);
}
