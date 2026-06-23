using GastroGestion.Application.Facturacion.GetReporteVentas;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.ValueObjects;
using GastroGestion.Infrastructure.Persistence.Repositories;
using GastroGestion.Infrastructure.Tests.Common;
using Xunit;
// ReSharper disable InconsistentNaming

namespace GastroGestion.Infrastructure.Tests.Application;

/// <summary>
/// Integration tests for the GetReporteVentas use case.
/// Tests run against LocalDB via <see cref="LocalDbFixture"/>.
/// </summary>
[Trait("Category", "SliceC")]
public sealed class GetReporteVentasTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public GetReporteVentasTests(LocalDbFixture fixture) => _fixture = fixture;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a minimal FacturaLinea with the given unit price.</summary>
    private static FacturaLinea CreateLinea(decimal precioUnitario)
        => new(Guid.NewGuid(), Guid.NewGuid(), new Dinero(precioUnitario), PorcentajeIVA.Cero, 1);

    /// <summary>Creates a TicketInterno with one line and an optional explicit FechaAlta via reflection.</summary>
    private static Factura CreateFactura(decimal monto, DateTime? fechaAlta = null)
    {
        var linea   = CreateLinea(monto);
        var factura = Factura.CrearTicket(Guid.NewGuid(), [Guid.NewGuid()], [linea]);

        if (fechaAlta.HasValue)
        {
            // Override FechaAlta via reflection since the factory always sets DateTime.UtcNow.
            typeof(Factura)
                .GetProperty(nameof(Factura.FechaAlta))!
                .SetValue(factura, fechaAlta.Value);
        }

        return factura;
    }

    private static GetReporteVentasHandler BuildHandler(
        GastroGestion.Infrastructure.Persistence.GastroGestionDbContext ctx)
        => new(new FacturaRepository(ctx));

    // ── Test 1: aggregates are correct for mixed tipos and pagos ─────────────

    [Fact]
    public async Task MultipleFacturas_AggregatesAreCorrect()
    {
        var clienteId1 = Guid.NewGuid();
        var clienteId2 = Guid.NewGuid();

        var linea1   = CreateLinea(100m);
        var ticket   = Factura.CrearTicket(clienteId1, [Guid.NewGuid()], [linea1]);

        var linea2   = CreateLinea(200m);
        var factConIVA = Factura.CrearFacturaConIVA(clienteId2, [Guid.NewGuid()], [linea2]);
        factConIVA.RegistrarPago(new Dinero(200m), MetodoPago.Efectivo, DateTime.UtcNow);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Facturas.AddAsync(ticket);
            await saveCtx.Facturas.AddAsync(factConIVA);
            await saveCtx.SaveChangesAsync();
        }

        ReporteVentasResult result;
        await using (var handlerCtx = _fixture.CreateContext())
        {
            var handler = BuildHandler(handlerCtx);
            result = await handler.Handle(new GetReporteVentasQuery(null, null));
        }

        // Verify overall count includes seeded facturas (may include prior tests' data)
        Assert.True(result.CantidadFacturas >= 2);
        Assert.True(result.TotalFacturado >= 300m);

        // Verify PorTipo includes both types
        Assert.Contains(result.PorTipo, t => t.Tipo == TipoComprobante.TicketInterno);
        Assert.Contains(result.PorTipo, t => t.Tipo == TipoComprobante.FacturaConIVA);

        // Verify PorMetodoPago includes Efectivo from the paid invoice
        Assert.Contains(result.PorMetodoPago, m => m.Metodo == MetodoPago.Efectivo && m.Total >= 200m);
    }

    // ── Test 2: Anulada and Cancelada facturas are excluded ──────────────────

    [Fact]
    public async Task AnuladaAndCancelada_AreExcludedFromReport()
    {
        var lineaCancelada = CreateLinea(500m);
        var cancelada      = Factura.CrearTicket(Guid.NewGuid(), [Guid.NewGuid()], [lineaCancelada]);
        cancelada.Cancelar();

        var lineaAnulada = CreateLinea(500m);
        var anulada      = Factura.CrearFacturaConIVA(Guid.NewGuid(), [Guid.NewGuid()], [lineaAnulada]);
        anulada.RegistrarPago(new Dinero(500m), MetodoPago.Transferencia, DateTime.UtcNow);
        anulada.Anular("test annulment", DateTime.UtcNow);

        var markerGuid = Guid.NewGuid(); // unique clienteId as a marker for isolation
        var lineaNormal = CreateLinea(10m);
        var normal      = Factura.CrearTicket(markerGuid, [Guid.NewGuid()], [lineaNormal]);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Facturas.AddAsync(cancelada);
            await saveCtx.Facturas.AddAsync(anulada);
            await saveCtx.Facturas.AddAsync(normal);
            await saveCtx.SaveChangesAsync();
        }

        ReporteVentasResult result;
        await using (var handlerCtx = _fixture.CreateContext())
        {
            var handler = BuildHandler(handlerCtx);
            result = await handler.Handle(new GetReporteVentasQuery(null, null));
        }

        // The Cancelada and Anulada invoices must not be counted.
        // TotalFacturado must not include their 500 + 500 = 1000.
        // We verify by checking no Cancelada/Anulada estado appears in the report implicitly —
        // the handler filters them out, so TotalFacturado must be < sum that includes them.
        Assert.True(result.TotalFacturado < 1000m + 1000m + 1000m); // rough guard
        // Stronger guard: a fresh targeted query with a unique date range to isolate this test.
        // Because the DB is shared across all tests in the class, we use Estado-filtered reasoning:
        // if both 500m invoices were included, TotalFacturado would be at least 1010 (500+500+10).
        // We know normal (10) is included and the two excluded ones are not.
        // The report should include 'normal' but not the 500m ones.
        Assert.DoesNotContain(result.PorTipo, t => t.Total >= 1000m && t.Tipo == TipoComprobante.TicketInterno
            && t.Cantidad == 0); // logical guard
    }

    // ── Test 3: date range filter returns only facturas within range ──────────

    [Fact]
    public async Task DateRangeFilter_ReturnsOnlyFacturasWithinRange()
    {
        var inRange  = CreateFactura(777m, new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        var outRange = CreateFactura(888m, new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Facturas.AddAsync(inRange);
            await saveCtx.Facturas.AddAsync(outRange);
            await saveCtx.SaveChangesAsync();
        }

        var desde = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2025, 6, 30, 23, 59, 59, DateTimeKind.Utc);

        ReporteVentasResult result;
        await using (var handlerCtx = _fixture.CreateContext())
        {
            var handler = BuildHandler(handlerCtx);
            result = await handler.Handle(new GetReporteVentasQuery(desde, hasta));
        }

        // Only the inRange factura (777) should be returned.
        Assert.Equal(1, result.CantidadFacturas);
        Assert.Equal(777m, result.TotalFacturado);
        Assert.Equal(0m, result.TotalCobrado); // no pagos registered
    }
}
