using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GastroGestion.Application.Abstractions.Security;
using GastroGestion.Domain.Usuarios;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GastroGestion.Infrastructure.Security;

/// <summary>
/// Infrastructure implementation of ITokenIssuer using JwtSecurityTokenHandler (NOT JsonWebTokenHandler —
/// see ADR-4: JsonWebTokenHandler parses claims differently and risks validation mismatch).
/// Reads Jwt:Issuer, Jwt:Audience, and Jwt:SigningKey from IConfiguration — the SAME keys used by
/// the existing TokenValidationParameters in Program.cs (lines 59-62). Token expiry is 8 hours (ADR-5).
/// </summary>
internal sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly IConfiguration _config;

    public JwtTokenIssuer(IConfiguration config) => _config = config;

    public AccessToken Issue(Usuario usuario)
    {
        var issuer     = _config["Jwt:Issuer"]     ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience   = _config["Jwt:Audience"]   ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var signingKey = _config["Jwt:SigningKey"]  ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = DateTime.UtcNow.AddHours(8);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            // ClaimTypes.Role so the existing bearer middleware maps it to the claims principal role
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            expiresAtUtc,
            signingCredentials: creds);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
