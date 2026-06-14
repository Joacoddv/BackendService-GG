using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Facturacion.CrearFactura;
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
        var pedidoRepo  = new PedidoRepository(ctx);
        var facturaRepo = new FacturaRepository(ctx);
        var uow         = new UnitOfWork(ctx);
        return new CrearFacturaHandler(pedidoRepo, facturaRepo, uow);
    }

    // ── REQ-11 Scenario 11-A: same-client Pedidos create Factura ─────────────

    [Fact]
    public async Task SameClientPedidos_CreatesFactura()
    {
        var clienteId = Guid.NewGuid();
        var pedido1   = CreatePedidoWithConfirmedLine(clienteId);
        var pedido2   = CreatePedidoWithConfirmedLine(clienteId);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido1);
            await saveCtx.Pedidos.AddAsync(pedido2);
            await saveCtx.SaveChangesAsync();
        }

        Guid facturaId;
        await using (var handlerCtx = _fixture.CreateContext())
        {
            var handler = BuildHandler(handlerCtx);
            var cmd     = new CrearFacturaCommand(
                clienteId,
                [pedido1.Id, pedido2.Id],
                TipoComprobanteSolicitado.FacturaConIVA);

            facturaId = await handler.Handle(cmd);
        }

        await using var readCtx = _fixture.CreateContext();
        var factura = await readCtx.Facturas.FirstOrDefaultAsync(f => f.Id == facturaId);

        Assert.NotNull(factura);
        Assert.Equal(clienteId, factura.ClienteId);
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
}
