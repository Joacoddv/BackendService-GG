using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.Pedidos.Events;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Tests;

/// <summary>
/// Covers spec REQ-07 through REQ-11 and REQ-15: Pedido creation invariants,
/// DireccionEntrega, PedidoTransicionRegistry, LineaPedido, OrdenTrabajo,
/// cancellation cascade, and domain events.
/// </summary>
public class PedidoTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DireccionEntrega ValidDireccion() =>
        new("Corrientes", "1234", "CABA", "Buenos Aires", "C1043");

    private static Dinero Precio100() => new(100m);
    private static PorcentajeIVA Iva21() => new(AlicuotaIVA.General);

    private static IReadOnlyList<LineaRecetaSnapshot> SingleSnapshot() =>
        [new LineaRecetaSnapshot(Guid.NewGuid(), new Cantidad(1m, UnidadDeMedida.Kilogramo))];

    private static Pedido PedidoTakeAway() =>
        Pedido.Crear(TipoPedido.TakeAway, null, null, null, DateTime.UtcNow);

    private static Pedido PedidoDelivery() =>
        Pedido.Crear(TipoPedido.Delivery, null, null, ValidDireccion(), DateTime.UtcNow);

    private static Pedido PedidoSalon() =>
        Pedido.Crear(TipoPedido.Salon, Guid.NewGuid(), null, null, DateTime.UtcNow);

    /// <summary>
    /// Adds a line to <paramref name="pedido"/> and immediately confirms its price.
    /// Returns the confirmed <see cref="LineaPedido"/> so tests can reference it.
    /// </summary>
    private static LineaPedido AgregarLineaConPrecio(Pedido pedido, Guid platoId, int cantidad = 1)
    {
        var linea = pedido.AgregarLinea(platoId, cantidad);
        linea.ConfirmarPrecio(Precio100(), Iva21());
        return linea;
    }

    // ── REQ-07: Pedido creation invariants ───────────────────────────────────

    [Fact]
    public void Crear_Salon_WithMesaId_InitialStateIsAbierto()
    {
        var pedido = PedidoSalon();

        pedido.Tipo.Should().Be(TipoPedido.Salon);
        pedido.Estado.Should().Be(EstadoPedido.Abierto);
        pedido.MesaId.Should().NotBeNull();
        pedido.DireccionEntrega.Should().BeNull();
    }

    [Fact]
    public void Crear_Salon_WithoutMesaId_ThrowsDomainException()
    {
        var act = () => Pedido.Crear(TipoPedido.Salon, null, null, null, DateTime.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*MesaId*");
    }

    [Fact]
    public void Crear_TakeAway_InitialStateIsCreado()
    {
        var pedido = PedidoTakeAway();

        pedido.Tipo.Should().Be(TipoPedido.TakeAway);
        pedido.Estado.Should().Be(EstadoPedido.Creado);
        pedido.MesaId.Should().BeNull();
    }

    [Fact]
    public void Crear_Delivery_WithoutDireccion_ThrowsDomainException()
    {
        var act = () => Pedido.Crear(TipoPedido.Delivery, null, null, null, DateTime.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*DireccionEntrega*");
    }

    [Fact]
    public void Crear_Delivery_WithDireccion_SnapshotIsFrozen()
    {
        var pedido = PedidoDelivery();

        pedido.DireccionEntrega.Should().NotBeNull();
        pedido.DireccionEntrega!.Calle.Should().Be("Corrientes");
    }

    [Fact]
    public void Crear_NonSalon_WithMesaId_ThrowsDomainException()
    {
        var act = () => Pedido.Crear(TipoPedido.TakeAway, Guid.NewGuid(), null, null, DateTime.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*MesaId*null*");
    }

    [Fact]
    public void Crear_NonDelivery_WithDireccion_ThrowsDomainException()
    {
        var act = () => Pedido.Crear(TipoPedido.TakeAway, null, null, ValidDireccion(), DateTime.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*DireccionEntrega*Delivery*");
    }

    // ── REQ-15: PedidoCreado and LineaPedidoAgregada events ──────────────────

    [Fact]
    public void Crear_RaisesPedidoCreadoEvent()
    {
        var pedido = PedidoTakeAway();

        pedido.DomainEvents.OfType<PedidoCreado>().Should().HaveCount(1);
        var evt = pedido.DomainEvents.OfType<PedidoCreado>().Single();
        evt.PedidoId.Should().Be(pedido.Id);
        evt.Tipo.Should().Be(TipoPedido.TakeAway);
    }

    [Fact]
    public void AgregarLinea_RaisesLineaPedidoAgregadaEvent()
    {
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        pedido.ClearDomainEvents();

        var linea = pedido.AgregarLinea(platoId, 2);

        var evt = pedido.DomainEvents.OfType<LineaPedidoAgregada>().SingleOrDefault();
        evt.Should().NotBeNull();
        evt!.PedidoId.Should().Be(pedido.Id);
        evt.LineaPedidoId.Should().Be(linea.Id);
        evt.PlatoId.Should().Be(platoId);
        evt.Cantidad.Should().Be(2);
    }

    // ── DireccionEntrega immutability (REQ-07) ────────────────────────────────

    [Fact]
    public void DireccionEntrega_IsValueObject_StructuralEquality()
    {
        var a = new DireccionEntrega("Lavalle", "200", "CABA", "Buenos Aires", "C1047");
        var b = new DireccionEntrega("Lavalle", "200", "CABA", "Buenos Aires", "C1047");
        var c = new DireccionEntrega("Florida", "500", "CABA", "Buenos Aires", "C1005");

        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void DireccionEntrega_EmptyCalle_ThrowsDomainException()
    {
        var act = () => new DireccionEntrega("", "1", "Ciudad", "Prov", "1234");
        act.Should().Throw<DomainException>().WithMessage("*Calle*");
    }

    // ── REQ-09: PedidoTransicionRegistry ─────────────────────────────────────

    [Fact]
    public void Registry_ValidSalonTransition_ReturnsRow()
    {
        var row = PedidoTransicionRegistry.Buscar(
            TipoPedido.Salon, EstadoPedido.Abierto, EstadoPedido.Cerrado);

        row.Should().NotBeNull();
        row!.RolesPermitidos.Should().Contain(RolUsuario.Mozo);
    }

    [Fact]
    public void Registry_InvalidSalonTransition_ReturnsNull()
    {
        var row = PedidoTransicionRegistry.Buscar(
            TipoPedido.Salon, EstadoPedido.Abierto, EstadoPedido.Entregado);

        row.Should().BeNull();
    }

    [Fact]
    public void Registry_TakeAway_CreatedToModificado_ReturnsRow()
    {
        var row = PedidoTransicionRegistry.Buscar(
            TipoPedido.TakeAway, EstadoPedido.Creado, EstadoPedido.Modificado);

        row.Should().NotBeNull();
    }

    [Fact]
    public void Registry_Delivery_PreparandoseToListo_RequiresCocinero()
    {
        var row = PedidoTransicionRegistry.Buscar(
            TipoPedido.Delivery, EstadoPedido.Preparandose, EstadoPedido.ListoParaEntregar);

        row.Should().NotBeNull();
        row!.RolesPermitidos.Should().Contain(RolUsuario.Cocinero);
        row.RolesPermitidos.Should().NotContain(RolUsuario.Cajero);
    }

    [Fact]
    public void TransicionarEstado_ValidTransition_ChangesState_AndRaisesEvent()
    {
        var pedido = PedidoTakeAway();

        pedido.TransicionarEstado(EstadoPedido.Modificado, RolUsuario.Cajero);

        pedido.Estado.Should().Be(EstadoPedido.Modificado);
        pedido.DomainEvents.Should().Contain(e => e is PedidoEstadoCambiado);
        var evt = (PedidoEstadoCambiado)pedido.DomainEvents.Single(e => e is PedidoEstadoCambiado);
        evt.EstadoAnterior.Should().Be(EstadoPedido.Creado);
        evt.EstadoNuevo.Should().Be(EstadoPedido.Modificado);
    }

    [Fact]
    public void TransicionarEstado_InvalidTransition_ThrowsDomainException()
    {
        var pedido = PedidoTakeAway();

        var act = () => pedido.TransicionarEstado(EstadoPedido.Entregado, RolUsuario.Cajero);
        act.Should().Throw<DomainException>().WithMessage("*not valid*");
    }

    [Fact]
    public void TransicionarEstado_WrongRole_ThrowsDomainException()
    {
        // A TakeAway order in Preparandose: only Cocinero/Administrador may advance it to
        // ListoParaEntregar (registry row). A Cajero/Ventas role must be rejected.
        var pedido = PedidoTakeAway();
        pedido.TransicionarEstado(EstadoPedido.Preparandose, RolUsuario.Cajero);

        // Cajero is NOT in the Cocinero-only gate for Preparandose → ListoParaEntregar.
        var act = () => pedido.TransicionarEstado(EstadoPedido.ListoParaEntregar, RolUsuario.Cajero);
        act.Should().Throw<DomainException>().WithMessage("*not authorised*");
    }

    [Fact]
    public void TransicionarEstado_FromTerminalState_ThrowsDomainException()
    {
        var pedido = PedidoTakeAway();
        pedido.TransicionarEstado(EstadoPedido.Cancelado, RolUsuario.Cajero);

        var act = () => pedido.TransicionarEstado(EstadoPedido.Modificado, RolUsuario.Cajero);
        act.Should().Throw<DomainException>().WithMessage("*terminal state*");
    }

    // ── REQ-08: LineaPedido price snapshot + edit-lock ────────────────────────

    [Fact]
    public void ConfirmarPrecio_SetOnce_CalculatesTotals()
    {
        var pedido = PedidoTakeAway();
        var linea = pedido.AgregarLinea(Guid.NewGuid(), 2);

        linea.ConfirmarPrecio(Precio100(), Iva21());

        linea.PrecioUnitario!.Monto.Should().Be(100m);
        linea.SubtotalLinea!.Monto.Should().Be(200m);        // 100 * 2
        linea.IVALinea!.Monto.Should().Be(42m);              // 200 * 0.21
        linea.TotalLinea!.Monto.Should().Be(242m);           // 200 + 42
    }

    [Fact]
    public void ConfirmarPrecio_CalledTwice_ThrowsDomainException()
    {
        var pedido = PedidoTakeAway();
        var linea = pedido.AgregarLinea(Guid.NewGuid(), 1);
        linea.ConfirmarPrecio(Precio100(), Iva21());

        var act = () => linea.ConfirmarPrecio(new Dinero(50m), Iva21());
        act.Should().Throw<DomainException>().WithMessage("*set-once*");
    }

    [Fact]
    public void LineaTotals_BeforeConfirmar_AreNull()
    {
        var pedido = PedidoTakeAway();
        var linea = pedido.AgregarLinea(Guid.NewGuid(), 3);

        linea.SubtotalLinea.Should().BeNull();
        linea.IVALinea.Should().BeNull();
        linea.TotalLinea.Should().BeNull();
    }

    [Fact]
    public void AgregarLinea_OnTerminalPedido_ThrowsDomainException()
    {
        var pedido = PedidoTakeAway();
        pedido.TransicionarEstado(EstadoPedido.Cancelado, RolUsuario.Cajero);

        var act = () => pedido.AgregarLinea(Guid.NewGuid(), 1);
        act.Should().Throw<DomainException>().WithMessage("*terminal state*");
    }

    // ── REQ-10: OrdenTrabajo — all-or-nothing + duplicate block + auto-advance ─

    private static IReadOnlyDictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        BuildRecetaMap(params Guid[] platoIds)
    {
        return platoIds.ToDictionary(
            id => id,
            id => (IReadOnlyList<LineaRecetaSnapshot>)SingleSnapshot());
    }

    [Fact]
    public void GenerarOTs_AllLinesHaveReceta_CreatesOneOTPerLine()
    {
        var pedido = PedidoTakeAway();
        var platoId1 = Guid.NewGuid();
        var platoId2 = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId1);
        AgregarLineaConPrecio(pedido, platoId2, 2);

        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId1, platoId2));

        pedido.OrdenesTrabajo.Should().HaveCount(2);
        pedido.OrdenesTrabajo.All(ot => ot.Estado == EstadoOT.Creada).Should().BeTrue();
        pedido.DomainEvents.OfType<OrdenTrabajoCreada>().Should().HaveCount(2);
    }

    [Fact]
    public void GenerarOTs_MissingRecetaForOneLine_ThrowsAndCreatesNoOT()
    {
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);

        var emptyMap = new Dictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>();
        var act = () => pedido.GenerarOrdenesTrabajo(emptyMap);

        act.Should().Throw<DomainException>().WithMessage("*recipe snapshot*");
        pedido.OrdenesTrabajo.Should().BeEmpty(); // all-or-nothing
    }

    [Fact]
    public void GenerarOTs_DuplicateOT_ThrowsDomainException()
    {
        // Duplicate is per LineaPedidoId — same line attempted twice.
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));

        // Attempt to generate again for the same line
        var act = () => pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));
        act.Should().Throw<DomainException>().WithMessage("*already exists*");
    }

    [Fact]
    public void GenerarOTs_TwoLinesWithSamePlato_CreatesTwoOTs()
    {
        // REQ-10 / Scenario 10-B: two distinct lines ordering the same Plato must
        // each produce their own OT — the duplicate check is per LineaPedidoId, not PlatoId.
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);       // line 1 for platoId
        AgregarLineaConPrecio(pedido, platoId, 2);    // line 2 for the same platoId

        var act = () => pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));
        act.Should().NotThrow();

        pedido.OrdenesTrabajo.Should().HaveCount(2);
        pedido.OrdenesTrabajo.All(ot => ot.PlatoId == platoId).Should().BeTrue();
    }

    [Fact]
    public void GenerarOTs_LineWithoutConfirmedPrice_ThrowsDomainException()
    {
        // WARNING 6: GenerarOrdenesTrabajo must enforce that every line has a confirmed price.
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        pedido.AgregarLinea(platoId, 1); // price NOT confirmed

        var act = () => pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));
        act.Should().Throw<DomainException>().WithMessage("*confirmed price*");
        pedido.OrdenesTrabajo.Should().BeEmpty(); // all-or-nothing
    }

    [Fact]
    public void MarcarOTLista_AllOTsLista_AutoAdvancesToListoParaEntregar()
    {
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));
        pedido.TransicionarEstado(EstadoPedido.Preparandose, RolUsuario.Cajero);

        var ot = pedido.OrdenesTrabajo.Single();
        ot.AsignarCocinero(new LegajoId(Guid.NewGuid())); // → Preparandose
        pedido.MarcarOrdenTrabajoLista(ot.Id, RolUsuario.Cocinero);

        pedido.Estado.Should().Be(EstadoPedido.ListoParaEntregar);
    }

    [Fact]
    public void MarcarOTLista_NotAllOTsLista_DoesNotAutoAdvance()
    {
        var pedido = PedidoTakeAway();
        var plato1 = Guid.NewGuid();
        var plato2 = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, plato1);
        AgregarLineaConPrecio(pedido, plato2);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(plato1, plato2));
        pedido.TransicionarEstado(EstadoPedido.Preparandose, RolUsuario.Cajero);

        var ot1 = pedido.OrdenesTrabajo.First();
        ot1.AsignarCocinero(new LegajoId(Guid.NewGuid()));
        pedido.MarcarOrdenTrabajoLista(ot1.Id, RolUsuario.Cocinero);

        // Only one of two OTs is Lista → should NOT advance
        pedido.Estado.Should().Be(EstadoPedido.Preparandose);
    }

    [Fact]
    public void AsignarCocinero_MovesOTToPreparandose()
    {
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));

        var ot = pedido.OrdenesTrabajo.Single();
        ot.AsignarCocinero(new LegajoId(Guid.NewGuid()));

        ot.Estado.Should().Be(EstadoOT.Preparandose);
        ot.CocineroAsignado.Should().NotBeNull();
    }

    [Fact]
    public void AsignarCocinero_WhenNotCreada_ThrowsDomainException()
    {
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));

        var ot = pedido.OrdenesTrabajo.Single();
        ot.AsignarCocinero(new LegajoId(Guid.NewGuid())); // now Preparandose

        var act = () => ot.AsignarCocinero(new LegajoId(Guid.NewGuid()));
        act.Should().Throw<DomainException>().WithMessage("*Creada*");
    }

    // ── REQ-11: Cancellation cascade with stock restoration events ───────────

    [Fact]
    public void Cancelar_WithCreadaOT_RaisesStockDebeRestaurarse()
    {
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));
        pedido.ClearDomainEvents();

        pedido.TransicionarEstado(EstadoPedido.Cancelado, RolUsuario.Cajero);

        pedido.DomainEvents.OfType<StockDebeRestaurarse>().Should().HaveCount(1);
        pedido.DomainEvents.OfType<PedidoCancelado>().Should().HaveCount(1);
    }

    [Fact]
    public void Cancelar_WithPreparandoseOT_DoesNotRaiseStockRestore()
    {
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));

        var ot = pedido.OrdenesTrabajo.Single();
        ot.AsignarCocinero(new LegajoId(Guid.NewGuid())); // OT → Preparandose
        pedido.ClearDomainEvents();

        pedido.TransicionarEstado(EstadoPedido.Cancelado, RolUsuario.Cajero);

        pedido.DomainEvents.OfType<StockDebeRestaurarse>().Should().BeEmpty();
        pedido.DomainEvents.OfType<PedidoCancelado>().Should().HaveCount(1);
        pedido.OrdenesTrabajo.Single().Estado.Should().Be(EstadoOT.Cancelada);
    }

    [Fact]
    public void Cancelar_WithListaOT_TransitionsOTToCancelada_AndDoesNotRaiseStockRestoreForIt()
    {
        // BLOCKER 1 fix: an OT in Lista must also become Cancelada on Pedido cancel.
        // No stock restoration event is raised for the Lista OT — stock is already consumed.
        // Setup: one OT advances to Lista, one stays Creada (prevents auto-advance so the
        // Pedido remains in Preparandose and can still be cancelled via that path).
        // After cancel: ot1 (Lista→Cancelada, no restore), ot2 (Creada→Cancelada, restore raised).
        var pedido = PedidoTakeAway();
        var plato1 = Guid.NewGuid();
        var plato2 = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, plato1);
        AgregarLineaConPrecio(pedido, plato2);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(plato1, plato2));
        pedido.TransicionarEstado(EstadoPedido.Preparandose, RolUsuario.Cajero);

        var ot1 = pedido.OrdenesTrabajo.First();
        var ot2 = pedido.OrdenesTrabajo.Last();

        // Advance ot1 to Lista; ot2 stays Creada so Pedido doesn't auto-advance.
        ot1.AsignarCocinero(new LegajoId(Guid.NewGuid())); // ot1 → Preparandose
        pedido.MarcarOrdenTrabajoLista(ot1.Id, RolUsuario.Cocinero); // ot1 → Lista

        ot1.Estado.Should().Be(EstadoOT.Lista);        // pre-condition
        ot2.Estado.Should().Be(EstadoOT.Creada);       // pre-condition
        pedido.Estado.Should().Be(EstadoPedido.Preparandose); // not auto-advanced

        pedido.ClearDomainEvents();
        pedido.TransicionarEstado(EstadoPedido.Cancelado, RolUsuario.Cajero);

        // Both OTs are now Cancelada.
        ot1.Estado.Should().Be(EstadoOT.Cancelada);
        ot2.Estado.Should().Be(EstadoOT.Cancelada);

        // Only ot2 (was Creada) raised StockDebeRestaurarse — ot1 (was Lista) did not.
        var restoreEvents = pedido.DomainEvents.OfType<StockDebeRestaurarse>().ToList();
        restoreEvents.Should().HaveCount(1);
        restoreEvents[0].OrdenTrabajoId.Should().Be(ot2.Id);

        pedido.DomainEvents.OfType<PedidoCancelado>().Should().HaveCount(1);
    }

    [Fact]
    public void Cancelar_WithoutOTs_RaisesPedidoCanceladoOnly()
    {
        var pedido = PedidoTakeAway();
        pedido.ClearDomainEvents();

        pedido.TransicionarEstado(EstadoPedido.Cancelado, RolUsuario.Cajero);

        pedido.DomainEvents.OfType<PedidoCancelado>().Should().HaveCount(1);
        pedido.DomainEvents.OfType<StockDebeRestaurarse>().Should().BeEmpty();
    }

    [Fact]
    public void Cancelar_AllOTsSetToCancelada()
    {
        var pedido = PedidoTakeAway();
        var plato1 = Guid.NewGuid();
        var plato2 = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, plato1);
        AgregarLineaConPrecio(pedido, plato2);
        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(plato1, plato2));

        pedido.TransicionarEstado(EstadoPedido.Cancelado, RolUsuario.Cajero);

        pedido.OrdenesTrabajo.All(ot => ot.Estado == EstadoOT.Cancelada).Should().BeTrue();
    }

    // ── Pedido RowVersion ─────────────────────────────────────────────────────

    [Fact]
    public void Pedido_RowVersion_IsInitializedAsEmptyArray()
    {
        var pedido = PedidoTakeAway();
        pedido.RowVersion.Should().NotBeNull();
        pedido.RowVersion.Should().BeEmpty();
    }

    // ── OT recipe snapshot ────────────────────────────────────────────────────

    [Fact]
    public void GenerarOTs_RecetaSnapshot_IsStoredOnOT()
    {
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        var ingredienteId = Guid.NewGuid();
        AgregarLineaConPrecio(pedido, platoId);

        var snapshot = new List<LineaRecetaSnapshot>
        {
            new(ingredienteId, new Cantidad(250m, UnidadDeMedida.Gramo))
        };

        pedido.GenerarOrdenesTrabajo(new Dictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        {
            { platoId, snapshot }
        });

        var ot = pedido.OrdenesTrabajo.Single();
        ot.RecetaSnapshot.Should().HaveCount(1);
        ot.RecetaSnapshot[0].IngredienteId.Should().Be(ingredienteId);
        ot.RecetaSnapshot[0].Cantidad.Valor.Should().Be(250m);
    }

    // ── OT LineaPedidoId keying ───────────────────────────────────────────────

    [Fact]
    public void GenerarOTs_OT_HasCorrectLineaPedidoId()
    {
        // The OT must carry the LineaPedidoId of the originating line.
        var pedido = PedidoTakeAway();
        var platoId = Guid.NewGuid();
        var linea = AgregarLineaConPrecio(pedido, platoId);

        pedido.GenerarOrdenesTrabajo(BuildRecetaMap(platoId));

        var ot = pedido.OrdenesTrabajo.Single();
        ot.LineaPedidoId.Should().Be(linea.Id);
    }

    // ── Salon auto-advance guard (Salon does NOT auto-advance to ListoParaEntregar) ─

    [Fact]
    public void Salon_TransicionarEstado_ToClosedWithMozo_Succeeds()
    {
        var pedido = PedidoSalon();

        pedido.TransicionarEstado(EstadoPedido.Cerrado, RolUsuario.Mozo);

        pedido.Estado.Should().Be(EstadoPedido.Cerrado);
    }

    [Fact]
    public void Salon_TransicionarEstado_ToClosedWithAdministrador_Succeeds()
    {
        var pedido = PedidoSalon();

        pedido.TransicionarEstado(EstadoPedido.Cerrado, RolUsuario.Administrador);

        pedido.Estado.Should().Be(EstadoPedido.Cerrado);
    }
}
