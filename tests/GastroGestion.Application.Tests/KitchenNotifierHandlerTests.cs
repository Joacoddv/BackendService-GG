using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Realtime;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Pedidos.AsignarCocinero;
using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Application.Pedidos.MarcarOrdenTrabajoLista;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GastroGestion.Application.Tests;

/// <summary>
/// Unit tests verifying that IKitchenNotifier is called exactly once after a successful
/// state-change, and NOT called when the handler throws before or during save.
/// Covers OT-05 (ADR-003) for AsignarCocineroHandler and MarcarOrdenTrabajoListaHandler.
/// </summary>
public class KitchenNotifierHandlerTests
{
    // ── Shared mocks ──────────────────────────────────────────────────────────

    private readonly IPedidoRepository _pedidos         = Substitute.For<IPedidoRepository>();
    private readonly IUnitOfWork       _uow             = Substitute.For<IUnitOfWork>();
    private readonly IKitchenNotifier  _kitchenNotifier = Substitute.For<IKitchenNotifier>();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a Pedido that has one work order already generated (Creada state),
    /// ready for AsignarCocinero. Returns the OT id via out param.
    /// </summary>
    private static Domain.Pedidos.Pedido BuildPedidoWithOneOt(out Guid otId)
    {
        var platoId = Guid.NewGuid();
        var pedido  = Domain.Pedidos.Pedido.Crear(TipoPedido.TakeAway, null, null, null, DateTime.UtcNow);
        var linea   = pedido.AgregarLinea(platoId, 1);
        linea.ConfirmarPrecio(new Dinero(100m), new PorcentajeIVA(AlicuotaIVA.General));

        var receta = new Dictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        {
            [platoId] = new List<LineaRecetaSnapshot>
            {
                new(Guid.NewGuid(), new Cantidad(200m, UnidadDeMedida.Gramo))
            }
        };
        pedido.GenerarOrdenesTrabajo(receta);

        otId = pedido.OrdenesTrabajo.First().Id;
        return pedido;
    }

    // ═══════════════════════════════════════════════════════════════
    // AsignarCocineroHandler — notifier contract
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// OT-05: Successful AsignarCocinero → IKitchenNotifier.NotifyOtChangedAsync called once.
    /// </summary>
    [Fact]
    public async Task AsignarCocinero_Success_CallsNotifierOnce()
    {
        var pedido = BuildPedidoWithOneOt(out var otId);
        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);

        var sut = new AsignarCocineroHandler(_pedidos, _uow, _kitchenNotifier);
        var cmd = new AsignarCocineroCommand(pedido.Id, otId, Guid.NewGuid(), RolUsuario.Cocinero);

        await sut.Handle(cmd);

        await _kitchenNotifier.Received(1)
            .NotifyOtChangedAsync(
                Arg.Is<OrdenTrabajoBoardItem>(b => b.OtId == otId),
                Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// OT-05: ForbiddenException before save → IKitchenNotifier must NOT be called.
    /// </summary>
    [Fact]
    public async Task AsignarCocinero_ForbiddenRole_DoesNotCallNotifier()
    {
        var pedido = BuildPedidoWithOneOt(out var otId);
        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);

        var sut = new AsignarCocineroHandler(_pedidos, _uow, _kitchenNotifier);
        var cmd = new AsignarCocineroCommand(pedido.Id, otId, Guid.NewGuid(), RolUsuario.Mozo);

        var act = async () => await sut.Handle(cmd);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _kitchenNotifier.DidNotReceive()
            .NotifyOtChangedAsync(
                Arg.Any<OrdenTrabajoBoardItem>(),
                Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════
    // MarcarOrdenTrabajoListaHandler — notifier contract
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// OT-05: Successful MarcarOrdenTrabajoLista → IKitchenNotifier.NotifyOtChangedAsync called once.
    /// </summary>
    [Fact]
    public async Task MarcarOrdenTrabajoLista_Success_CallsNotifierOnce()
    {
        var pedido = BuildPedidoWithOneOt(out var otId);

        // First assign a cook so the OT moves to Preparandose (required before MarcarLista)
        var cocineroId = Guid.NewGuid();
        pedido.AsignarCocineroAOT(otId, new LegajoId(cocineroId), RolUsuario.Cocinero);

        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);

        var sut = new MarcarOrdenTrabajoListaHandler(_pedidos, _uow, _kitchenNotifier);
        var cmd = new MarcarOrdenTrabajoListaCommand(pedido.Id, otId, RolUsuario.Cocinero);

        await sut.Handle(cmd);

        await _kitchenNotifier.Received(1)
            .NotifyOtChangedAsync(
                Arg.Is<OrdenTrabajoBoardItem>(b => b.OtId == otId),
                Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// OT-05: Domain throws (OT not in Preparandose state) before save →
    /// IKitchenNotifier must NOT be called.
    /// </summary>
    [Fact]
    public async Task MarcarOrdenTrabajoLista_DomainThrows_DoesNotCallNotifier()
    {
        // OT is in Creada state (no asignar-cocinero called) → domain throws on MarcarLista
        var pedido = BuildPedidoWithOneOt(out var otId);
        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);

        var sut = new MarcarOrdenTrabajoListaHandler(_pedidos, _uow, _kitchenNotifier);
        var cmd = new MarcarOrdenTrabajoListaCommand(pedido.Id, otId, RolUsuario.Cocinero);

        // Domain enforces Preparandose guard — expect some exception (DomainException / InvalidOperationException)
        var act = async () => await sut.Handle(cmd);

        await act.Should().ThrowAsync<Exception>();
        await _kitchenNotifier.DidNotReceive()
            .NotifyOtChangedAsync(
                Arg.Any<OrdenTrabajoBoardItem>(),
                Arg.Any<CancellationToken>());
    }
}
