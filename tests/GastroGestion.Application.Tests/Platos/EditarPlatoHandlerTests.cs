using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Platos.EditarPlato;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;
using NSubstitute;

namespace GastroGestion.Application.Tests.Platos;

/// <summary>
/// Unit tests for EditarPlatoHandler. All collaborators are mocked with NSubstitute.
/// </summary>
public sealed class EditarPlatoHandlerTests
{
    private readonly IPlatoRepository _platos = Substitute.For<IPlatoRepository>();
    private readonly IUnitOfWork      _uow    = Substitute.For<IUnitOfWork>();
    private readonly EditarPlatoHandler _sut;

    public EditarPlatoHandlerTests()
        => _sut = new EditarPlatoHandler(_platos, _uow);

    private static Plato BuildPlato(string nombre = "Milanesa", decimal precio = 1000m)
        => Plato.Crear(nombre, new Dinero(precio), AlicuotaIVA.General);

    [Fact]
    public async Task Handle_HappyPath_UpdatesNombreAndPrecioAndSaves()
    {
        var plato = BuildPlato("Milanesa", 1000m);
        var cmd   = new EditarPlatoCommand(plato.Id, "Milanesa Napolitana", 1500m);
        _platos.GetByIdAsync(plato.Id, Arg.Any<CancellationToken>()).Returns(plato);

        var result = await _sut.Handle(cmd);

        result.Nombre.Should().Be("Milanesa Napolitana");
        result.PrecioBase.Monto.Should().Be(1500m);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PlatoNotFound_ThrowsNotFoundException()
    {
        var id  = Guid.NewGuid();
        var cmd = new EditarPlatoCommand(id, "Nuevo", 100m);
        _platos.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Plato?)null);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyNombre_ThrowsDomainException()
    {
        var plato = BuildPlato();
        var cmd   = new EditarPlatoCommand(plato.Id, "", 100m);
        _platos.GetByIdAsync(plato.Id, Arg.Any<CancellationToken>()).Returns(plato);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<DomainException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
