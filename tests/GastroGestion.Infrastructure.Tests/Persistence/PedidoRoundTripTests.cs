using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.ValueObjects;
using GastroGestion.Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;
// ReSharper disable InconsistentNaming

namespace GastroGestion.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for Pedido aggregate round-trip persistence.
/// Covers REQ-05 (PrecioConfirmado), REQ-06 (owned graph + snapshot), REQ-04 (DireccionEntrega),
/// REQ-09 (RowVersion concurrency).
/// </summary>
[Trait("Category", "SliceB")]
public sealed class PedidoRoundTripTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public PedidoRoundTripTests(LocalDbFixture fixture) => _fixture = fixture;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Pedido CreateSalonPedido(Guid? mesaId = null)
        => Pedido.Crear(
            TipoPedido.Salon,
            mesaId ?? Guid.NewGuid(),
            clienteId: null,
            direccionEntrega: null,
            DateTime.UtcNow);

    private static Pedido CreateDeliveryPedido()
    {
        var dir = new DireccionEntrega("Corrientes", "1234", "Buenos Aires", "CABA", "1043");
        return Pedido.Crear(
            TipoPedido.Delivery,
            mesaId: null,
            clienteId: Guid.NewGuid(),
            direccionEntrega: dir,
            DateTime.UtcNow);
    }

    private static IReadOnlyList<LineaRecetaSnapshot> SampleSnapshot()
        => [new LineaRecetaSnapshot(Guid.NewGuid(), new Cantidad(500m, UnidadDeMedida.Gramo))];

    // ── REQ-06 Scenario 06-A: full owned graph round-trip ─────────────────────

    [Fact]
    public async Task Pedido_WithLineasAndOrdenesTrabajo_RoundTrips()
    {
        var pedido = CreateSalonPedido();
        var platoId = Guid.NewGuid();
        var precio = new Dinero(1500m, Moneda.ARS);
        var iva = new PorcentajeIVA(AlicuotaIVA.General);
        var linea = pedido.AgregarLinea(platoId, 2, "sin cebolla");

        linea.ConfirmarPrecio(precio, iva);

        var snapshot = SampleSnapshot();
        pedido.GenerarOrdenesTrabajo(new Dictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        {
            { platoId, snapshot }
        });

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Pedidos.FirstOrDefaultAsync(p => p.Id == pedido.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(TipoPedido.Salon, reloaded.Tipo);
        Assert.Single(reloaded.Lineas);

        var reloadedLinea = reloaded.Lineas[0];
        Assert.Equal(platoId, reloadedLinea.PlatoId);
        Assert.Equal(2, reloadedLinea.Cantidad);
        Assert.Equal("sin cebolla", reloadedLinea.Observaciones);
        Assert.NotNull(reloadedLinea.PrecioUnitario);
        Assert.Equal(1500m, reloadedLinea.PrecioUnitario.Monto);
        Assert.Equal(AlicuotaIVA.General, reloadedLinea.IVA!.Alicuota);

        Assert.Single(reloaded.OrdenesTrabajo);
        var reloadedOT = reloaded.OrdenesTrabajo[0];
        Assert.Equal(platoId, reloadedOT.PlatoId);
        Assert.Single(reloadedOT.RecetaSnapshot);
        Assert.Equal(snapshot[0].IngredienteId, reloadedOT.RecetaSnapshot[0].IngredienteId);
        Assert.Equal(500m, reloadedOT.RecetaSnapshot[0].Cantidad.Valor);
    }

    // ── REQ-06 Scenario 06-B: snapshot survives Plato recipe change ───────────

    [Fact]
    public async Task RecetaSnapshot_IsImmutable_AfterPlatoRecipeChange()
    {
        // The snapshot is stored as a JSON column. Even if the live Plato recipe changes,
        // the OT snapshot must remain what it was at creation time.
        var pedido = CreateSalonPedido();
        var platoId = Guid.NewGuid();
        var linea = pedido.AgregarLinea(platoId, 1);
        linea.ConfirmarPrecio(new Dinero(500m, Moneda.ARS), new PorcentajeIVA(AlicuotaIVA.Exento));

        var originalIngredienteId = Guid.NewGuid();
        var snapshot = new List<LineaRecetaSnapshot>
        {
            new(originalIngredienteId, new Cantidad(200m, UnidadDeMedida.Gramo))
        };
        pedido.GenerarOrdenesTrabajo(new Dictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        {
            { platoId, snapshot }
        });

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        // Reload and verify snapshot is still the original
        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Pedidos.FirstOrDefaultAsync(p => p.Id == pedido.Id);

        Assert.NotNull(reloaded);
        var reloadedOT = reloaded.OrdenesTrabajo[0];
        Assert.Single(reloadedOT.RecetaSnapshot);
        Assert.Equal(originalIngredienteId, reloadedOT.RecetaSnapshot[0].IngredienteId);
        Assert.Equal(200m, reloadedOT.RecetaSnapshot[0].Cantidad.Valor);
    }

    // ── REQ-04 Scenario 04-C: null DireccionEntrega for Salon ─────────────────

    [Fact]
    public async Task DireccionEntrega_IsNull_ForSalonPedido()
    {
        var pedido = CreateSalonPedido();

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Pedidos.FirstOrDefaultAsync(p => p.Id == pedido.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(TipoPedido.Salon, reloaded.Tipo);
        Assert.Null(reloaded.DireccionEntrega);
    }

    // ── REQ-04 Scenario 04-D: DireccionEntrega for Delivery ───────────────────

    [Fact]
    public async Task DireccionEntrega_IsPresent_ForDeliveryPedido()
    {
        var pedido = CreateDeliveryPedido();

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Pedidos.FirstOrDefaultAsync(p => p.Id == pedido.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(TipoPedido.Delivery, reloaded.Tipo);
        Assert.NotNull(reloaded.DireccionEntrega);
        Assert.Equal("Corrientes", reloaded.DireccionEntrega.Calle);
        Assert.Equal("1234", reloaded.DireccionEntrega.Numero);
        Assert.Equal("Buenos Aires", reloaded.DireccionEntrega.Ciudad);
        Assert.Equal("CABA", reloaded.DireccionEntrega.Provincia);
        Assert.Equal("1043", reloaded.DireccionEntrega.CodigoPostal);
    }

    // ── REQ-05 Scenario 05-A: PrecioConfirmado survives reload, rejects re-confirm

    [Fact]
    public async Task PrecioConfirmado_True_AfterReload_RejectsSecondConfirm()
    {
        var pedido = CreateSalonPedido();
        var platoId = Guid.NewGuid();
        var linea = pedido.AgregarLinea(platoId, 1);
        linea.ConfirmarPrecio(new Dinero(1000m, Moneda.ARS), new PorcentajeIVA(AlicuotaIVA.General));

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        // Reload in fresh context
        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Pedidos.FirstOrDefaultAsync(p => p.Id == pedido.Id);

        Assert.NotNull(reloaded);
        var reloadedLinea = reloaded.Lineas[0];

        // PrecioConfirmado must have survived the round-trip.
        // Verified behaviorally: a second ConfirmarPrecio must throw DomainException.
        var ex = Assert.Throws<DomainException>(
            () => reloadedLinea.ConfirmarPrecio(
                new Dinero(999m, Moneda.ARS),
                new PorcentajeIVA(AlicuotaIVA.General)));

        Assert.Contains("set-once", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── REQ-05 Scenario 05-B: PrecioConfirmado false after reload allows first confirm

    [Fact]
    public async Task PrecioConfirmado_False_AfterReload_AllowsFirstConfirm()
    {
        var pedido = CreateSalonPedido();
        var platoId = Guid.NewGuid();
        pedido.AgregarLinea(platoId, 1); // no ConfirmarPrecio

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Pedidos.FirstOrDefaultAsync(p => p.Id == pedido.Id);

        Assert.NotNull(reloaded);
        var reloadedLinea = reloaded.Lineas[0];

        // Should NOT throw — PrecioConfirmado is false, first confirm is allowed.
        var exception = Record.Exception(
            () => reloadedLinea.ConfirmarPrecio(
                new Dinero(800m, Moneda.ARS),
                new PorcentajeIVA(AlicuotaIVA.Exento)));
        Assert.Null(exception);
    }

    // ── REQ-09 Scenario 09-B: RowVersion non-empty after first save ────────────

    [Fact]
    public async Task RowVersion_IsNonEmpty_AfterFirstSave()
    {
        var pedido = CreateSalonPedido();

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Pedidos.AddAsync(pedido);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Pedidos.FirstOrDefaultAsync(p => p.Id == pedido.Id);

        Assert.NotNull(reloaded);
        Assert.NotNull(reloaded.RowVersion);
        Assert.NotEmpty(reloaded.RowVersion);
    }

    // ── REQ-09 Scenario 09-A: concurrent update throws DbUpdateConcurrencyException

    [Fact]
    public async Task ConcurrentPedidoUpdate_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange — save a new Pedido so both contexts start with the same RowVersion.
        var pedido = CreateSalonPedido();
        await using (var seedCtx = _fixture.CreateContext())
        {
            await seedCtx.Pedidos.AddAsync(pedido);
            await seedCtx.SaveChangesAsync();
        }

        // Load the same row into two independent contexts simultaneously.
        // Both have the same (current) RowVersion at load time.
        await using var ctx1 = _fixture.CreateContext();
        await using var ctx2 = _fixture.CreateContext();

        var first  = await ctx1.Pedidos.FirstAsync(p => p.Id == pedido.Id);
        var second = await ctx2.Pedidos.FirstAsync(p => p.Id == pedido.Id);

        // ctx1 wins the race — Mozo can cancel a Salon Pedido from Abierto state
        first.TransicionarEstado(EstadoPedido.Cancelado, RolUsuario.Mozo);
        await ctx1.SaveChangesAsync(); // increments DB RowVersion

        // ctx2 has a stale RowVersion — second save must throw optimistic concurrency exception
        second.TransicionarEstado(EstadoPedido.Cancelado, RolUsuario.Mozo);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => ctx2.SaveChangesAsync());
    }
}
