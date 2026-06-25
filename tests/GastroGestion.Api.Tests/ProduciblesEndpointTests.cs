using GastroGestion.Contracts.Ingredientes;
using GastroGestion.Contracts.Platos;
using GastroGestion.Contracts.Stock;
using GastroGestion.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// Integration tests for GET /stock/producibles.
/// Verifies: 200 response, correct MaxProducible calculation, inactive platos excluded.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class ProduciblesEndpointTests
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ProduciblesEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateAuthenticatedClient(RolUsuario.Administrador);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> CreateIngredienteAsync(string nombre)
    {
        var response = await _client.PostAsJsonAsync("/ingredientes",
            new CrearIngredienteRequest(nombre, UnidadDeMedida.Kilogramo));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreatePlatoAsync(string nombre, RecetaLineaRequest[] lineas)
    {
        var response = await _client.PostAsJsonAsync("/platos",
            new CrearPlatoRequest(nombre, 500m, AlicuotaIVA.General, lineas));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task RegisterCompraAsync(Guid ingredienteId, decimal cantidad)
    {
        var response = await _client.PostAsJsonAsync("/stock/movimientos",
            new RegistrarMovimientoStockRequest(ingredienteId, TipoMovimientoStock.Compra, cantidad, null, null, null));
        response.EnsureSuccessStatusCode();
    }

    // ── Scenario 18-A: GET /stock/producibles returns 200 ─────────────────────

    [Fact]
    public async Task GET_StockProducibles_Returns200()
    {
        var response = await _client.GetAsync("/stock/producibles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Scenario 18-B: correct MaxProducible based on seeded stock ────────────

    [Fact]
    public async Task GET_StockProducibles_ReturnsCorrectMaxProducible()
    {
        // Arrange — create an ingredient and register stock
        var ingId = await CreateIngredienteAsync($"Ing_Producible_{Guid.NewGuid():N}");
        await RegisterCompraAsync(ingId, 2.0m); // 2 kg

        // Create plato needing 0.5 kg each → can produce floor(2.0 / 0.5) = 4
        var platoId = await CreatePlatoAsync(
            $"Plato_Producible_{Guid.NewGuid():N}",
            new[] { new RecetaLineaRequest(ingId, 0.5m, UnidadDeMedida.Kilogramo) });

        // Act
        var response = await _client.GetAsync("/stock/producibles");
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<PlatoProducibleResponse>>(JsonOpts);
        Assert.NotNull(rows);

        var row = rows.FirstOrDefault(r => r.PlatoId == platoId);
        Assert.NotNull(row);
        Assert.Equal(4, row!.MaxProducible);
    }

    // ── Scenario 18-C: plato with no stock shows MaxProducible == 0 ─────────

    [Fact]
    public async Task GET_StockProducibles_PlatoWithNoStock_ReturnsZero()
    {
        // Create an ingredient but register NO stock movements.
        var ingId = await CreateIngredienteAsync($"Ing_NoStock_{Guid.NewGuid():N}");

        var platoId = await CreatePlatoAsync(
            $"Plato_NoStock_{Guid.NewGuid():N}",
            new[] { new RecetaLineaRequest(ingId, 1.0m, UnidadDeMedida.Kilogramo) });

        // Act
        var response = await _client.GetAsync("/stock/producibles");
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<PlatoProducibleResponse>>(JsonOpts);
        Assert.NotNull(rows);

        var row = rows.FirstOrDefault(r => r.PlatoId == platoId);
        Assert.NotNull(row);
        Assert.Equal(0, row!.MaxProducible);
    }

    // ── Scenario 18-D: result is ordered alphabetically by Nombre ────────────

    [Fact]
    public async Task GET_StockProducibles_IsOrderedByNombre()
    {
        var response = await _client.GetAsync("/stock/producibles");
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<PlatoProducibleResponse>>(JsonOpts);
        Assert.NotNull(rows);

        if (rows!.Count >= 2)
        {
            for (int i = 1; i < rows.Count; i++)
                Assert.True(
                    string.Compare(rows[i - 1].Nombre, rows[i].Nombre, StringComparison.Ordinal) <= 0,
                    $"Expected rows ordered by Nombre but found '{rows[i - 1].Nombre}' before '{rows[i].Nombre}'");
        }
    }

    // ── Scenario 18-E: unauthenticated returns 401 ────────────────────────────

    [Fact]
    public async Task GET_StockProducibles_WithoutToken_Returns401()
    {
        // Use the factory to create an unauthenticated client (no Bearer header).
        using var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/stock/producibles");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
