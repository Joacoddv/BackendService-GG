using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.ValueObjects;
using GastroGestion.Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;
// ReSharper disable InconsistentNaming

namespace GastroGestion.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for Factura aggregate round-trip persistence.
/// Covers REQ-10: flat table, TipoComprobante discriminator column,
/// nullable CAE, PedidosFacturados JSON column.
/// </summary>
[Trait("Category", "SliceC")]
public sealed class FacturaRoundTripTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public FacturaRoundTripTests(LocalDbFixture fixture) => _fixture = fixture;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<FacturaLinea> SampleLineas(PorcentajeIVA? iva = null)
    {
        iva ??= new PorcentajeIVA(AlicuotaIVA.General);
        return
        [
            new FacturaLinea(Guid.NewGuid(), Guid.NewGuid(), new Dinero(1000m), iva, 2)
        ];
    }

    // ── REQ-10 Scenario 10-A: TicketInterno persists with null CAE ────────────

    [Fact]
    public async Task TicketInterno_Persists_WithNullCae()
    {
        var clienteId = Guid.NewGuid();
        var pedidoIds = new List<Guid> { Guid.NewGuid() };
        var factura   = Factura.CrearTicket(clienteId, pedidoIds, SampleLineas());

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Facturas.AddAsync(factura);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Facturas.FirstOrDefaultAsync(f => f.Id == factura.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(TipoComprobante.TicketInterno, reloaded.TipoComprobante);
        Assert.Equal(EstadoFactura.Creada, reloaded.Estado);
        Assert.Equal(clienteId, reloaded.ClienteId);
        Assert.Null(reloaded.CAE);
        Assert.Null(reloaded.VencimientoCAE);
        Assert.Single(reloaded.Lineas);
        // TicketInterno forces all lines to Exento (zero IVA)
        Assert.Equal(AlicuotaIVA.Exento, reloaded.Lineas[0].IVA.Alicuota);
    }

    // ── REQ-10 Scenario 10-B: FacturaConIVA persists with null CAE ───────────

    [Fact]
    public async Task FacturaConIVA_Persists_WithNullCae()
    {
        var clienteId = Guid.NewGuid();
        var pedidoIds = new List<Guid> { Guid.NewGuid() };
        var lineas    = SampleLineas(new PorcentajeIVA(AlicuotaIVA.General));
        var factura   = Factura.CrearFacturaConIVA(clienteId, pedidoIds, lineas);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Facturas.AddAsync(factura);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Facturas.FirstOrDefaultAsync(f => f.Id == factura.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(TipoComprobante.FacturaConIVA, reloaded.TipoComprobante);
        Assert.Null(reloaded.CAE);
        Assert.Null(reloaded.VencimientoCAE);
        Assert.Single(reloaded.Lineas);
        Assert.Equal(AlicuotaIVA.General, reloaded.Lineas[0].IVA.Alicuota);
    }

    // ── REQ-10 Scenario 10-C: FacturaElectronica accepts CAE after reload ─────

    [Fact]
    public async Task FacturaElectronica_AcceptsCaeAssignment_AfterReload()
    {
        var clienteId = Guid.NewGuid();
        var pedidoIds = new List<Guid> { Guid.NewGuid() };
        var factura   = Factura.CrearFacturaElectronica(clienteId, pedidoIds, SampleLineas());

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Facturas.AddAsync(factura);
            await saveCtx.SaveChangesAsync();
        }

        // Reload and assign CAE (simulating AFIP response)
        await using (var updateCtx = _fixture.CreateContext())
        {
            var loaded = await updateCtx.Facturas.FirstOrDefaultAsync(f => f.Id == factura.Id);
            Assert.NotNull(loaded);
            Assert.Equal(TipoComprobante.FacturaElectronica, loaded.TipoComprobante);
            Assert.Null(loaded.CAE);

            var cae        = "12345678901234";
            var vencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
            loaded.AsignarCae(cae, vencimiento);
            await updateCtx.SaveChangesAsync();
        }

        // Final read: verify CAE and VencimientoCAE are persisted
        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Facturas.FirstOrDefaultAsync(f => f.Id == factura.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("12345678901234", reloaded.CAE);
        Assert.NotNull(reloaded.VencimientoCAE);
    }

    // ── REQ-10 Scenario 10-D: PedidosFacturados JSON column round-trips ──────

    [Fact]
    public async Task PedidosFacturados_JsonColumn_RoundTrips_ThreeGuids()
    {
        var clienteId = Guid.NewGuid();
        var pedidoId1 = Guid.NewGuid();
        var pedidoId2 = Guid.NewGuid();
        var pedidoId3 = Guid.NewGuid();
        var pedidoIds = new List<Guid> { pedidoId1, pedidoId2, pedidoId3 };

        var lineas = new List<FacturaLinea>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), new Dinero(500m), new PorcentajeIVA(AlicuotaIVA.General), 1),
            new(Guid.NewGuid(), Guid.NewGuid(), new Dinero(300m), new PorcentajeIVA(AlicuotaIVA.Exento), 2),
        };

        var factura = Factura.CrearFacturaConIVA(clienteId, pedidoIds, lineas);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Facturas.AddAsync(factura);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Facturas.FirstOrDefaultAsync(f => f.Id == factura.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(3, reloaded.PedidosFacturados.Count);
        Assert.Contains(pedidoId1, reloaded.PedidosFacturados);
        Assert.Contains(pedidoId2, reloaded.PedidosFacturados);
        Assert.Contains(pedidoId3, reloaded.PedidosFacturados);
    }

    // ── Multi-payment totals round-trip ───────────────────────────────────────

    [Fact]
    public async Task MultiPago_Totals_RoundTrip()
    {
        var clienteId = Guid.NewGuid();
        var pedidoIds = new List<Guid> { Guid.NewGuid() };
        var lineas    = new List<FacturaLinea>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), new Dinero(1000m), new PorcentajeIVA(AlicuotaIVA.Exento), 1)
        };

        var factura = Factura.CrearTicket(clienteId, pedidoIds, lineas);
        factura.RegistrarPago(new Dinero(400m), MetodoPago.Efectivo, DateTime.UtcNow);
        factura.RegistrarPago(new Dinero(600m), MetodoPago.TarjetaDebito, DateTime.UtcNow);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Facturas.AddAsync(factura);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Facturas.FirstOrDefaultAsync(f => f.Id == factura.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(EstadoFactura.Pagada, reloaded.Estado);
        Assert.Equal(2, reloaded.Pagos.Count);

        var efectivo = reloaded.Pagos.Single(p => p.MetodoPago == MetodoPago.Efectivo);
        var tarjeta  = reloaded.Pagos.Single(p => p.MetodoPago == MetodoPago.TarjetaDebito);
        Assert.Equal(400m, efectivo.Monto.Monto);
        Assert.Equal(600m, tarjeta.Monto.Monto);
    }
}
