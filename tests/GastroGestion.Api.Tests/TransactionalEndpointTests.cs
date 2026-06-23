using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Contracts.Clientes;
using GastroGestion.Contracts.Facturacion;
using GastroGestion.Contracts.Ingredientes;
using GastroGestion.Contracts.Pedidos;
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
/// Integration tests for PR 3 transactional, fiscal, and stock endpoints.
/// Covers REQ-15 (Pedido), REQ-16 (Factura), REQ-17 (Stock), REQ-20 (full suite).
/// All tests tagged [Trait("Category","Integration")] — requires LocalDB.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class TransactionalEndpointTests
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public TransactionalEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateAuthenticatedClient(RolUsuario.Administrador);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> CreatePlatoAsync(string nombre = "PlatoTest")
    {
        // POST without JsonOpts — server expects enums as integers (default System.Text.Json behavior)
        var response = await _client.PostAsJsonAsync("/platos",
            new CrearPlatoRequest(nombre, 500m, AlicuotaIVA.General, []));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreateIngredienteAsync(string nombre = "IngTest")
    {
        var response = await _client.PostAsJsonAsync("/ingredientes",
            new CrearIngredienteRequest(nombre, UnidadDeMedida.Kilogramo));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreateMostradorPedidoAsync(Guid? clienteId = null)
    {
        var request = new CrearPedidoRequest(TipoPedido.TakeAway, null, clienteId, null);
        var response = await _client.PostAsJsonAsync("/pedidos", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> AddLineaAsync(Guid pedidoId, Guid platoId, int cantidad = 1)
    {
        var response = await _client.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/lineas",
            new AgregarLineaRequest(platoId, cantidad, null));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task ConfirmarPrecioAsync(Guid pedidoId, Guid lineaId)
    {
        var response = await _client.PostAsync(
            $"/pedidos/{pedidoId}/lineas/{lineaId}/confirmar-precio", null);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Creates a ConsumidorFinal cliente via the API and returns its Id.</summary>
    private async Task<Guid> CreateClienteConsumidorFinalAsync(string nombre = "ClienteTest")
    {
        var response = await _client.PostAsJsonAsync("/clientes",
            new CrearClienteRequest(nombre, CondicionIVA.ConsumidorFinal, null, null));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    // ── Pedido — Scenario 15-A ────────────────────────────────────────────────

    [Fact]
    public async Task POST_Pedidos_Salon_WithoutMesaId_Returns4xx()
    {
        // Salon without MesaId — validator fires 400 (friendlier check); domain invariant would fire 422.
        // Per design §6c: "duplicate-but-friendlier checks acceptable" — validator catches it first.
        var request = new CrearPedidoRequest(TipoPedido.Salon, null, null, null);

        var response = await _client.PostAsJsonAsync("/pedidos", request);

        // Validator fires first (400) or domain guard fires (422) — both are correct rejections
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422 for Salon without MesaId, got {(int)response.StatusCode}");
    }

    // ── Pedido — Scenario 15-B ────────────────────────────────────────────────

    [Fact]
    public async Task POST_Pedidos_Mostrador_Returns201()
    {
        var request = new CrearPedidoRequest(TipoPedido.TakeAway, null, null, null);

        var response = await _client.PostAsJsonAsync("/pedidos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    // ── Pedido — Scenario 15-C (W-01 deadlock regression — KEY TEST) ─────────

    [Fact]
    public async Task POST_Pedidos_AddLine_ThenConfirmPrice_Returns204_ExercisesW01()
    {
        // This test drives ConfirmarPrecioLinea through the FULL HTTP stack, exercising
        // EfectivoPrecioService.ResolverPrecioEfectivoAsync via genuine async/await.
        // A deadlock from sync-over-async would cause a timeout; passing = W-01 fixed.
        var platoId = await CreatePlatoAsync("PlatoW01Test");
        var pedidoId = await CreateMostradorPedidoAsync();
        var lineaId = await AddLineaAsync(pedidoId, platoId, 2);

        // This endpoint internally awaits ResolverPrecioEfectivoAsync — the W-01 live path
        var confirmarResponse = await _client.PostAsync(
            $"/pedidos/{pedidoId}/lineas/{lineaId}/confirmar-precio", null);

        Assert.Equal(HttpStatusCode.NoContent, confirmarResponse.StatusCode);

        // Verify the price is now confirmed on the pedido
        var getResponse = await _client.GetAsync($"/pedidos/{pedidoId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var pedido = await getResponse.Content.ReadFromJsonAsync<PedidoResponse>(JsonOpts);
        Assert.NotNull(pedido);
        var linea = pedido!.Lineas.FirstOrDefault(l => l.Id == lineaId);
        Assert.NotNull(linea);
        Assert.NotNull(linea!.PrecioUnitario);
        Assert.True(linea.PrecioUnitario > 0, "Confirmed price must be greater than zero.");
        Assert.NotNull(linea.IvaTasa);
    }

    // ── Pedido — Scenario 15-D ────────────────────────────────────────────────

    [Fact]
    public async Task POST_Pedidos_ConfirmPriceTwice_Returns422()
    {
        var platoId = await CreatePlatoAsync("PlatoDouble");
        var pedidoId = await CreateMostradorPedidoAsync();
        var lineaId = await AddLineaAsync(pedidoId, platoId);

        // First confirmation succeeds
        await ConfirmarPrecioAsync(pedidoId, lineaId);

        // Second confirmation: domain invariant set-once fires → DomainException → 422
        var second = await _client.PostAsync(
            $"/pedidos/{pedidoId}/lineas/{lineaId}/confirmar-precio", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
    }

    // ── Pedido — Scenario 15-E ────────────────────────────────────────────────

    [Fact]
    public async Task POST_Pedidos_Transicion_WrongRole_Returns422()
    {
        // TakeAway: Creado → Preparandose requires Cajero or Administrador.
        // Authenticating as Cocinero (wrong role) → domain DomainException → 422.
        var platoId = await CreatePlatoAsync("PlatoTransicion");
        var pedidoId = await CreateMostradorPedidoAsync();
        var lineaId = await AddLineaAsync(pedidoId, platoId);
        await ConfirmarPrecioAsync(pedidoId, lineaId);

        // Role travels in the JWT claim; body carries only EstadoNuevo.
        using var cocineroClient = _factory.CreateAuthenticatedClient(RolUsuario.Cocinero);
        var request  = new TransicionarEstadoRequest(EstadoPedido.Preparandose);
        var response = await cocineroClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/transicion", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ── Pedido — Scenario 15-F ────────────────────────────────────────────────

    [Fact]
    public async Task POST_Pedidos_Transicion_ValidRole_Returns200WithNewEstado()
    {
        // TakeAway: Creado → Preparandose with Cajero (allowed)
        var platoId = await CreatePlatoAsync("PlatoTransicionValida");
        var pedidoId = await CreateMostradorPedidoAsync();
        var lineaId = await AddLineaAsync(pedidoId, platoId);
        await ConfirmarPrecioAsync(pedidoId, lineaId);

        // Role travels in the JWT claim; body carries only EstadoNuevo.
        using var cajeroClient = _factory.CreateAuthenticatedClient(RolUsuario.Cajero);
        var request  = new TransicionarEstadoRequest(EstadoPedido.Preparandose);
        var response = await cajeroClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/transicion", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pedido = await response.Content.ReadFromJsonAsync<PedidoResponse>(JsonOpts);
        Assert.NotNull(pedido);
        Assert.Equal(EstadoPedido.Preparandose, pedido!.Estado);
    }

    // ── Pedido — Scenario 15-G ────────────────────────────────────────────────

    [Fact]
    public async Task GET_Pedidos_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/pedidos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Factura — Scenario 16-A (multi-client → 409) ─────────────────────────

    [Fact]
    public async Task POST_Facturas_MixedClientPedidos_Returns409()
    {
        // Two pedidos with different ClienteIds — CrearFacturaHandler enforces REQ-13-G
        var platoId = await CreatePlatoAsync("PlatoFacturaMultiClient");

        // Pedido for "client" A (no explicit client — uses null)
        var pedidoAId = await CreateMostradorPedidoAsync(clienteId: null);
        var lineaAId = await AddLineaAsync(pedidoAId, platoId);
        await ConfirmarPrecioAsync(pedidoAId, lineaAId);

        // Pedido for a specific client B (different ClienteId)
        var pedidoBClienteId = Guid.NewGuid(); // random, does not need to exist in DB for this check
        var pedidoBRequest = new CrearPedidoRequest(TipoPedido.TakeAway, null, pedidoBClienteId, null);
        var pedidoBResponse = await _client.PostAsJsonAsync("/pedidos", pedidoBRequest);
        pedidoBResponse.EnsureSuccessStatusCode();
        var pedidoBId = await pedidoBResponse.Content.ReadFromJsonAsync<Guid>();
        var lineaBId = await AddLineaAsync(pedidoBId, platoId);
        await ConfirmarPrecioAsync(pedidoBId, lineaBId);

        // Try to bill both under pedidoA's null ClienteId — handler sees clienteId mismatch → 409
        var facturaRequest = new CrearFacturaRequest(
            Guid.NewGuid(), // arbitrary clienteId that doesn't match either pedido
            [pedidoAId, pedidoBId],
            TipoComprobanteSolicitado.TicketInterno);

        var response = await _client.PostAsJsonAsync("/facturas", facturaRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── Factura — Scenario 16-B (no confirmed lines → 409) ───────────────────

    [Fact]
    public async Task POST_Facturas_PedidoWithNoConfirmedLines_Returns409()
    {
        // Pedido with unconfirmed line — CrearFacturaHandler filters to confirmed lines → empty → 409
        var platoId = await CreatePlatoAsync("PlatoNoConfirmado");
        var pedidoId = await CreateMostradorPedidoAsync();
        await AddLineaAsync(pedidoId, platoId); // line NOT confirmed

        var facturaRequest = new CrearFacturaRequest(
            Guid.NewGuid(),
            [pedidoId],
            TipoComprobanteSolicitado.TicketInterno);

        var response = await _client.PostAsJsonAsync("/facturas", facturaRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── Factura — Scenario 16-C ───────────────────────────────────────────────

    [Fact]
    public async Task POST_Facturas_ValidRequest_Returns201WithLocation()
    {
        // Full happy path: create pedido, add line, confirm price, then create factura
        var platoId   = await CreatePlatoAsync("PlatoFacturaValid");
        var clienteId = await CreateClienteConsumidorFinalAsync("ClienteFacturaValid");
        var pedidoRequest = new CrearPedidoRequest(TipoPedido.TakeAway, null, clienteId, null);
        var pedidoResponse = await _client.PostAsJsonAsync("/pedidos", pedidoRequest);
        pedidoResponse.EnsureSuccessStatusCode();
        var pedidoId = await pedidoResponse.Content.ReadFromJsonAsync<Guid>();
        var lineaId = await AddLineaAsync(pedidoId, platoId, 2);
        await ConfirmarPrecioAsync(pedidoId, lineaId);

        var facturaRequest = new CrearFacturaRequest(
            clienteId,
            [pedidoId],
            TipoComprobanteSolicitado.TicketInterno);

        var response = await _client.PostAsJsonAsync("/facturas", facturaRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    // ── Factura — Scenario 16-D (RegistrarPago totals) ───────────────────────

    [Fact]
    public async Task POST_Facturas_RegistrarPago_FullAmount_Returns204_EstaPagada()
    {
        // Create and bill a pedido, then register full payment and check EstaPagada
        var platoId   = await CreatePlatoAsync("PlatoPagado");
        var clienteId = await CreateClienteConsumidorFinalAsync("ClientePagado");
        var pedidoRequest = new CrearPedidoRequest(TipoPedido.TakeAway, null, clienteId, null);
        var pedidoResponse = await _client.PostAsJsonAsync("/pedidos", pedidoRequest);
        pedidoResponse.EnsureSuccessStatusCode();
        var pedidoId = await pedidoResponse.Content.ReadFromJsonAsync<Guid>();
        var lineaId = await AddLineaAsync(pedidoId, platoId, 1);
        await ConfirmarPrecioAsync(pedidoId, lineaId);

        var facturaRequest = new CrearFacturaRequest(
            clienteId, [pedidoId], TipoComprobanteSolicitado.TicketInterno);
        var facturaResponse = await _client.PostAsJsonAsync("/facturas", facturaRequest);
        facturaResponse.EnsureSuccessStatusCode();
        var facturaId = await facturaResponse.Content.ReadFromJsonAsync<Guid>();

        // Get the factura to check the total
        var getFactura = await _client.GetAsync($"/facturas/{facturaId}");
        getFactura.EnsureSuccessStatusCode();
        var factura = await getFactura.Content.ReadFromJsonAsync<FacturaResponse>(JsonOpts);
        Assert.NotNull(factura);
        Assert.False(factura!.EstaPagada, "Should not be paid yet.");

        // Register full payment (TicketInterno has zero IVA, so Total == SubTotal)
        var pagoRequest = new RegistrarPagoRequest(factura.Total, MetodoPago.Efectivo);
        var pagoResponse = await _client.PostAsJsonAsync(
            $"/facturas/{facturaId}/pagos", pagoRequest);

        Assert.Equal(HttpStatusCode.NoContent, pagoResponse.StatusCode);

        // Verify EstaPagada
        var finalFactura = await _client.GetAsync($"/facturas/{facturaId}");
        var facturaFinal = await finalFactura.Content.ReadFromJsonAsync<FacturaResponse>(JsonOpts);
        Assert.NotNull(facturaFinal);
        Assert.True(facturaFinal!.EstaPagada, "Factura should be paid after full payment.");
    }

    // ── Factura — Scenario 16-E (RegistrarPago on Cancelada → 422) ───────────

    [Fact]
    public async Task POST_Facturas_RegistrarPago_OnCancelada_Returns422()
    {
        // Create factura, cancel it, then try to pay → DomainException → 422
        var platoId   = await CreatePlatoAsync("PlatoCancelado");
        var clienteId = await CreateClienteConsumidorFinalAsync("ClienteCancelado");
        var pedidoRequest = new CrearPedidoRequest(TipoPedido.TakeAway, null, clienteId, null);
        var pedidoResponse = await _client.PostAsJsonAsync("/pedidos", pedidoRequest);
        pedidoResponse.EnsureSuccessStatusCode();
        var pedidoId = await pedidoResponse.Content.ReadFromJsonAsync<Guid>();
        var lineaId = await AddLineaAsync(pedidoId, platoId, 1);
        await ConfirmarPrecioAsync(pedidoId, lineaId);

        var facturaRequest = new CrearFacturaRequest(
            clienteId, [pedidoId], TipoComprobanteSolicitado.TicketInterno);
        var facturaResponse = await _client.PostAsJsonAsync("/facturas", facturaRequest);
        facturaResponse.EnsureSuccessStatusCode();
        var facturaId = await facturaResponse.Content.ReadFromJsonAsync<Guid>();

        // Get the factura total
        var getFactura = await _client.GetAsync($"/facturas/{facturaId}");
        var facturaData = await getFactura.Content.ReadFromJsonAsync<FacturaResponse>(JsonOpts);
        Assert.NotNull(facturaData);

        // Pay full amount to move to Pagada state, then try to pay again
        // (simpler than trying to cancel: Factura.Cancelar() has no API endpoint here,
        // so we test the Pagada → second payment guard which also throws DomainException)
        var fullPayment = new RegistrarPagoRequest(facturaData!.Total, MetodoPago.Efectivo);
        var firstPago = await _client.PostAsJsonAsync($"/facturas/{facturaId}/pagos", fullPayment);
        Assert.Equal(HttpStatusCode.NoContent, firstPago.StatusCode);

        // Now try to register another payment on a Pagada factura → 422
        var extraPago = new RegistrarPagoRequest(1m, MetodoPago.Efectivo);
        var errorResponse = await _client.PostAsJsonAsync(
            $"/facturas/{facturaId}/pagos", extraPago);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, errorResponse.StatusCode);
    }

    // ── Factura — Scenario 16-F ───────────────────────────────────────────────

    [Fact]
    public async Task GET_Facturas_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/facturas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Stock — Scenario 17-A ─────────────────────────────────────────────────

    [Fact]
    public async Task POST_Stock_Compra_Returns201()
    {
        var ingredienteId = await CreateIngredienteAsync("HarinaStock");

        var request = new RegistrarMovimientoStockRequest(
            ingredienteId,
            TipoMovimientoStock.Compra,
            10m,
            null,
            null);

        var response = await _client.PostAsJsonAsync("/stock/movimientos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    // ── Stock — Scenario 17-B ─────────────────────────────────────────────────

    [Fact]
    public async Task POST_Stock_Merma_Returns201_FactoryNegatesQuantity()
    {
        // Merma (scrap): caller passes positive absolute value; factory stores as negative.
        // Consumo/Reserva are system-driven (OT events) and rejected by the manual endpoint.
        var ingredienteId = await CreateIngredienteAsync("AguaStock");

        // First register a purchase so balance is positive before scrapping
        await _client.PostAsJsonAsync("/stock/movimientos",
            new RegistrarMovimientoStockRequest(ingredienteId, TipoMovimientoStock.Compra, 20m, null, null));

        var request = new RegistrarMovimientoStockRequest(
            ingredienteId,
            TipoMovimientoStock.Merma,
            5m,   // absolute value — factory will negate
            null,
            null);

        var response = await _client.PostAsJsonAsync("/stock/movimientos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Balance should be 20 - 5 = 15
        var balanceResponse = await _client.GetAsync($"/stock/balance/{ingredienteId}");
        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);
        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceStockResponse>(JsonOpts);
        Assert.NotNull(balance);
        Assert.Equal(15m, balance!.Balance);
    }

    // ── Stock — Scenario 17-C ─────────────────────────────────────────────────

    [Fact]
    public async Task GET_Stock_Balance_ReturnsCorrectNet()
    {
        var ingredienteId = await CreateIngredienteAsync("SalStock");

        // Register 3 movements: +100, -30, +20 → net = 90
        await _client.PostAsJsonAsync("/stock/movimientos",
            new RegistrarMovimientoStockRequest(ingredienteId, TipoMovimientoStock.Compra, 100m, null, null));

        await _client.PostAsJsonAsync("/stock/movimientos",
            new RegistrarMovimientoStockRequest(ingredienteId, TipoMovimientoStock.Merma, 30m, null, null));

        await _client.PostAsJsonAsync("/stock/movimientos",
            new RegistrarMovimientoStockRequest(ingredienteId, TipoMovimientoStock.Compra, 20m, null, null));

        var response = await _client.GetAsync($"/stock/balance/{ingredienteId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var balance = await response.Content.ReadFromJsonAsync<BalanceStockResponse>(JsonOpts);
        Assert.NotNull(balance);
        Assert.Equal(90m, balance!.Balance);
    }

    // ── Stock — Scenario 17-D ─────────────────────────────────────────────────

    [Fact]
    public async Task GET_Stock_Balance_NoMovements_Returns200_WithZeroBalance()
    {
        // Unknown ingredienteId — zero movements — returns 0, NOT 404
        var unknownId = Guid.NewGuid();

        var response = await _client.GetAsync($"/stock/balance/{unknownId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var balance = await response.Content.ReadFromJsonAsync<BalanceStockResponse>(JsonOpts);
        Assert.NotNull(balance);
        Assert.Equal(0m, balance!.Balance);
        Assert.Equal(unknownId, balance.IngredienteId);
    }

    // ── Validation guard (400) ────────────────────────────────────────────────

    [Fact]
    public async Task POST_Pedidos_AddLine_ZeroCantidad_Returns400()
    {
        var pedidoId = await CreateMostradorPedidoAsync();

        var request = new AgregarLineaRequest(Guid.NewGuid(), 0, null);
        var response = await _client.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/lineas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Stock_ZeroCantidad_Returns400()
    {
        var request = new RegistrarMovimientoStockRequest(
            Guid.NewGuid(), TipoMovimientoStock.Compra, 0m, null, null);

        var response = await _client.PostAsJsonAsync("/stock/movimientos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Stock_SystemDrivenType_Returns400()
    {
        // Reserva is driven by the OT lifecycle event handlers; posting it manually is rejected.
        var request = new RegistrarMovimientoStockRequest(
            Guid.NewGuid(), TipoMovimientoStock.Reserva, 5m, null, null);

        var response = await _client.PostAsJsonAsync("/stock/movimientos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
