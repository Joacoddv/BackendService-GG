using GastroGestion.Application.Auth.Login;

namespace GastroGestion.Contracts.Auth;

/// <summary>Mapping extensions between Auth contracts and Application types.</summary>
public static class AuthMappings
{
    public static LoginCommand ToCommand(this LoginRequest request)
        => new(request.Email, request.Password);

    public static LoginResponse ToResponse(this LoginResult result)
        => new(result.AccessToken, result.ExpiresAtUtc, result.UsuarioId, result.Rol);
}
