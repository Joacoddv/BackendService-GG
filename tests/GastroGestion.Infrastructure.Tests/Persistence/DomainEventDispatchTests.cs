using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for in-process domain event dispatch.
/// Covers REQ-08 Scenarios 08-A (dispatched once after commit), 08-B (not dispatched on failure),
/// 08-C (events cleared after dispatch).
/// </summary>
[Trait("Category", "SliceB")]
public sealed class DomainEventDispatchTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public DomainEventDispatchTests(LocalDbFixture fixture) => _fixture = fixture;

    // ── REQ-08 Scenario 08-A: events dispatched once after successful save ────

    [Fact]
    public async Task Events_AreDispatchedOnce_AfterSuccessfulSave()
    {
        var dispatcher = new CapturingDomainEventDispatcher();

        var pedido = Pedido.Crear(
            TipoPedido.Salon,
            Guid.NewGuid(),
            clienteId: null,
            direccionEntrega: null,
            DateTime.UtcNow);

        // Pedido.Crear raises PedidoCreado — one domain event
        Assert.Single(pedido.DomainEvents);

        await using var ctx = _fixture.CreateContext(dispatcher);
        await ctx.Pedidos.AddAsync(pedido);
        await ctx.SaveChangesAsync();

        // Dispatcher should have received exactly the one event
        Assert.Single(dispatcher.CapturedEvents);
    }

    // ── REQ-08 Scenario 08-B: events NOT dispatched when save fails ───────────

    [Fact]
    public async Task Events_AreNotDispatched_OnSaveFailure()
    {
        var dispatcher = new CapturingDomainEventDispatcher();

        var mesaId = Guid.NewGuid();
        var pedido = Pedido.Crear(TipoPedido.Salon, mesaId, null, null, DateTime.UtcNow);

        await using (var seedCtx = _fixture.CreateContext())
        {
            await seedCtx.Pedidos.AddAsync(pedido);
            await seedCtx.SaveChangesAsync();
        }

        // Try to save a duplicate — will fail on the unique Id constraint.
        // The Pedido factory always raises PedidoCreado, so DomainEvents count > 0.
        var duplicate = Pedido.Crear(TipoPedido.Salon, mesaId, null, null, DateTime.UtcNow);

        // Force the same Id to trigger a PK violation on save
        // We can't set Id directly (private set), so instead we try saving the original
        // instance again in a new context (which has it as Added, not Unchanged).
        await using var ctx = _fixture.CreateContext(dispatcher);
        await ctx.Pedidos.AddAsync(pedido); // same Id as already-seeded row → PK violation

        await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());

        // Dispatch must NOT have been called (save failed before dispatch step)
        Assert.Empty(dispatcher.CapturedEvents);
    }

    // ── REQ-08 Scenario 08-C: events cleared after successful dispatch ────────

    [Fact]
    public async Task Events_AreCleared_AfterSuccessfulDispatch()
    {
        var dispatcher = new CapturingDomainEventDispatcher();

        var pedido = Pedido.Crear(
            TipoPedido.TakeAway,
            mesaId: null,
            clienteId: Guid.NewGuid(),
            direccionEntrega: null,
            DateTime.UtcNow);

        Assert.NotEmpty(pedido.DomainEvents);

        await using var ctx = _fixture.CreateContext(dispatcher);
        await ctx.Pedidos.AddAsync(pedido);
        await ctx.SaveChangesAsync();

        // After a successful save+dispatch, the aggregate's DomainEvents list must be empty
        Assert.Empty(pedido.DomainEvents);
    }
}
