using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Mesas.DesactivarMesa;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Mesas;
using NSubstitute;

namespace GastroGestion.Application.Tests.Mesas;

/// <summary>Unit tests for DesactivarMesaHandler.</summary>
public sealed class DesactivarMesaHandlerTests
{
    private readonly IMesaRepository _mesas = Substitute.For<IMesaRepository>();
    private readonly IUnitOfWork     _uow   = Substitute.For<IUnitOfWork>();
    private readonly DesactivarMesaHandler _sut;

    public DesactivarMesaHandlerTests()
        => _sut = new DesactivarMesaHandler(_mesas, _uow);

    private static Mesa BuildActiveMesa()
        => Mesa.Crear(1, 4);

    [Fact]
    public async Task Handle_FreeMesa_SetsActivaFalseAndSaves()
    {
        var mesa = BuildActiveMesa();
        _mesas.GetByIdAsync(mesa.Id, Arg.Any<CancellationToken>()).Returns(mesa);

        await _sut.Handle(new DesactivarMesaCommand(mesa.Id));

        mesa.Activa.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyInactiveMesa_IsIdempotentAndSaves()
    {
        var mesa = BuildActiveMesa();
        mesa.Desactivar();
        _mesas.GetByIdAsync(mesa.Id, Arg.Any<CancellationToken>()).Returns(mesa);

        var act = async () => await _sut.Handle(new DesactivarMesaCommand(mesa.Id));
        await act.Should().NotThrowAsync();

        mesa.Activa.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MesaNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mesas.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Mesa?)null);

        var act = async () => await _sut.Handle(new DesactivarMesaCommand(id));

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MesaWithActivePedido_ThrowsDomainException()
    {
        var mesa = BuildActiveMesa();
        mesa.AsignarPedido(Guid.NewGuid()); // table now has an active Pedido
        _mesas.GetByIdAsync(mesa.Id, Arg.Any<CancellationToken>()).Returns(mesa);

        var act = async () => await _sut.Handle(new DesactivarMesaCommand(mesa.Id));

        await act.Should().ThrowAsync<DomainException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
