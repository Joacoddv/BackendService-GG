using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Mesas.UbicarMesa;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Mesas;
using NSubstitute;

namespace GastroGestion.Application.Tests.Mesas;

/// <summary>Unit tests for UbicarMesaHandler. All collaborators are mocked with NSubstitute.</summary>
public sealed class UbicarMesaHandlerTests
{
    private readonly IMesaRepository  _mesas = Substitute.For<IMesaRepository>();
    private readonly IUnitOfWork      _uow   = Substitute.For<IUnitOfWork>();
    private readonly UbicarMesaHandler _sut;

    public UbicarMesaHandlerTests()
        => _sut = new UbicarMesaHandler(_mesas, _uow);

    private static Mesa BuildMesa(int numero = 1, int capacidad = 4)
        => Mesa.Crear(numero, capacidad);

    [Fact]
    public async Task Handle_HappyPath_SetsPosicionAndSaves()
    {
        var mesa = BuildMesa();
        var cmd  = new UbicarMesaCommand(mesa.Id, 100, 200);
        _mesas.GetByIdAsync(mesa.Id, Arg.Any<CancellationToken>()).Returns(mesa);

        var result = await _sut.Handle(cmd);

        result.PosicionX.Should().Be(100);
        result.PosicionY.Should().Be(200);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MesaNotFound_ThrowsNotFoundException()
    {
        var id  = Guid.NewGuid();
        var cmd = new UbicarMesaCommand(id, 10, 20);
        _mesas.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Mesa?)null);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeX_ThrowsDomainExceptionAndDoesNotSave()
    {
        var mesa = BuildMesa();
        var cmd  = new UbicarMesaCommand(mesa.Id, -1, 20);
        _mesas.GetByIdAsync(mesa.Id, Arg.Any<CancellationToken>()).Returns(mesa);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<DomainException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
