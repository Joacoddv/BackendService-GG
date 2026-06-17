using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Realtime;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Pedidos.AsignarCocinero;
using GastroGestion.Application.Pedidos.GenerarOrdenesTrabajo;
using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Application.Pedidos.MarcarOrdenTrabajoLista;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;
using NSubstitute;

namespace GastroGestion.Application.Tests;

/// <summary>
/// Unit tests for the kitchen workflow use-case handlers.
/// Covers OT-01-A..E (GenerarOrdenesTrabajoHandler), OT-02 (AsignarCocineroHandler),
/// OT-03 (MarcarOrdenTrabajoListaHandler), and OT-04/board (GetOrdenesByEstadoHandler).
/// All collaborators are mocked with NSubstitute — no database involved.
/// </summary>
public class GenerarOrdenesTrabajoHandlerTests
{
    // ── Shared mocks ──────────────────────────────────────────────────────────

    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly IPlatoRepository  _platos  = Substitute.For<IPlatoRepository>();
    private readonly IUnitOfWork       _uow     = Substitute.For<IUnitOfWork>();

    private readonly GenerarOrdenesTrabajoHandler _sut;

    public GenerarOrdenesTrabajoHandlerTests()
        => _sut = new GenerarOrdenesTrabajoHandler(_pedidos, _platos, _uow);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Pedido BuildPedidoWithPricedLine(Guid platoId, out Guid lineaId)
    {
        var pedido = Pedido.Crear(TipoPedido.TakeAway, null, null, null, DateTime.UtcNow);
        var linea  = pedido.AgregarLinea(platoId, 1);
        linea.ConfirmarPrecio(new Dinero(100m), new PorcentajeIVA(AlicuotaIVA.General));
        lineaId = linea.Id;
        return pedido;
    }

    private static Plato BuildPlatoWithRecipe(Guid platoId)
    {
        var plato = Plato.Crear("TestPlato", new Dinero(100m), AlicuotaIVA.General);
        // Use reflection to set the Id because Plato.Crear assigns a new Guid internally.
        // Instead, build via the domain factory and add a recipe line — we match by the
        // returned plato.Id in the mock, not the platoId passed in; so we return the plato
        // and let the test wire the mock using plato.Id.
        plato.AgregarLineaReceta(
            Guid.NewGuid(),
            new Cantidad(200m, UnidadDeMedida.Gramo));
        return plato;
    }

    // ── OT-01-A: happy path ────────────────────────────────────────────────────

    /// <summary>
    /// OT-01-A: Mozo with priced line + plato with recipe → handler completes,
    /// SaveChangesAsync called exactly once.
    /// </summary>
    [Fact]
    public async Task Handle_HappyPath_Mozo_CallsSaveChangesOnce()
    {
        var plato   = BuildPlatoWithRecipe(Guid.NewGuid());
        var pedido  = BuildPedidoWithPricedLine(plato.Id, out _);

        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);
        _platos.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
               .Returns(new List<Plato> { plato });

        var cmd = new GenerarOrdenesTrabajoCommand(pedido.Id, RolUsuario.Mozo);

        await _sut.Handle(cmd);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        pedido.OrdenesTrabajo.Should().HaveCount(1);
    }

    /// <summary>
    /// OT-01-A: Administrador role is also permitted to generate OTs.
    /// </summary>
    [Fact]
    public async Task Handle_HappyPath_Administrador_CallsSaveChangesOnce()
    {
        var plato  = BuildPlatoWithRecipe(Guid.NewGuid());
        var pedido = BuildPedidoWithPricedLine(plato.Id, out _);

        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);
        _platos.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
               .Returns(new List<Plato> { plato });

        var cmd = new GenerarOrdenesTrabajoCommand(pedido.Id, RolUsuario.Administrador);

        await _sut.Handle(cmd);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── OT-01-B: empty recipe → ValidationException ────────────────────────────

    /// <summary>
    /// OT-01-B: Plato with empty LineasReceta → ValidationException thrown before domain call;
    /// SaveChangesAsync must NOT be called.
    /// </summary>
    [Fact]
    public async Task Handle_EmptyRecipe_ThrowsValidationException_NoSave()
    {
        var platoId = Guid.NewGuid();
        var pedido  = BuildPedidoWithPricedLine(platoId, out _);

        // Plato with no recipe lines
        var platoNoRecipe = Plato.Crear("NoRecipe", new Dinero(50m), AlicuotaIVA.General);
        // Wire mock so GetByIdsAsync returns this plato for the platoId used by the pedido.
        // Because Plato.Crear assigns its own Id, we need a plato whose Id matches platoId.
        // We build a pedido around the plato's actual Id instead.
        var platoEmpty = Plato.Crear("EmptyReceta", new Dinero(50m), AlicuotaIVA.General);
        var pedido2    = BuildPedidoWithPricedLine(platoEmpty.Id, out _);

        _pedidos.GetByIdAsync(pedido2.Id).Returns(pedido2);
        _platos.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
               .Returns(new List<Plato> { platoEmpty }); // no recipe lines

        var cmd = new GenerarOrdenesTrabajoCommand(pedido2.Id, RolUsuario.Mozo);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<ValidationException>()
                          .WithMessage("*recipe*");
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── OT-01-C: unconfirmed price → ValidationException ──────────────────────

    /// <summary>
    /// OT-01-C: Line without confirmed price → ValidationException;
    /// SaveChangesAsync must NOT be called.
    /// </summary>
    [Fact]
    public async Task Handle_UnconfirmedPrice_ThrowsValidationException_NoSave()
    {
        var platoId = Guid.NewGuid();
        // Build pedido with a line that does NOT have a confirmed price
        var pedido = Pedido.Crear(TipoPedido.TakeAway, null, null, null, DateTime.UtcNow);
        pedido.AgregarLinea(platoId, 1); // no ConfirmarPrecio call

        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);

        // Plato needs a recipe but the price check fires first
        var plato = Plato.Crear("PlatoUnpriced", new Dinero(50m), AlicuotaIVA.General);
        plato.AgregarLineaReceta(Guid.NewGuid(), new Cantidad(1m, UnidadDeMedida.Kilogramo));
        _platos.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
               .Returns(new List<Plato> { plato });

        var cmd = new GenerarOrdenesTrabajoCommand(pedido.Id, RolUsuario.Mozo);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<ValidationException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── OT-01-D: OTs already exist → ConflictException ────────────────────────

    /// <summary>
    /// OT-01-D: Pedido already has OTs → ConflictException (no re-generation).
    /// </summary>
    [Fact]
    public async Task Handle_OTsAlreadyExist_ThrowsConflictException()
    {
        var plato  = BuildPlatoWithRecipe(Guid.NewGuid());
        var pedido = BuildPedidoWithPricedLine(plato.Id, out _);

        // Pre-generate OTs via domain
        var snapshotMap = new Dictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        {
            {
                plato.Id,
                plato.LineasReceta
                     .Select(lr => new LineaRecetaSnapshot(lr.IngredienteId, lr.Cantidad))
                     .ToList()
            }
        };
        pedido.GenerarOrdenesTrabajo(snapshotMap);

        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);
        _platos.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
               .Returns(new List<Plato> { plato });

        var cmd = new GenerarOrdenesTrabajoCommand(pedido.Id, RolUsuario.Mozo);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── OT-01-E: pedido not found → NotFoundException ─────────────────────────

    /// <summary>
    /// OT-01-E: Repository returns null → NotFoundException.
    /// </summary>
    [Fact]
    public async Task Handle_PedidoNotFound_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _pedidos.GetByIdAsync(missingId).Returns((Pedido?)null);

        var cmd = new GenerarOrdenesTrabajoCommand(missingId, RolUsuario.Mozo);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── OT-01 role gate: wrong role → ForbiddenException ──────────────────────

    /// <summary>
    /// Role gate: Cocinero is not allowed to generate OTs → ForbiddenException.
    /// </summary>
    [Fact]
    public async Task Handle_WrongRole_Cocinero_ThrowsForbiddenException()
    {
        var plato  = BuildPlatoWithRecipe(Guid.NewGuid());
        var pedido = BuildPedidoWithPricedLine(plato.Id, out _);

        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);
        _platos.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
               .Returns(new List<Plato> { plato });

        var cmd = new GenerarOrdenesTrabajoCommand(pedido.Id, RolUsuario.Cocinero);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Unit tests for AsignarCocineroHandler.
/// Covers OT-02 scenarios.
/// </summary>
public class AsignarCocineroHandlerTests
{
    private readonly IPedidoRepository   _pedidos         = Substitute.For<IPedidoRepository>();
    private readonly IUnitOfWork         _uow             = Substitute.For<IUnitOfWork>();
    private readonly IKitchenNotifier    _kitchenNotifier = Substitute.For<IKitchenNotifier>();
    private readonly AsignarCocineroHandler _sut;

    public AsignarCocineroHandlerTests()
        => _sut = new AsignarCocineroHandler(_pedidos, _uow, _kitchenNotifier);

    private static (Pedido pedido, Guid otId) SeedPedidoWithOT()
    {
        var pedido  = Pedido.Crear(TipoPedido.TakeAway, null, null, null, DateTime.UtcNow);
        var platoId = Guid.NewGuid();
        var linea   = pedido.AgregarLinea(platoId, 1);
        linea.ConfirmarPrecio(new Dinero(100m), new PorcentajeIVA(AlicuotaIVA.General));
        pedido.GenerarOrdenesTrabajo(new Dictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        {
            { platoId, [new LineaRecetaSnapshot(Guid.NewGuid(), new Cantidad(1m, UnidadDeMedida.Kilogramo))] }
        });
        return (pedido, pedido.OrdenesTrabajo.Single().Id);
    }

    /// <summary>
    /// OT-02-A: Cocinero assigns a cook → OT moves to Preparandose; SaveChanges called once.
    /// </summary>
    [Fact]
    public async Task Handle_HappyPath_Cocinero_SavesChanges()
    {
        var (pedido, otId) = SeedPedidoWithOT();
        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);

        var legajoId = Guid.NewGuid();
        var cmd = new AsignarCocineroCommand(pedido.Id, otId, legajoId, RolUsuario.Cocinero);

        await _sut.Handle(cmd);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        var ot = pedido.OrdenesTrabajo.Single();
        ot.Estado.Should().Be(EstadoOT.Preparandose);
        ot.CocineroAsignado!.Valor.Should().Be(legajoId);
    }

    /// <summary>
    /// OT-02-B: Mozo role → ForbiddenException; SaveChanges NOT called.
    /// </summary>
    [Fact]
    public async Task Handle_WrongRole_Mozo_ThrowsForbiddenException()
    {
        var (pedido, otId) = SeedPedidoWithOT();
        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);

        var cmd = new AsignarCocineroCommand(pedido.Id, otId, Guid.NewGuid(), RolUsuario.Mozo);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// OT-02-C: Pedido not found → NotFoundException.
    /// </summary>
    [Fact]
    public async Task Handle_PedidoNotFound_ThrowsNotFoundException()
    {
        _pedidos.GetByIdAsync(Arg.Any<Guid>()).Returns((Pedido?)null);

        var cmd = new AsignarCocineroCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), RolUsuario.Cocinero);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

/// <summary>
/// Unit tests for MarcarOrdenTrabajoListaHandler.
/// Covers OT-03 scenarios.
/// </summary>
public class MarcarOrdenTrabajoListaHandlerTests
{
    private readonly IPedidoRepository            _pedidos         = Substitute.For<IPedidoRepository>();
    private readonly IUnitOfWork                  _uow             = Substitute.For<IUnitOfWork>();
    private readonly IKitchenNotifier             _kitchenNotifier = Substitute.For<IKitchenNotifier>();
    private readonly MarcarOrdenTrabajoListaHandler _sut;

    public MarcarOrdenTrabajoListaHandlerTests()
        => _sut = new MarcarOrdenTrabajoListaHandler(_pedidos, _uow, _kitchenNotifier);

    private static (Pedido pedido, Guid otId) SeedPreparandosePedido()
    {
        var pedido  = Pedido.Crear(TipoPedido.TakeAway, null, null, null, DateTime.UtcNow);
        var platoId = Guid.NewGuid();
        var linea   = pedido.AgregarLinea(platoId, 1);
        linea.ConfirmarPrecio(new Dinero(100m), new PorcentajeIVA(AlicuotaIVA.General));
        pedido.GenerarOrdenesTrabajo(new Dictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        {
            { platoId, [new LineaRecetaSnapshot(Guid.NewGuid(), new Cantidad(1m, UnidadDeMedida.Kilogramo))] }
        });
        pedido.TransicionarEstado(EstadoPedido.Preparandose, RolUsuario.Cajero);
        var ot = pedido.OrdenesTrabajo.Single();
        pedido.AsignarCocineroAOT(ot.Id, new LegajoId(Guid.NewGuid()), RolUsuario.Cocinero);
        return (pedido, ot.Id);
    }

    /// <summary>
    /// OT-03-A: Cocinero marks last OT Lista → OT is Lista; SaveChanges called once.
    /// </summary>
    [Fact]
    public async Task Handle_HappyPath_Cocinero_MarksOtLista()
    {
        var (pedido, otId) = SeedPreparandosePedido();
        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);

        var cmd = new MarcarOrdenTrabajoListaCommand(pedido.Id, otId, RolUsuario.Cocinero);

        await _sut.Handle(cmd);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        pedido.OrdenesTrabajo.Single().Estado.Should().Be(EstadoOT.Lista);
    }

    /// <summary>
    /// OT-03-B: Cajero role → ForbiddenException; SaveChanges NOT called.
    /// </summary>
    [Fact]
    public async Task Handle_WrongRole_Cajero_ThrowsForbiddenException()
    {
        var (pedido, otId) = SeedPreparandosePedido();
        _pedidos.GetByIdAsync(pedido.Id).Returns(pedido);

        var cmd = new MarcarOrdenTrabajoListaCommand(pedido.Id, otId, RolUsuario.Cajero);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// OT-03-C: Pedido not found → NotFoundException.
    /// </summary>
    [Fact]
    public async Task Handle_PedidoNotFound_ThrowsNotFoundException()
    {
        _pedidos.GetByIdAsync(Arg.Any<Guid>()).Returns((Pedido?)null);

        var cmd = new MarcarOrdenTrabajoListaCommand(Guid.NewGuid(), Guid.NewGuid(), RolUsuario.Cocinero);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

/// <summary>
/// Unit tests for GetOrdenesByEstadoHandler.
/// Covers OT-04 board query scenarios.
/// </summary>
public class GetOrdenesByEstadoHandlerTests
{
    private readonly IPedidoRepository       _pedidos = Substitute.For<IPedidoRepository>();
    private readonly GetOrdenesByEstadoHandler _sut;

    public GetOrdenesByEstadoHandlerTests()
        => _sut = new GetOrdenesByEstadoHandler(_pedidos);

    /// <summary>
    /// OT-04-A: null estado filter → returns all items from repository.
    /// </summary>
    [Fact]
    public async Task Handle_NoFilter_ReturnsAllItems()
    {
        var items = new List<OrdenTrabajoBoardItem>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), TipoPedido.TakeAway, Guid.NewGuid(), Guid.NewGuid(), EstadoOT.Creada,   null),
            new(Guid.NewGuid(), Guid.NewGuid(), TipoPedido.Delivery, Guid.NewGuid(), Guid.NewGuid(), EstadoOT.Preparandose, Guid.NewGuid()),
        };
        _pedidos.GetAllOrdenesTrabajoAsync(null, Arg.Any<CancellationToken>())
                .Returns(items);

        var query = new GetOrdenesByEstadoQuery(null);
        var result = await _sut.Handle(query);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// OT-04-B: filtered by EstadoOT.Creada → repository receives that filter.
    /// </summary>
    [Fact]
    public async Task Handle_WithEstadoFilter_PassesFilterToRepository()
    {
        var items = new List<OrdenTrabajoBoardItem>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), TipoPedido.TakeAway, Guid.NewGuid(), Guid.NewGuid(), EstadoOT.Creada, null),
        };
        _pedidos.GetAllOrdenesTrabajoAsync(EstadoOT.Creada, Arg.Any<CancellationToken>())
                .Returns(items);

        var query = new GetOrdenesByEstadoQuery(EstadoOT.Creada);
        var result = await _sut.Handle(query);

        result.Should().HaveCount(1);
        result[0].Estado.Should().Be(EstadoOT.Creada);
        await _pedidos.Received(1).GetAllOrdenesTrabajoAsync(EstadoOT.Creada, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// OT-04-C: repository returns empty list → handler returns empty list (no exception).
    /// </summary>
    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        _pedidos.GetAllOrdenesTrabajoAsync(Arg.Any<EstadoOT?>(), Arg.Any<CancellationToken>())
                .Returns(new List<OrdenTrabajoBoardItem>());

        var result = await _sut.Handle(new GetOrdenesByEstadoQuery(EstadoOT.Lista));

        result.Should().BeEmpty();
    }
}
