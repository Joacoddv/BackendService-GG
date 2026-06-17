using GastroGestion.Contracts.Pedidos;
using GastroGestion.Contracts.Platos;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// Integration tests for the OrdenTrabajo kitchen workflow endpoints.
/// Covers OT-01..OT-04 (generate, assign, mark-lista, board GET) and OT-06 (contract shape guard).
///
/// NOTE — 204 vs 201 discrepancy (OT-01-A):
/// The endpoint returns 204 NoContent for POST /ordenes-trabajo (see OrdenTrabajoEndpoints.cs line 39).
/// Spec OT-01-A originally described 201 Created. Tasks.md OW-14 documents 204.
/// Tests assert the CURRENT implementation (204). Flagged for reconciliation in sdd-verify.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class KitchenEndpointTests
{
    private readonly HttpClient _adminClient;
    private readonly HttpClient _mozoClient;
    private readonly HttpClient _cocineroClient;
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public KitchenEndpointTests(ApiFactory factory)
    {
        _factory        = factory;
        _adminClient    = factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        _mozoClient     = factory.CreateAuthenticatedClient(RolUsuario.Mozo);
        _cocineroClient = factory.CreateAuthenticatedClient(RolUsuario.Cocinero);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an Ingrediente and returns its ID.
    /// </summary>
    private async Task<Guid> CreateIngredienteAsync(string nombre = "Harina")
    {
        var response = await _adminClient.PostAsJsonAsync("/ingredientes",
            new { nombre, unidadDeMedida = (int)UnidadDeMedida.Kilogramo });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>
    /// Creates a Plato with one recipe line and returns its ID.
    /// </summary>
    private async Task<Guid> CreatePlatoWithRecipeAsync(string nombre = "PlatoKitchen")
    {
        var ingredienteId = await CreateIngredienteAsync($"Ing_{nombre}");

        var response = await _adminClient.PostAsJsonAsync("/platos",
            new CrearPlatoRequest(
                nombre,
                250m,
                AlicuotaIVA.General,
                [new RecetaLineaRequest(ingredienteId, 200m, UnidadDeMedida.Gramo)]));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>
    /// Creates a Plato without any recipe lines and returns its ID.
    /// </summary>
    private async Task<Guid> CreatePlatoWithoutRecipeAsync(string nombre = "PlatoNoReceta")
    {
        var response = await _adminClient.PostAsJsonAsync("/platos",
            new CrearPlatoRequest(nombre, 100m, AlicuotaIVA.General, []));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>
    /// Creates a TakeAway Pedido, adds a line for the given Plato, confirms its price,
    /// and returns the pedidoId.
    /// </summary>
    private async Task<(Guid pedidoId, Guid lineaId)> CreatePedidoWithPricedLineAsync(Guid platoId)
    {
        var pedidoResponse = await _adminClient.PostAsJsonAsync("/pedidos",
            new CrearPedidoRequest(TipoPedido.TakeAway, null, null, null));
        pedidoResponse.EnsureSuccessStatusCode();
        var pedidoId = await pedidoResponse.Content.ReadFromJsonAsync<Guid>();

        var lineaResponse = await _adminClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/lineas",
            new AgregarLineaRequest(platoId, 1, null));
        lineaResponse.EnsureSuccessStatusCode();
        var lineaId = await lineaResponse.Content.ReadFromJsonAsync<Guid>();

        var confirmResponse = await _adminClient.PostAsync(
            $"/pedidos/{pedidoId}/lineas/{lineaId}/confirmar-precio", null);
        confirmResponse.EnsureSuccessStatusCode();

        return (pedidoId, lineaId);
    }

    /// <summary>
    /// Generates work orders for a Pedido using the Mozo client.
    /// Asserts 204 (see class-level note about 204 vs 201).
    /// Returns the response for additional assertions.
    /// </summary>
    private async Task<HttpResponseMessage> GenerarOTsAsync(Guid pedidoId, HttpClient? client = null)
    {
        client ??= _mozoClient;
        return await client.PostAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo", null);
    }

    /// <summary>
    /// Gets the single OT ID for a Pedido via the board endpoint.
    /// </summary>
    private async Task<Guid> GetFirstOtIdAsync(Guid pedidoId)
    {
        var boardResponse = await _adminClient.GetAsync("/ordenes-trabajo");
        boardResponse.EnsureSuccessStatusCode();
        var items = await boardResponse.Content
            .ReadFromJsonAsync<List<OrdenTrabajoBoardResponse>>(JsonOpts);
        return items!.First(i => i.PedidoId == pedidoId).OtId;
    }

    // ── Generate OTs ──────────────────────────────────────────────────────────

    /// <summary>
    /// OT-01-A: Mozo POSTs to generate OTs → 204 (current implementation; spec says 201 — see class note).
    /// Board GET returns the created item with correct PedidoId and PlatoId.
    /// </summary>
    [Fact]
    public async Task POST_GenerarOrdenesTrabajo_HappyPath_Returns204_CreatesBoardItems()
    {
        var platoId          = await CreatePlatoWithRecipeAsync("PlatoHappy");
        var (pedidoId, lineaId) = await CreatePedidoWithPricedLineAsync(platoId);

        var genResponse = await GenerarOTsAsync(pedidoId);

        // NOTE: current implementation returns 204, not 201. See class-level note.
        Assert.Equal(HttpStatusCode.NoContent, genResponse.StatusCode);

        // Board should contain an item for this Pedido
        var boardResponse = await _adminClient.GetAsync($"/ordenes-trabajo?estado=Creada");
        Assert.Equal(HttpStatusCode.OK, boardResponse.StatusCode);

        var items = await boardResponse.Content
            .ReadFromJsonAsync<List<OrdenTrabajoBoardResponse>>(JsonOpts);
        Assert.NotNull(items);

        var item = items!.FirstOrDefault(i => i.PedidoId == pedidoId);
        Assert.NotNull(item);
        Assert.Equal(platoId, item!.PlatoId);
        Assert.Equal(lineaId, item.LineaPedidoId);
        Assert.Equal(EstadoOT.Creada, item.Estado);
        Assert.Null(item.CocineroAsignadoLegajoId);
    }

    /// <summary>
    /// OT-01-B: Plato with empty LineasReceta → 422 UnprocessableEntity; board stays empty for this Pedido.
    /// </summary>
    [Fact]
    public async Task POST_GenerarOrdenesTrabajo_EmptyRecipe_Returns422_NothingPersisted()
    {
        var platoId          = await CreatePlatoWithoutRecipeAsync("PlatoEmptyRecipe");
        var (pedidoId, _)   = await CreatePedidoWithPricedLineAsync(platoId);

        var genResponse = await GenerarOTsAsync(pedidoId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, genResponse.StatusCode);

        // Board should have no items for this Pedido
        var boardResponse = await _adminClient.GetAsync("/ordenes-trabajo");
        boardResponse.EnsureSuccessStatusCode();
        var items = await boardResponse.Content
            .ReadFromJsonAsync<List<OrdenTrabajoBoardResponse>>(JsonOpts);
        var forThisPedido = items!.Where(i => i.PedidoId == pedidoId).ToList();
        Assert.Empty(forThisPedido);
    }

    /// <summary>
    /// OT-01-C: Line without confirmed price → 422 UnprocessableEntity.
    /// </summary>
    [Fact]
    public async Task POST_GenerarOrdenesTrabajo_UnconfirmedPrice_Returns422()
    {
        var platoId = await CreatePlatoWithRecipeAsync("PlatoUnpricedLine");

        // Create Pedido with line but do NOT confirm price
        var pedidoResponse = await _adminClient.PostAsJsonAsync("/pedidos",
            new CrearPedidoRequest(TipoPedido.TakeAway, null, null, null));
        pedidoResponse.EnsureSuccessStatusCode();
        var pedidoId = await pedidoResponse.Content.ReadFromJsonAsync<Guid>();

        await _adminClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/lineas",
            new AgregarLineaRequest(platoId, 1, null));
        // Price NOT confirmed

        var genResponse = await GenerarOTsAsync(pedidoId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, genResponse.StatusCode);
    }

    /// <summary>
    /// OT-01-D: OTs already generated → 409 Conflict.
    /// </summary>
    [Fact]
    public async Task POST_GenerarOrdenesTrabajo_AlreadyGenerated_Returns409()
    {
        var platoId          = await CreatePlatoWithRecipeAsync("PlatoDoubleGen");
        var (pedidoId, _)   = await CreatePedidoWithPricedLineAsync(platoId);

        // First generation succeeds
        var first = await GenerarOTsAsync(pedidoId);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        // Second generation → 409
        var second = await GenerarOTsAsync(pedidoId);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    /// <summary>
    /// OT-01-E: Pedido not found → 404.
    /// </summary>
    [Fact]
    public async Task POST_GenerarOrdenesTrabajo_PedidoNotFound_Returns404()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await _mozoClient.PostAsync(
            $"/pedidos/{nonExistentId}/ordenes-trabajo", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Assign cook ───────────────────────────────────────────────────────────

    /// <summary>
    /// OT-02-A: Cocinero assigns cook → 200 OK; OT estado is Preparandose; CocineroAsignadoLegajoId set.
    /// </summary>
    [Fact]
    public async Task POST_AsignarCocinero_Cocinero_Returns200_OtIsPreparandose()
    {
        var platoId            = await CreatePlatoWithRecipeAsync("PlatoAsignarCocinero");
        var (pedidoId, _)     = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();
        var otId               = await GetFirstOtIdAsync(pedidoId);
        var cocineroLegajoId   = Guid.NewGuid();

        var response = await _cocineroClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(cocineroLegajoId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ot = await response.Content.ReadFromJsonAsync<OrdenTrabajoResponse>(JsonOpts);
        Assert.NotNull(ot);
        Assert.Equal(EstadoOT.Preparandose, ot!.Estado);
        Assert.Equal(cocineroLegajoId, ot.CocineroAsignadoLegajoId);
    }

    /// <summary>
    /// OT-02-A: Administrador role can also assign a cook → 200 OK.
    /// </summary>
    [Fact]
    public async Task POST_AsignarCocinero_Administrador_Returns200()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoAsignarAdmin");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();
        var otId              = await GetFirstOtIdAsync(pedidoId);

        var response = await _adminClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// OT-02-B: Mozo role → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task POST_AsignarCocinero_Mozo_Returns403()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoAsignarMozo");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();
        var otId              = await GetFirstOtIdAsync(pedidoId);

        var response = await _mozoClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// OT-02-B: Authenticated user with no role claim → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task POST_AsignarCocinero_WithoutRole_Returns403()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoAsignarNoRole");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();
        var otId              = await GetFirstOtIdAsync(pedidoId);

        using var noRoleClient = _factory.CreateAuthenticatedClientWithoutRole();
        var response = await noRoleClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// OT-02-B: Authenticated user with unparseable role claim → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task POST_AsignarCocinero_BogusRole_Returns403()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoAsignarBogus");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();
        var otId              = await GetFirstOtIdAsync(pedidoId);

        using var bogusClient = _factory.CreateAuthenticatedClientWithBogusRole("NotARealRole");
        var response = await bogusClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// OT-02-C: OT already in Preparandose → domain throws DomainException → 422.
    /// </summary>
    [Fact]
    public async Task POST_AsignarCocinero_AlreadyPreparandose_Returns422()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoDoubleAssign");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();
        var otId              = await GetFirstOtIdAsync(pedidoId);
        var legajoId          = Guid.NewGuid();

        // First assign — OK
        var first = await _cocineroClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(legajoId));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Second assign — OT is now Preparandose, not Creada → domain throws → 422
        var second = await _cocineroClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
    }

    /// <summary>
    /// OT-02-D: Pedido not found → 404.
    /// </summary>
    [Fact]
    public async Task POST_AsignarCocinero_PedidoNotFound_Returns404()
    {
        var response = await _cocineroClient.PostAsJsonAsync(
            $"/pedidos/{Guid.NewGuid()}/ordenes-trabajo/{Guid.NewGuid()}/asignar-cocinero",
            new AsignarCocineroRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Mark lista ────────────────────────────────────────────────────────────

    /// <summary>
    /// OT-03-A: Cocinero marks last OT Lista → 200 OK; OT estado is Lista.
    /// </summary>
    [Fact]
    public async Task POST_MarcarLista_Cocinero_Returns200_OtIsLista()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoMarcarLista");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();
        var otId              = await GetFirstOtIdAsync(pedidoId);

        // Assign cook first
        (await _cocineroClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(Guid.NewGuid()))).EnsureSuccessStatusCode();

        // Transition Pedido to Preparandose (required for auto-advance check)
        await _adminClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/transicion",
            new TransicionarEstadoRequest(EstadoPedido.Preparandose));

        var response = await _cocineroClient.PostAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/marcar-lista", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ot = await response.Content.ReadFromJsonAsync<OrdenTrabajoResponse>(JsonOpts);
        Assert.NotNull(ot);
        Assert.Equal(EstadoOT.Lista, ot!.Estado);
    }

    /// <summary>
    /// OT-03-B: Last OT marked Lista on non-Salon Pedido in Preparandose →
    /// Pedido auto-advances to ListoParaEntregar (assert via GET /pedidos/{id}).
    /// </summary>
    [Fact]
    public async Task POST_MarcarLista_LastOt_NonSalon_AutoAdvancesPedidoToListoParaEntregar()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoAutoAdvance");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();
        var otId              = await GetFirstOtIdAsync(pedidoId);

        // Assign cook
        (await _cocineroClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(Guid.NewGuid()))).EnsureSuccessStatusCode();

        // Transition Pedido to Preparandose (required for auto-advance)
        (await _adminClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/transicion",
            new TransicionarEstadoRequest(EstadoPedido.Preparandose))).EnsureSuccessStatusCode();

        // Mark OT Lista (last OT → auto-advance fires)
        (await _cocineroClient.PostAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/marcar-lista", null))
            .EnsureSuccessStatusCode();

        // Verify Pedido state via GET /pedidos/{id}
        var pedidoGet = await _adminClient.GetAsync($"/pedidos/{pedidoId}");
        Assert.Equal(HttpStatusCode.OK, pedidoGet.StatusCode);

        var pedido = await pedidoGet.Content.ReadFromJsonAsync<PedidoResponse>(JsonOpts);
        Assert.NotNull(pedido);
        Assert.Equal(EstadoPedido.ListoParaEntregar, pedido!.Estado);
    }

    /// <summary>
    /// OT-03-C: Mozo role → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task POST_MarcarLista_WrongRole_Returns403()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoMarcarListaWrongRole");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();
        var otId              = await GetFirstOtIdAsync(pedidoId);

        (await _cocineroClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero",
            new AsignarCocineroRequest(Guid.NewGuid()))).EnsureSuccessStatusCode();

        var response = await _mozoClient.PostAsync(
            $"/pedidos/{pedidoId}/ordenes-trabajo/{otId}/marcar-lista", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Board GET ─────────────────────────────────────────────────────────────

    /// <summary>
    /// OT-04-A: GET /ordenes-trabajo?estado=Creada returns only OTs with that state.
    /// </summary>
    [Fact]
    public async Task GET_OrdenesTrabajo_WithEstadoFilter_ReturnsOnlyMatching()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoFilterTest");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();

        var response = await _adminClient.GetAsync("/ordenes-trabajo?estado=Creada");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content
            .ReadFromJsonAsync<List<OrdenTrabajoBoardResponse>>(JsonOpts);
        Assert.NotNull(items);
        Assert.All(items!, i => Assert.Equal(EstadoOT.Creada, i.Estado));
    }

    /// <summary>
    /// OT-04-B: GET /ordenes-trabajo with no filter returns 200 with a list (may include all states).
    /// </summary>
    [Fact]
    public async Task GET_OrdenesTrabajo_NoFilter_Returns200()
    {
        var response = await _adminClient.GetAsync("/ordenes-trabajo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content
            .ReadFromJsonAsync<List<OrdenTrabajoBoardResponse>>(JsonOpts);
        Assert.NotNull(items);
    }

    /// <summary>
    /// OT-04-C: GET /ordenes-trabajo?estado=BogusValue → 400 Bad Request with details.
    /// </summary>
    [Fact]
    public async Task GET_OrdenesTrabajo_InvalidEstado_Returns400()
    {
        var response = await _adminClient.GetAsync("/ordenes-trabajo?estado=BogusEstado");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BogusEstado", body);
    }

    /// <summary>
    /// OT-04-D: GET /ordenes-trabajo without auth → 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GET_OrdenesTrabajo_Unauthenticated_Returns401()
    {
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync("/ordenes-trabajo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Contract shape guard (OT-06) ──────────────────────────────────────────

    /// <summary>
    /// OT-06: GET /pedidos/{id} response shape must be identical before and after generating OTs.
    /// The PedidoResponse contract must not change (locked decision 5 — OT-06).
    /// PedidoResponse does NOT include OrdenesTrabajo — they live in the separate board endpoint.
    /// </summary>
    [Fact]
    public async Task GET_PedidoById_AfterGeneratingOTs_ResponseShapeUnchanged()
    {
        var platoId           = await CreatePlatoWithRecipeAsync("PlatoContractGuard");
        var (pedidoId, _)    = await CreatePedidoWithPricedLineAsync(platoId);

        // Snapshot before generating OTs
        var beforeResponse = await _adminClient.GetAsync($"/pedidos/{pedidoId}");
        Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);
        var beforeJson = await beforeResponse.Content.ReadAsStringAsync();
        var beforePedido = JsonSerializer.Deserialize<PedidoResponse>(beforeJson, JsonOpts);
        Assert.NotNull(beforePedido);

        // Generate OTs
        (await GenerarOTsAsync(pedidoId)).EnsureSuccessStatusCode();

        // Snapshot after generating OTs
        var afterResponse = await _adminClient.GetAsync($"/pedidos/{pedidoId}");
        Assert.Equal(HttpStatusCode.OK, afterResponse.StatusCode);
        var afterPedido = await afterResponse.Content
            .ReadFromJsonAsync<PedidoResponse>(JsonOpts);
        Assert.NotNull(afterPedido);

        // Shape contract: same Id, same Tipo, same Lineas count — OTs are NOT in PedidoResponse
        Assert.Equal(beforePedido!.Id,    afterPedido!.Id);
        Assert.Equal(beforePedido.Tipo,   afterPedido.Tipo);
        Assert.Equal(beforePedido.Lineas.Count, afterPedido.Lineas.Count);

        // Verify OrdenesTrabajo are NOT embedded in PedidoResponse (OT-06 locked decision)
        // We assert by deserializing raw JSON and confirming no "ordenesTrabajo" key exists.
        var afterJson = await afterResponse.Content.ReadAsStringAsync();
        var doc       = JsonDocument.Parse(afterJson);
        Assert.False(
            doc.RootElement.TryGetProperty("ordenesTrabajo", out _),
            "PedidoResponse must NOT include an 'ordenesTrabajo' property (OT-06 contract lock).");
    }
}
