using GastroGestion.Contracts.Platos;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests.Platos;

/// <summary>Integration tests for PUT /platos/{id} and DELETE /platos/{id}.</summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class PlatoCrudEndpointTests
{
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PlatoCrudEndpointTests(ApiFactory factory)
        => _factory = factory;

    private async Task<Guid> CreatePlatoAsync(string nombre, decimal precio = 1000m)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db    = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
        var plato = Plato.Crear(nombre, new Dinero(precio), AlicuotaIVA.General);
        await db.Platos.AddAsync(plato);
        await db.SaveChangesAsync();
        return plato.Id;
    }

    // ── PUT /platos/{id} ──────────────────────────────────────────────────────

    [Fact]
    public async Task PUT_Platos_Admin_ValidRequest_Returns200()
    {
        var id     = await CreatePlatoAsync($"Plato_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarPlatoRequest("Milanesa Napolitana", 1800m);

        var response = await client.PutAsJsonAsync($"/platos/{id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PlatoResponse>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal("Milanesa Napolitana", result!.Nombre);
        Assert.Equal(1800m, result.PrecioBase);
    }

    [Fact]
    public async Task PUT_Platos_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarPlatoRequest("Nombre Nuevo", 500m);

        var response = await client.PutAsJsonAsync($"/platos/{id}", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task PUT_Platos_NonAdmin_Returns403(RolUsuario role)
    {
        var id     = await CreatePlatoAsync($"RoleTest_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(role);
        var body   = new EditarPlatoRequest("Nuevo Nombre", 500m);

        var response = await client.PutAsJsonAsync($"/platos/{id}", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── DELETE /platos/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task DELETE_Platos_Admin_Returns204()
    {
        var id     = await CreatePlatoAsync($"ToDelete_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/platos/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Platos_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/platos/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task DELETE_Platos_NonAdmin_Returns403(RolUsuario role)
    {
        var id     = await CreatePlatoAsync($"DeleteRoleTest_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(role);

        var response = await client.DeleteAsync($"/platos/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
