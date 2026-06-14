using GastroGestion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
}
