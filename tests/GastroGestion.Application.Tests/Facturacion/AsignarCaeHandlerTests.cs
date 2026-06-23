using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Facturacion.AsignarCae;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.ValueObjects;
using NSubstitute;

namespace GastroGestion.Application.Tests.Facturacion;

/// <summary>
/// Unit tests for AsignarCaeHandler.
/// All collaborators are mocked with NSubstitute — no database involved.
/// </summary>
public sealed class AsignarCaeHandlerTests
{
    private readonly IFacturaRepository _facturas = Substitute.For<IFacturaRepository>();
    private readonly IUnitOfWork        _uow      = Substitute.For<IUnitOfWork>();
    private readonly AsignarCaeHandler  _sut;

    private static readonly Guid ClienteId = Guid.NewGuid();

    public AsignarCaeHandlerTests()
        => _sut = new AsignarCaeHandler(_facturas, _uow);

    private static List<FacturaLinea> LineasConIVA() =>
    [
        new FacturaLinea(Guid.NewGuid(), Guid.NewGuid(), new Dinero(100m), new PorcentajeIVA(AlicuotaIVA.General), 2)
    ];

    private static Factura BuildFacturaElectronica()
        => Factura.CrearFacturaElectronica(ClienteId, [Guid.NewGuid()], LineasConIVA());

    private static Factura BuildTicket()
        => Factura.CrearTicket(ClienteId, [Guid.NewGuid()],
            [new FacturaLinea(Guid.NewGuid(), Guid.NewGuid(), new Dinero(100m), PorcentajeIVA.Cero, 1)]);

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_FacturaElectronica_PersistsCaeAndVencimiento()
    {
        var factura     = BuildFacturaElectronica();
        var cae         = "12345678901234";
        var vencimiento = new DateOnly(2027, 1, 1);
        var cmd         = new AsignarCaeCommand(factura.Id, cae, vencimiento);

        _facturas.GetByIdAsync(factura.Id, Arg.Any<CancellationToken>()).Returns(factura);

        await _sut.Handle(cmd);

        factura.CAE.Should().Be(cae);
        factura.VencimientoCAE.Should().Be(vencimiento);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_FacturaNotFound_ThrowsNotFoundException()
    {
        var id  = Guid.NewGuid();
        var cmd = new AsignarCaeCommand(id, "12345678901234", new DateOnly(2027, 1, 1));

        _facturas.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Factura?)null);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Non-electronic factura throws ─────────────────────────────────────────

    [Fact]
    public async Task Handle_Ticket_ThrowsDomainException()
    {
        var factura = BuildTicket();
        var cmd     = new AsignarCaeCommand(factura.Id, "12345678901234", new DateOnly(2027, 1, 1));

        _facturas.GetByIdAsync(factura.Id, Arg.Any<CancellationToken>()).Returns(factura);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*TicketInterno*");
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Set-once guard ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CaeAlreadyAssigned_ThrowsDomainException()
    {
        var factura = BuildFacturaElectronica();
        factura.AsignarCae("12345678901234", new DateOnly(2027, 1, 1));

        var cmd = new AsignarCaeCommand(factura.Id, "99999999999999", new DateOnly(2027, 6, 1));

        _facturas.GetByIdAsync(factura.Id, Arg.Any<CancellationToken>()).Returns(factura);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already assigned*");
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
