using GastroGestion.Domain.Enums;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// Test application factory. Boots the full Minimal API host with:
/// - Environment = "Development" (triggers auto-migrate + DevDataSeeder).
/// - Dedicated LocalDB test database (isolated from the dev database).
/// - Inline JWT signing key that satisfies the startup guard.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=GastroGestion_ApiTests;Trusted_Connection=True;TrustServerCertificate=True";

    // Must be ≥32 chars to satisfy the startup guard minimum-length expectation.
    private const string TestJwtSigningKey =
        "TestSigningKeyForApiTestsMinimumLength32Chars";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:GastroGestion", TestConnectionString);
        builder.UseSetting("Jwt:SigningKey",  TestJwtSigningKey);
        builder.UseSetting("Jwt:Issuer",      "GastroGestion");
        builder.UseSetting("Jwt:Audience",    "GastroGestion");
    }

    public async Task InitializeAsync()
    {
        // Migrate + seed happen inside the host startup (Program.cs dev block).
        // We just need to ensure the host has started before tests run.
        _ = Server; // forces lazy host startup
        await Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        // Drop the test database so each test run starts fresh.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
        await db.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Issues a signed JWT using the same signing key, issuer, and audience injected at factory
    /// startup. The token passes the test host's TokenValidationParameters.
    /// </summary>
    public string GenerateTestToken(RolUsuario role)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:   "GastroGestion",
            audience: "GastroGestion",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            },
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> pre-configured with a Bearer token for the given role.
    /// Defaults to <see cref="RolUsuario.Administrador"/> when no role is specified.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(RolUsuario role = RolUsuario.Administrador)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestToken(role));
        return client;
    }
}
