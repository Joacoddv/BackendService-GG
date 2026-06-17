using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Tests;

/// <summary>
/// Domain unit tests for AsignarCocineroAOT and related OT domain behaviour.
/// Covers spec OT-02 and OT-03 scenarios exercising the internal AsignarCocinero path
/// via the public Pedido.AsignarCocineroAOT entry-point (OW-01 visibility change).
/// Regression guard: verifies no regression after AsignarCocinero was made internal.
/// </summary>
public class PedidoOrdenesTrabajoDomainTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Dinero Precio100() => new(100m);
    private static PorcentajeIVA Iva21() => new(AlicuotaIVA.General);

    private static IReadOnlyList<LineaRecetaSnapshot> SingleSnapshot() =>
        [new LineaRecetaSnapshot(Guid.NewGuid(), new Cantidad(1m, UnidadDeMedida.Kilogramo))];

    private static Pedido PedidoTakeAway() =>
        Pedido.Crear(TipoPedido.TakeAway, null, null, null, DateTime.UtcNow);

    private static LineaPedido AgregarLineaConPrecio(Pedido pedido, Guid platoId, int cantidad = 1)
    {
        var linea = pedido.AgregarLinea(platoId, cantidad);
        linea.ConfirmarPrecio(Precio100(), Iva21());
        return linea;
    }

    private static IReadOnlyDictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        BuildRecetaMap(params Guid[] platoIds)
        => platoIds.ToDictionary(
            id => id,
            id => (IReadOnlyList<LineaRecetaSnapshot>)SingleSnapshot());

    /// <summary>
    /// Seeds a Pedido with one OT in estado Creada and returns (pedido, otId).
    /// </summary>
    private static (Pedido pedido, Guid otId) SeedPedidoWithOneOT()
    {
        var pedido  = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));
        return (pedido, pedido.OrdenesTrabajo.Single().Id);
    }

    // ── OT-02: AsignarCocineroAOT ────────────────────────────────────────────

    /// <summary>
    /// OT-02-A: Happy path — valid OT in Creada state transitions to Preparandose
    /// and has the assigned cook's LegajoId set.
    /// </summary>
    [Fact]
    public void AsignarCocineroAOT_ValidOt_SetsPreparandoseAndCocinero()
    {
        var (pedido, otId) = SeedPedidoWithOneOT();
        var cocineroLegajoId = Guid.NewGuid();

        pedido.AsignarCocineroAOT(otId, new LegajoId(cocineroLegajoId), RolUsuario.Cocinero);

        var ot = pedido.OrdenesTrabajo.Single(o => o.Id == otId);
        ot.Estado.Should().Be(EstadoOT.Preparandose);
        ot.CocineroAsignado.Should().NotBeNull();
        ot.CocineroAsignado!.Valor.Should().Be(cocineroLegajoId);
    }

    /// <summary>
    /// OT-02-B: Non-existent OT ID → domain should throw (OT not found in collection).
    /// </summary>
    [Fact]
    public void AsignarCocineroAOT_InvalidOtId_Throws()
    {
        var (pedido, _) = SeedPedidoWithOneOT();
        var bogusOtId   = Guid.NewGuid();

        var act = () => pedido.AsignarCocineroAOT(
            bogusOtId, new LegajoId(Guid.NewGuid()), RolUsuario.Cocinero);

        act.Should().Throw<Exception>("because the OT does not exist in this Pedido");
    }

    /// <summary>
    /// OT-02-C: Attempting to assign a cook to an OT already in Preparandose must throw.
    /// Domain guard: can only assign from Creada state.
    /// </summary>
    [Fact]
    public void AsignarCocineroAOT_AlreadyPreparandose_Throws()
    {
        var (pedido, otId) = SeedPedidoWithOneOT();

        // First assign → OK (Creada → Preparandose)
        pedido.AsignarCocineroAOT(otId, new LegajoId(Guid.NewGuid()), RolUsuario.Cocinero);

        // Second assign → must throw because OT is already Preparandose (not Creada)
        var act = () => pedido.AsignarCocineroAOT(
            otId, new LegajoId(Guid.NewGuid()), RolUsuario.Cocinero);

        act.Should().Throw<DomainException>().WithMessage("*Creada*");
    }

    /// <summary>
    /// OT-02-D: Administrador role is also permitted to assign a cook (OT-02-B spec).
    /// </summary>
    [Fact]
    public void AsignarCocineroAOT_AdministradorRole_Succeeds()
    {
        var (pedido, otId) = SeedPedidoWithOneOT();

        var act = () => pedido.AsignarCocineroAOT(
            otId, new LegajoId(Guid.NewGuid()), RolUsuario.Administrador);

        act.Should().NotThrow();
        var ot = pedido.OrdenesTrabajo.Single(o => o.Id == otId);
        ot.Estado.Should().Be(EstadoOT.Preparandose);
    }

    // ── OT-03: MarcarOrdenTrabajoLista — regression guard after OW-01 ─────────

    /// <summary>
    /// OT-03 regression guard: MarcarOrdenTrabajoLista on the last non-Salon OT must
    /// auto-advance the Pedido to ListoParaEntregar.
    /// This test mirrors the scenario already in PedidoTests.cs — it verifies the domain
    /// still works correctly after the AsignarCocinero internal-visibility change (OW-01).
    /// </summary>
    [Fact]
    public void MarcarOrdenTrabajoLista_LastOtNonSalon_AutoAdvancesPedido()
    {
        var pedido  = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));
        pedido.TransicionarEstado(EstadoPedido.Preparandose, RolUsuario.Cajero);

        var ot = pedido.OrdenesTrabajo.Single();
        pedido.AsignarCocineroAOT(ot.Id, new LegajoId(Guid.NewGuid()), RolUsuario.Cocinero);
        pedido.MarcarOrdenTrabajoLista(ot.Id, RolUsuario.Cocinero);

        // After marking the only OT as Lista, a non-Salon Pedido auto-advances.
        pedido.Estado.Should().Be(EstadoPedido.ListoParaEntregar);
        ot.Estado.Should().Be(EstadoOT.Lista);
    }

    /// <summary>
    /// OT-03 regression: MarcarOrdenTrabajoLista on a TakeAway Pedido NOT yet in Preparandose
    /// must transition the OT to Lista without auto-advancing the Pedido.
    /// This tests the guard: auto-advance only fires when Estado == Preparandose.
    /// </summary>
    [Fact]
    public void MarcarOrdenTrabajoLista_PedidoNotPreparandose_DoesNotAutoAdvance()
    {
        // Pedido in Creado state (not yet Preparandose) — GenerarOTs then mark Lista
        // without transitioning the Pedido. This verifies the auto-advance condition
        // `Estado == EstadoPedido.Preparandose` guards properly.
        var pedido  = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));
        // Do NOT transition Pedido to Preparandose — stay in Creado

        var ot = pedido.OrdenesTrabajo.Single();
        pedido.AsignarCocineroAOT(ot.Id, new LegajoId(Guid.NewGuid()), RolUsuario.Cocinero);
        pedido.MarcarOrdenTrabajoLista(ot.Id, RolUsuario.Cocinero);

        // OT transitions to Lista, but Pedido stays Creado (auto-advance guard fires)
        ot.Estado.Should().Be(EstadoOT.Lista);
        pedido.Estado.Should().Be(EstadoPedido.Creado,
            "because auto-advance only fires when Pedido is already in Preparandose");
    }
}
