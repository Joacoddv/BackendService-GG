using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Platos.DesactivarPlato;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;
using NSubstitute;

namespace GastroGestion.Application.Tests.Platos;

/// <summary>Unit tests for DesactivarPlatoHandler.</summary>
public sealed class DesactivarPlatoHandlerTests
{
    private readonly IPlatoRepository _platos = Substitute.For<IPlatoRepository>();
    private readonly IUnitOfWork      _uow    = Substitute.For<IUnitOfWork>();
    private readonly DesactivarPlatoHandler _sut;

    public DesactivarPlatoHandlerTests()
        => _sut = new DesactivarPlatoHandler(_platos, _uow);

    private static Plato BuildActivePlato()
        => Plato.Crear("Test Plato", new Dinero(500m), AlicuotaIVA.General);

    [Fact]
    public async Task Handle_ActivePlato_SetsActivoFalseAndSaves()
    {
        var plato = BuildActivePlato();
        _platos.GetByIdAsync(plato.Id, Arg.Any<CancellationToken>()).Returns(plato);

        await _sut.Handle(new DesactivarPlatoCommand(plato.Id));

        plato.Activo.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyInactivePlato_IsIdempotentAndSaves()
    {
        var plato = BuildActivePlato();
        plato.Desactivar();
        _platos.GetByIdAsync(plato.Id, Arg.Any<CancellationToken>()).Returns(plato);

        var act = async () => await _sut.Handle(new DesactivarPlatoCommand(plato.Id));
        await act.Should().NotThrowAsync();

        plato.Activo.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PlatoNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _platos.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Plato?)null);

        var act = async () => await _sut.Handle(new DesactivarPlatoCommand(id));

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
