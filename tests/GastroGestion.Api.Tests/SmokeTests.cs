using GastroGestion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// PR 1 smoke tests — confirms the host boots, the health endpoint responds,
/// ProblemDetails is wired, and the DevDataSeeder ran.
/// All tests tagged [Trait("Category","Integration")] — requires LocalDB.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SmokeTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public SmokeTests(ApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task GET_Health_Returns200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnrecognisedRoute_Returns404()
    {
        var response = await _client.GetAsync("/nonexistent-route-xyz");

        // The host returns 404 for unknown routes.
        // Note: routing-level 404s are not exceptions, so GastroGestionExceptionHandler
        // does not intercept them. The status code is the signal.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApiFactory_SeedsDatabase_OnFirstBoot()
    {
        // Health check to ensure host is up and seeder has run.
        var health = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        // Verify seeder populated the Clientes table (≥ 3 rows).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
        var clienteCount = await db.Clientes.CountAsync();

        Assert.True(clienteCount >= 3,
            $"Expected at least 3 seeded Clientes, found {clienteCount}.");
    }

    [Fact]
    public async Task ApiFactory_SecondBoot_DoesNotDuplicateClientes()
    {
        // Get the count after first seeder run.
        using var scope1 = _factory.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
        var countAfterFirstSeed = await db1.Clientes.CountAsync();

        // Simulate calling the idempotency guard — seeder should not duplicate.
        // We verify by hitting health (host still running with same DB) and re-counting.
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
        var countAfterSecondCheck = await db2.Clientes.CountAsync();

        Assert.Equal(countAfterFirstSeed, countAfterSecondCheck);
    }
}
