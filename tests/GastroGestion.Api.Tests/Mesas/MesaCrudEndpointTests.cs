using GastroGestion.Contracts.Mesas;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Mesas;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests.Mesas;

/// <summary>Integration tests for PUT /mesas/{id} and DELETE /mesas/{id}.</summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class MesaCrudEndpointTests
{
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // Numero has a UNIQUE DB index; allocate distinct numbers in a high range
    // to avoid clashing with seeds (1-4) or other tests.
    private static int _numeroSeq = 100_000;
    private static int NextNumero() => Interlocked.Increment(ref _numeroSeq);

    public MesaCrudEndpointTests(ApiFactory factory)
        => _factory = factory;

    private async Task<Guid> CreateMesaAsync(int numero, int capacidad = 4)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db   = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
        var mesa = Mesa.Crear(numero, capacidad);
        await db.Mesas.AddAsync(mesa);
        await db.SaveChangesAsync();
        return mesa.Id;
    }

    // ── PUT /mesas/{id} ───────────────────────────────────────────────────────

    [Fact]
    public async Task PUT_Mesas_Admin_ValidRequest_Returns200()
    {
        var id        = await CreateMesaAsync(NextNumero());
        var client    = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var newNumero = NextNumero();
        var body      = new EditarMesaRequest(newNumero, 8);

        var response = await client.PutAsJsonAsync($"/mesas/{id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MesaResponse>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal(newNumero, result!.Numero);
        Assert.Equal(8, result.Capacidad);
    }

    [Fact]
    public async Task PUT_Mesas_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarMesaRequest(NextNumero(), 4);

        var response = await client.PutAsJsonAsync($"/mesas/{id}", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PUT_Mesas_Admin_NumeroConflict_Returns409()
    {
        var existingNumero = NextNumero();
        await CreateMesaAsync(existingNumero);
        var id2 = await CreateMesaAsync(NextNumero());

        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        // Try to rename second mesa to the first mesa's Numero
        var body = new EditarMesaRequest(existingNumero, 4);

        var response = await client.PutAsJsonAsync($"/mesas/{id2}", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task PUT_Mesas_NonAdmin_Returns403(RolUsuario role)
    {
        var id     = await CreateMesaAsync(NextNumero());
        var client = _factory.CreateAuthenticatedClient(role);
        var body   = new EditarMesaRequest(NextNumero(), 4);

        var response = await client.PutAsJsonAsync($"/mesas/{id}", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── DELETE /mesas/{id} ────────────────────────────────────────────────────

    [Fact]
    public async Task DELETE_Mesas_Admin_Returns204()
    {
        var id     = await CreateMesaAsync(NextNumero());
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/mesas/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Mesas_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/mesas/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task DELETE_Mesas_NonAdmin_Returns403(RolUsuario role)
    {
        var id     = await CreateMesaAsync(NextNumero());
        var client = _factory.CreateAuthenticatedClient(role);

        var response = await client.DeleteAsync($"/mesas/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── PUT /mesas/{id}/posicion ──────────────────────────────────────────────

    [Fact]
    public async Task PUT_Mesas_Posicion_Admin_Returns200WithCoordinates()
    {
        var id     = await CreateMesaAsync(NextNumero());
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new UbicarMesaRequest(X: 150, Y: 300);

        var response = await client.PutAsJsonAsync($"/mesas/{id}/posicion", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MesaResponse>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal(150, result!.PosicionX);
        Assert.Equal(300, result.PosicionY);
    }

    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task PUT_Mesas_Posicion_NonAdmin_Returns403(RolUsuario role)
    {
        var id     = await CreateMesaAsync(NextNumero());
        var client = _factory.CreateAuthenticatedClient(role);
        var body   = new UbicarMesaRequest(X: 50, Y: 50);

        var response = await client.PutAsJsonAsync($"/mesas/{id}/posicion", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
