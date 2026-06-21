using GastroGestion.Contracts.Usuarios;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Usuarios;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GastroGestion.Api.Tests.Usuarios;

/// <summary>
/// Integration tests for GET /usuarios/cocineros (CCC-A01, CCC-T12).
/// Role gate: Cocinero | Administrador → 200; all others → 403; anonymous → 401.
/// Inactive cocinero must not appear in the response.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class UsuarioEndpointTests
{
    private readonly ApiFactory _factory;

    public UsuarioEndpointTests(ApiFactory factory)
        => _factory = factory;

    // ── Role gate tests ───────────────────────────────────────────────────────

    /// <summary>Administrador → 200 OK with array payload.</summary>
    [Fact]
    public async Task GET_Cocineros_Admin_Returns200()
    {
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var response = await client.GetAsync("/usuarios/cocineros");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<CocineroResponse>>();
        Assert.NotNull(body);
    }

    /// <summary>Cocinero → 200 OK with array payload.</summary>
    [Fact]
    public async Task GET_Cocineros_Cocinero_Returns200()
    {
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Cocinero);
        var response = await client.GetAsync("/usuarios/cocineros");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<CocineroResponse>>();
        Assert.NotNull(body);
    }

    /// <summary>Mozo → 403 Forbidden.</summary>
    [Fact]
    public async Task GET_Cocineros_Mozo_Returns403()
    {
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Mozo);
        var response = await client.GetAsync("/usuarios/cocineros");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Cajero → 403 Forbidden.</summary>
    [Fact]
    public async Task GET_Cocineros_Cajero_Returns403()
    {
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Cajero);
        var response = await client.GetAsync("/usuarios/cocineros");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Token without role claim → 403 Forbidden.</summary>
    [Fact]
    public async Task GET_Cocineros_WithoutRole_Returns403()
    {
        var client   = _factory.CreateAuthenticatedClientWithoutRole();
        var response = await client.GetAsync("/usuarios/cocineros");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Token with unparseable role claim → 403 Forbidden.</summary>
    [Fact]
    public async Task GET_Cocineros_BogusRole_Returns403()
    {
        var client   = _factory.CreateAuthenticatedClientWithBogusRole("NotARealRole");
        var response = await client.GetAsync("/usuarios/cocineros");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Anonymous (no Authorization header) → 401 Unauthorized.</summary>
    [Fact]
    public async Task GET_Cocineros_Anonymous_Returns401()
    {
        var client   = _factory.CreateClient();
        var response = await client.GetAsync("/usuarios/cocineros");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Data tests ────────────────────────────────────────────────────────────

    /// <summary>
    /// Active cocinero appears in the response; inactive cocinero does NOT (CCC-A01).
    /// Inserts both directly into the test database using the factory's service scope.
    /// </summary>
    [Fact]
    public async Task GET_Cocineros_InactiveCocineroAbsent()
    {
        // Arrange — insert one active and one inactive cocinero via service scope
        var activeCocineroId   = Guid.NewGuid();
        var inactiveCocineroId = Guid.NewGuid();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeguridadDbContext>();

            var active = Usuario.Crear(
                $"active.cocinero.{activeCocineroId:N}@test.local",
                "Active Cocinero",
                RolUsuario.Cocinero,
                "hash-active");

            var inactive = Usuario.Crear(
                $"inactive.cocinero.{inactiveCocineroId:N}@test.local",
                "Inactive Cocinero",
                RolUsuario.Cocinero,
                "hash-inactive");
            inactive.Desactivar();

            await db.Usuarios.AddRangeAsync(active, inactive);
            await db.SaveChangesAsync();

            // Capture the actual DB ids for assertion
            activeCocineroId   = active.Id;
            inactiveCocineroId = inactive.Id;
        }

        // Act
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var response = await client.GetAsync("/usuarios/cocineros");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<CocineroResponse>>();
        Assert.NotNull(body);

        Assert.Contains(body!,    r => r.Id == activeCocineroId);
        Assert.DoesNotContain(body!, r => r.Id == inactiveCocineroId);
    }
}
