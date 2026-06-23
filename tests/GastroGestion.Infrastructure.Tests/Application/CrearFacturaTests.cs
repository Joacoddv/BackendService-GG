using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.ValueObjects;
using GastroGestion.Infrastructure.Persistence;
using GastroGestion.Infrastructure.Persistence.Repositories;
using GastroGestion.Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;
// ReSharper disable InconsistentNaming

namespace GastroGestion.Infrastructure.Tests.Application;

/// <summary>
/// Integration tests for the CrearFactura use case.
/// Covers REQ-11 (multi-client ConflictException guard, REQ-13-G enforcement).
/// Tests run against LocalDB via <see cref="LocalDbFixture"/>.
/// </summary>
[Trait("Category", "SliceC")]
public sealed class CrearFacturaTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public CrearFacturaTests(LocalDbFixture fixture) => _fixture = fixture;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a TakeAway Pedido with one confirmed price line.</summary>
    private static Pedido CreatePedidoWithConfirmedLine(Guid clienteId)
    {
        var pedido = Pedido.Crear(
            TipoPedido.TakeAway,
            mesaId: null,
            clienteId: clienteId,
            direccionEntrega: null,
            DateTime.UtcNow);

        var linea = pedido.AgregarLinea(Guid.NewGuid(), 1);
        linea.ConfirmarPrecio(new Dinero(500m), new PorcentajeIVA(AlicuotaIVA.General));
        return pedido;
    }

    private static CrearFacturaHandler BuildHandler(GastroGestionDbContext ctx)
    {
        var pedidoRepo   = new PedidoRepository(ctx);
        var facturaRepo  = new FacturaRepository(ctx);
        var clienteRepo  = new ClienteRepository(ctx);
        var uow          = new UnitOfWork(ctx);
        return new CrearFacturaHandler(pedidoRepo, facturaRepo, clienteRepo, uow);
    }

    /// <summary>
    /// Creates a ResponsableInscripto client with the given CUIT.
    /// Each caller must supply a distinct valid CUIT to avoid the IX_Clientes_Cuit unique index conflict
    /// (tests share the same LocalDbFixture database instance).
    /// </summary>
    private static Cliente CreateClienteResponsableInscripto(string cuit)
        => Cliente.Crear("Test RI", CondicionIVA.ResponsableInscripto, new Cuit(cuit), null);

    /// <summary>Creates a ConsumidorFinal client (no CUIT required).</summary>
    private static Cliente CreateClienteConsumidorFinal()
        => Cliente.Crear("Test CF", CondicionIVA.ConsumidorFinal, null, null);

    // ── REQ-11 Scenario 11-A: same-client Pedidos create Factura ─────────────

    [Fact]
    public async Task SameClientPedidos_CreatesFactura()
    {
        // FacturaConIVA requires a non-ConsumidorFinal condition → use ResponsableInscripto
        var cliente = CreateClienteResponsableInscripto("20123456786");
        var pedido1 = CreatePedidoWithConfirmedLine(cliente.Id);
        var pedido2 = CreatePedidoWithConfirmedLine(cliente.Id);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Clientes.AddAsync(cliente);
            await saveCtx.Pedidos.AddAsync(pedido1);
            await saveCtx.Pedidos.AddAsync(pedido2);
            await saveCtx.SaveChangesAsync();
        }

        Guid facturaId;
        await using (var handlerCtx = _fixture.CreateContext())
        {
            var handler = BuildHandler(handlerCtx);
            var cmd     = new CrearFacturaCommand(
                cliente.Id,
                [pedido1.Id, pedido2.Id],
                TipoComprobanteSolicitado.FacturaConIVA);

            facturaId = await handler.Handle(cmd);
        }

        await using var readCtx = _fixture.CreateContext();
        var factura = await readCtx.Facturas.FirstOrDefaultAsync(f => f.Id == facturaId);

        Assert.NotNull(factura);
        Assert.Equal(cliente.Id, factura.ClienteId);
        Assert.Equal(TipoComprobante.FacturaConIVA, factura.TipoComprobante);
        Assert.Equal(2, factura.PedidosFacturados.Count);
        Assert.Contains(pedido1.Id, factura.PedidosFacturados);
        Assert.Contains(pedido2.Id, factura.PedidosFacturados);
    }

    // ── REQ-11 Scenario 11-B: mixed-client Pedidos throw ConflictException ────

    [Fact]
    public async Task MixedClientPedidos_ThrowsConflictException()
    {
        var clienteId1 = Guid.NewGuid();
        var clienteId2 = Guid.NewGuid(); // different client
        var pedido1    = CreatePedidoWithConfirmedLine(clienteId1);
        var pedido2    = CreatePedidoWithConfirmedLine(clienteId2);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido1);
            await saveCtx.Pedidos.AddAsync(pedido2);
            await saveCtx.SaveChangesAsync();
        }

        await using var handlerCtx = _fixture.CreateContext();
        var handler = BuildHandler(handlerCtx);
        var cmd = new CrearFacturaCommand(
            clienteId1,
            [pedido1.Id, pedido2.Id],
            TipoComprobanteSolicitado.TicketInterno);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(cmd));

        Assert.Contains("REQ-13-G", ex.Message, StringComparison.Ordinal);
    }

    // ── REQ-11 Scenario 11-C: non-existent Pedido throws ConflictException ────

    [Fact]
    public async Task NonExistentPedido_ThrowsConflictException()
    {
        var clienteId      = Guid.NewGuid();
        var pedido         = CreatePedidoWithConfirmedLine(clienteId);
        var nonExistentId  = Guid.NewGuid();

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        await using var handlerCtx = _fixture.CreateContext();
        var handler = BuildHandler(handlerCtx);
        var cmd = new CrearFacturaCommand(
            clienteId,
            [pedido.Id, nonExistentId], // one valid, one missing
            TipoComprobanteSolicitado.TicketInterno);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(cmd));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── CondicionIVA comprobante guard ────────────────────────────────────────

    /// <summary>ConsumidorFinal cannot receive a FacturaConIVA — throws ConflictException.</summary>
    [Fact]
    public async Task CrearFactura_ConsumidorFinal_FacturaConIVA_Throws()
    {
        var cliente = CreateClienteConsumidorFinal();
        var pedido  = CreatePedidoWithConfirmedLine(cliente.Id);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Clientes.AddAsync(cliente);
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        await using var handlerCtx = _fixture.CreateContext();
        var handler = BuildHandler(handlerCtx);
        var cmd = new CrearFacturaCommand(
            cliente.Id,
            [pedido.Id],
            TipoComprobanteSolicitado.FacturaConIVA);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(cmd));

        Assert.Contains("ConsumidorFinal", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>ConsumidorFinal can receive a TicketInterno — succeeds.</summary>
    [Fact]
    public async Task CrearFactura_ConsumidorFinal_TicketInterno_Succeeds()
    {
        var cliente = CreateClienteConsumidorFinal();
        var pedido  = CreatePedidoWithConfirmedLine(cliente.Id);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Clientes.AddAsync(cliente);
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        Guid facturaId;
        await using (var handlerCtx = _fixture.CreateContext())
        {
            var handler = BuildHandler(handlerCtx);
            var cmd     = new CrearFacturaCommand(
                cliente.Id,
                [pedido.Id],
                TipoComprobanteSolicitado.TicketInterno);

            facturaId = await handler.Handle(cmd);
        }

        await using var readCtx = _fixture.CreateContext();
        var factura = await readCtx.Facturas.FirstOrDefaultAsync(f => f.Id == facturaId);

        Assert.NotNull(factura);
        Assert.Equal(TipoComprobante.TicketInterno, factura.TipoComprobante);
    }

    /// <summary>ResponsableInscripto can receive a FacturaConIVA — succeeds.</summary>
    [Fact]
    public async Task CrearFactura_ResponsableInscripto_FacturaConIVA_Succeeds()
    {
        // Uses a different valid CUIT than SameClientPedidos_CreatesFactura to avoid IX_Clientes_Cuit conflict
        var cliente = CreateClienteResponsableInscripto("20234567897");
        var pedido  = CreatePedidoWithConfirmedLine(cliente.Id);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Clientes.AddAsync(cliente);
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        Guid facturaId;
        await using (var handlerCtx = _fixture.CreateContext())
        {
            var handler = BuildHandler(handlerCtx);
            var cmd     = new CrearFacturaCommand(
                cliente.Id,
                [pedido.Id],
                TipoComprobanteSolicitado.FacturaConIVA);

            facturaId = await handler.Handle(cmd);
        }

        await using var readCtx = _fixture.CreateContext();
        var factura = await readCtx.Facturas.FirstOrDefaultAsync(f => f.Id == facturaId);

        Assert.NotNull(factura);
        Assert.Equal(TipoComprobante.FacturaConIVA, factura.TipoComprobante);
    }
}
