using GastroGestion.Application.Abstractions;
using GastroGestion.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GastroGestion.Api.Security;

/// <summary>
/// Reads the current user's identity from <see cref="IHttpContextAccessor"/>.
/// Registered as scoped — one instance per HTTP request.
/// </summary>
internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;

    public Guid UsuarioId
    {
        get
        {
            // JwtRegisteredClaimNames.Sub → NameIdentifier (mapped by the JWT bearer middleware)
            var sub = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string Email =>
        Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value
        ?? Principal?.FindFirst(ClaimTypes.Email)?.Value
        ?? string.Empty;

    public RolUsuario? Rol
    {
        get
        {
            var rolClaim = Principal?.FindFirst(ClaimTypes.Role)?.Value;
            // No fallback to a concrete role: a missing or unparseable claim means "no role".
            return Enum.TryParse<RolUsuario>(rolClaim, out var rol) ? rol : null;
        }
    }
}
