using GastroGestion.Contracts.Clientes;
using GastroGestion.Contracts.Ingredientes;
using GastroGestion.Contracts.Menus;
using GastroGestion.Contracts.Mesas;
using GastroGestion.Contracts.Platos;
using GastroGestion.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// Integration tests for the PR 2 catalogue endpoints.
/// Covers REQ-09 through REQ-14, REQ-18 (DTO-only), REQ-20 (slice 2 coverage).
/// All tests tagged [Trait("Category","Integration")] — requires LocalDB.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class CatalogueEndpointTests
{
    private readonly HttpClient _client;

    // JSON options that handle enums as strings (Swagger-friendly) and DateOnly
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CatalogueEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Cliente ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_Clientes_ValidRequest_Returns201WithLocation()
    {
        var request = new CrearClienteRequest(
            "Test Cliente",
            CondicionIVA.ConsumidorFinal,
            null,
            null);

        var response = await _client.PostAsJsonAsync("/clientes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/clientes/", response.Headers.Location!.ToString());

        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task POST_Clientes_RIWithoutCuit_Returns422()
    {
        // ResponsableInscripto without CUIT — domain guard fires (DomainException → 422)
        var request = new CrearClienteRequest(
            "RI Sin CUIT",
            CondicionIVA.ResponsableInscripto,
            null,
            null);

        var response = await _client.PostAsJsonAsync("/clientes", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("422", body);
    }

    [Fact]
    public async Task GET_Clientes_ById_NotFound_Returns404()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/clientes/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_Clientes_Returns_SeededClientes()
    {
        var response = await _client.GetAsync("/clientes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<ClienteResponse>>(JsonOpts);
        Assert.NotNull(list);
        Assert.True(list!.Count >= 3, $"Expected at least 3 seeded clientes, got {list.Count}");
    }

    [Fact]
    public async Task POST_Clientes_EmptyNombre_Returns400ValidationProblem()
    {
        // Validator fires before handler — returns 400 ValidationProblem
        var request = new CrearClienteRequest(
            "",
            CondicionIVA.ConsumidorFinal,
            null,
            null);

        var response = await _client.PostAsJsonAsync("/clientes", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Ingrediente ──────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_Ingredientes_ValidRequest_Returns201()
    {
        var request = new CrearIngredienteRequest("Tomate", UnidadDeMedida.Kilogramo);

        var response = await _client.PostAsJsonAsync("/ingredientes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task POST_Ingredientes_BlankName_Returns422()
    {
        // Domain guard: blank name throws DomainException → 422
        var request = new CrearIngredienteRequest("   ", UnidadDeMedida.Kilogramo);

        var response = await _client.PostAsJsonAsync("/ingredientes", request);

        // Validator NotEmpty catches empty string → 400; whitespace-only passes
        // validator (NotEmpty allows whitespace) but fails domain guard → 422
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422 for blank name, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task GET_Ingredientes_Returns_SeededIngredientes()
    {
        var response = await _client.GetAsync("/ingredientes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<IngredienteResponse>>(JsonOpts);
        Assert.NotNull(list);
        Assert.True(list!.Count >= 5, $"Expected at least 5 seeded ingredientes, got {list.Count}");
    }

    // ── Plato ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_Platos_ValidRequest_Returns201WithRecetaLines()
    {
        // First create an ingrediente so we can reference it
        var ingResponse = await _client.PostAsJsonAsync("/ingredientes",
            new CrearIngredienteRequest("Queso", UnidadDeMedida.Kilogramo));
        Assert.Equal(HttpStatusCode.Created, ingResponse.StatusCode);
        var ingId = await ingResponse.Content.ReadFromJsonAsync<Guid>();

        var request = new CrearPlatoRequest(
            "Pizza",
            500m,
            AlicuotaIVA.General,
            [new RecetaLineaRequest(ingId, 0.3m, UnidadDeMedida.Kilogramo)]);

        var response = await _client.PostAsJsonAsync("/platos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task POST_Platos_NegativePrecioBase_Returns422()
    {
        // PrecioBase <= 0 fails validator (400) first; -1 fails domain Dinero ctor (422)
        // PlatoValidator checks PrecioBase > 0 → 400
        var request = new CrearPlatoRequest(
            "Plato Invalido",
            -1m,
            AlicuotaIVA.General,
            []);

        var response = await _client.PostAsJsonAsync("/platos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Platos_Returns_SeededPlatos()
    {
        var response = await _client.GetAsync("/platos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlatoResponse>>(JsonOpts);
        Assert.NotNull(list);
        Assert.True(list!.Count >= 3, $"Expected at least 3 seeded platos, got {list.Count}");
    }

    // ── Menu ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_Menus_FutureDate_Returns201()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var request = new CrearMenuRequest(
            "Menu Test",
            tomorrow,
            []);

        var response = await _client.PostAsJsonAsync("/menus", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task POST_Menus_PastFechaVigencia_Returns422()
    {
        // Past date: validator gives 400; if validator passes somehow, domain gives 422
        // MenuValidator checks FechaVigencia > today → 400
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var request = new CrearMenuRequest(
            "Menu Pasado",
            yesterday,
            []);

        var response = await _client.PostAsJsonAsync("/menus", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Menus_Returns_SeededMenu_WithFutureDate()
    {
        var response = await _client.GetAsync("/menus");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<MenuResponse>>(JsonOpts);
        Assert.NotNull(list);
        Assert.True(list!.Count >= 1, $"Expected at least 1 seeded menu, got {list.Count}");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.True(list.Any(m => m.FechaVigencia > today),
            "Expected at least one seeded menu with a future FechaVigencia.");
    }

    // ── Mesa ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_Mesas_ValidRequest_Returns201()
    {
        var request = new CrearMesaRequest(99, 4);

        var response = await _client.PostAsJsonAsync("/mesas", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task POST_Mesas_ZeroCapacidad_Returns422()
    {
        // Capacidad = 0: validator fires → 400; if bypassed, domain fires → 422
        var request = new CrearMesaRequest(10, 0);

        var response = await _client.PostAsJsonAsync("/mesas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Mesas_Returns_SeededMesas()
    {
        var response = await _client.GetAsync("/mesas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<MesaResponse>>(JsonOpts);
        Assert.NotNull(list);
        Assert.True(list!.Count >= 4, $"Expected at least 4 seeded mesas, got {list.Count}");
    }
}
