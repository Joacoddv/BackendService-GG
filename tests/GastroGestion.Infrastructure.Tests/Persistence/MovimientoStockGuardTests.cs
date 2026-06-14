using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Stock;
using GastroGestion.Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for MovimientoStock append-only enforcement.
/// Covers REQ-07 Scenarios 07-B (Modify throws), 07-C (Delete throws), 07-D (balance SUM).
/// </summary>
[Trait("Category", "SliceB")]
public sealed class MovimientoStockGuardTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public MovimientoStockGuardTests(LocalDbFixture fixture) => _fixture = fixture;

    // ── REQ-07 Scenario 07-B: modifying a persisted MovimientoStock throws ────

    [Fact]
    public async Task ModifyPersistedMovimientoStock_ThrowsBeforeCommit()
    {
        var ingredienteId = Guid.NewGuid();
        var movimiento = MovimientoStock.RegistrarCompra(ingredienteId, 20m);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.MovimientosStock.AddAsync(movimiento);
            await saveCtx.SaveChangesAsync();
        }

        await using var ctx = _fixture.CreateContext();
        var reloaded = await ctx.MovimientosStock.FirstAsync(m => m.Id == movimiento.Id);

        // Simulate a modification by directly telling EF the entry is Modified.
        // (There are no mutation methods on MovimientoStock by design.)
        ctx.Entry(reloaded).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ctx.SaveChangesAsync());

        Assert.Contains("append-only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── REQ-07 Scenario 07-C: deleting a persisted MovimientoStock throws ─────

    [Fact]
    public async Task DeletePersistedMovimientoStock_ThrowsBeforeCommit()
    {
        var ingredienteId = Guid.NewGuid();
        var movimiento = MovimientoStock.RegistrarCompra(ingredienteId, 10m);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.MovimientosStock.AddAsync(movimiento);
            await saveCtx.SaveChangesAsync();
        }

        await using var ctx = _fixture.CreateContext();
        var reloaded = await ctx.MovimientosStock.FirstAsync(m => m.Id == movimiento.Id);

        ctx.MovimientosStock.Remove(reloaded);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ctx.SaveChangesAsync());

        Assert.Contains("append-only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── REQ-07 Scenario 07-D: SUM balance is correct ─────────────────────────

    [Fact]
    public async Task CalcularBalance_SumIsCorrect()
    {
        // Design: Compra +20, Reserva -5, Consumo -5, LiberacionReserva +5 = 15
        var ingredienteId = Guid.NewGuid();

        var compra = MovimientoStock.RegistrarMovimiento(ingredienteId, TipoMovimientoStock.Compra, 20m);
        var reserva = MovimientoStock.RegistrarMovimiento(ingredienteId, TipoMovimientoStock.Reserva, 5m);
        var consumo = MovimientoStock.RegistrarMovimiento(ingredienteId, TipoMovimientoStock.Consumo, 5m);
        var liberacion = MovimientoStock.RegistrarMovimiento(ingredienteId, TipoMovimientoStock.LiberacionReserva, 5m);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.MovimientosStock.AddAsync(compra);
            await saveCtx.MovimientosStock.AddAsync(reserva);
            await saveCtx.MovimientosStock.AddAsync(consumo);
            await saveCtx.MovimientosStock.AddAsync(liberacion);
            await saveCtx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var balance = await readCtx.MovimientosStock
            .Where(m => m.IngredienteId == ingredienteId)
            .SumAsync(m => m.Cantidad);

        // +20 - 5 - 5 + 5 = 15
        Assert.Equal(15m, balance);
    }
}
