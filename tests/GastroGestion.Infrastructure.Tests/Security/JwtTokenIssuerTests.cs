using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Xunit;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Usuarios;
using GastroGestion.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GastroGestion.Infrastructure.Tests.Security;

/// <summary>
/// Unit tests for JwtTokenIssuer. Covers AUTH-04.5 scenarios A–C.
/// </summary>
public class JwtTokenIssuerTests
{
    private const string Issuer     = "GastroGestion";
    private const string Audience   = "GastroGestion";
    private const string SigningKey = "TestSigningKeyForApiTestsMinimumLength32Chars";

    private readonly JwtTokenIssuer _sut;

    public JwtTokenIssuerTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"]     = Issuer,
                ["Jwt:Audience"]   = Audience,
                ["Jwt:SigningKey"] = SigningKey
            })
            .Build();

        _sut = new JwtTokenIssuer(config);
    }

    private static Usuario MakeUsuario(RolUsuario rol = RolUsuario.Mozo)
        => Usuario.Crear("test@example.com", "Test User", rol, "HASH");

    // AUTH-04-A: token contains expected claims
    [Fact]
    public void Issue_ContainsExpectedClaims()
    {
        var usuario = MakeUsuario(RolUsuario.Mozo);

        var token      = _sut.Issue(usuario);
        var handler    = new JwtSecurityTokenHandler();
        var jwtToken   = handler.ReadJwtToken(token.Value);

        jwtToken.Subject.Should().Be(usuario.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == usuario.Email);
        jwtToken.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == "Mozo");
    }

    // AUTH-04-B: expiry is UtcNow + 8 hours (within 5 seconds tolerance)
    [Fact]
    public void Issue_ExpiryIsApproximatelyEightHoursFromNow()
    {
        var usuario    = MakeUsuario();
        var before     = DateTime.UtcNow;

        var token = _sut.Issue(usuario);

        token.ExpiresAtUtc.Should().BeCloseTo(before.AddHours(8), TimeSpan.FromSeconds(5));
    }

    // AUTH-04-C: issued token validates against matching TokenValidationParameters
    [Fact]
    public void Issue_TokenValidatesAgainstMatchingParameters()
    {
        var usuario  = MakeUsuario();
        var token    = _sut.Issue(usuario);
        var handler  = new JwtSecurityTokenHandler();

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = Issuer,
            ValidAudience            = Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey))
        };

        var act = () => handler.ValidateToken(token.Value, validationParams, out _);
        act.Should().NotThrow("the token issued by JwtTokenIssuer must validate against the same parameters used in Program.cs");
    }
}
