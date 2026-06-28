using GastroGestion.Contracts.Menus;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Menus;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests.Menus;

/// <summary>Integration tests for PUT /menus/{id} and DELETE /menus/{id}.</summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class MenuCrudEndpointTests
{
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public MenuCrudEndpointTests(ApiFactory factory)
        => _factory = factory;

    private static DateOnly FutureDate(int days = 7)
        => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(days);

    private async Task<Guid> CreateMenuAsync(string nombre)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db   = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
        var menu = Menu.Crear(nombre, FutureDate());
        await db.Menus.AddAsync(menu);
        await db.SaveChangesAsync();
        return menu.Id;
    }

    // ── PUT /menus/{id} ───────────────────────────────────────────────────────

    [Fact]
    public async Task PUT_Menus_Admin_ValidRequest_Returns200()
    {
        var id       = await CreateMenuAsync($"Menu_{Guid.NewGuid():N}");
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var newFecha = FutureDate(14);
        var body     = new EditarMenuRequest("Menu Especial", newFecha);

        var response = await client.PutAsJsonAsync($"/menus/{id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MenuResponse>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal("Menu Especial", result!.Nombre);
        Assert.Equal(newFecha, result.FechaVigencia);
    }

    [Fact]
    public async Task PUT_Menus_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarMenuRequest("Nombre Nuevo", FutureDate());

        var response = await client.PutAsJsonAsync($"/menus/{id}", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task PUT_Menus_NonAdmin_Returns403(RolUsuario role)
    {
        var id     = await CreateMenuAsync($"RoleTest_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(role);
        var body   = new EditarMenuRequest("Nuevo Nombre", FutureDate());

        var response = await client.PutAsJsonAsync($"/menus/{id}", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── DELETE /menus/{id} ────────────────────────────────────────────────────

    [Fact]
    public async Task DELETE_Menus_Admin_Returns204()
    {
        var id     = await CreateMenuAsync($"ToDelete_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/menus/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Menus_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/menus/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task DELETE_Menus_NonAdmin_Returns403(RolUsuario role)
    {
        var id     = await CreateMenuAsync($"DeleteRoleTest_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(role);

        var response = await client.DeleteAsync($"/menus/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
