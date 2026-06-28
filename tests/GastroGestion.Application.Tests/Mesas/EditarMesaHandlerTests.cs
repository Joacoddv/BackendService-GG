using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Mesas.EditarMesa;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Mesas;
using NSubstitute;

namespace GastroGestion.Application.Tests.Mesas;

/// <summary>Unit tests for EditarMesaHandler. All collaborators are mocked with NSubstitute.</summary>
public sealed class EditarMesaHandlerTests
{
    private readonly IMesaRepository _mesas = Substitute.For<IMesaRepository>();
    private readonly IUnitOfWork     _uow   = Substitute.For<IUnitOfWork>();
    private readonly EditarMesaHandler _sut;

    public EditarMesaHandlerTests()
        => _sut = new EditarMesaHandler(_mesas, _uow);

    private static Mesa BuildMesa(int numero = 1, int capacidad = 4)
        => Mesa.Crear(numero, capacidad);

    [Fact]
    public async Task Handle_HappyPath_UpdatesNumeroAndCapacidadAndSaves()
    {
        var mesa = BuildMesa(1, 4);
        var cmd  = new EditarMesaCommand(mesa.Id, 5, 8);
        _mesas.GetByIdAsync(mesa.Id, Arg.Any<CancellationToken>()).Returns(mesa);
        _mesas.NumeroExistsForOtherAsync(5, mesa.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(cmd);

        result.Numero.Should().Be(5);
        result.Capacidad.Should().Be(8);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MesaNotFound_ThrowsNotFoundException()
    {
        var id  = Guid.NewGuid();
        var cmd = new EditarMesaCommand(id, 2, 4);
        _mesas.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Mesa?)null);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NumeroConflict_ThrowsConflictException()
    {
        var mesa = BuildMesa(1, 4);
        var cmd  = new EditarMesaCommand(mesa.Id, 7, 4);
        _mesas.GetByIdAsync(mesa.Id, Arg.Any<CancellationToken>()).Returns(mesa);
        _mesas.NumeroExistsForOtherAsync(7, mesa.Id, Arg.Any<CancellationToken>()).Returns(true);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<ConflictException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidNumero_ThrowsDomainException()
    {
        var mesa = BuildMesa(1, 4);
        var cmd  = new EditarMesaCommand(mesa.Id, 0, 4);
        _mesas.GetByIdAsync(mesa.Id, Arg.Any<CancellationToken>()).Returns(mesa);
        _mesas.NumeroExistsForOtherAsync(0, mesa.Id, Arg.Any<CancellationToken>()).Returns(false);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<DomainException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
