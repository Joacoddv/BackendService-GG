using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Ingredientes.DesactivarIngrediente;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Ingredientes;
using NSubstitute;

namespace GastroGestion.Application.Tests.Ingredientes;

/// <summary>
/// Unit tests for DesactivarIngredienteHandler (CCC-T38, CCC-T49).
/// </summary>
public sealed class DesactivarIngredienteHandlerTests
{
    private readonly IIngredienteRepository    _ingredientes = Substitute.For<IIngredienteRepository>();
    private readonly IUnitOfWork               _uow          = Substitute.For<IUnitOfWork>();
    private readonly DesactivarIngredienteHandler _sut;

    public DesactivarIngredienteHandlerTests()
        => _sut = new DesactivarIngredienteHandler(_ingredientes, _uow);

    private static Ingrediente BuildActiveIngrediente()
        => Ingrediente.Crear("Test Ingrediente", UnidadDeMedida.Gramo);

    /// <summary>CCC-C02 happy path — Activo becomes false, SaveChanges called once.</summary>
    [Fact]
    public async Task Handle_ActiveIngrediente_SetsActivoFalseAndSaves()
    {
        var ingrediente = BuildActiveIngrediente();
        _ingredientes.GetByIdAsync(ingrediente.Id, Arg.Any<CancellationToken>()).Returns(ingrediente);

        await _sut.Handle(new DesactivarIngredienteCommand(ingrediente.Id));

        ingrediente.Activo.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-C02 — idempotent: calling on already-inactive ingrediente succeeds.</summary>
    [Fact]
    public async Task Handle_AlreadyInactiveIngrediente_IsIdempotentAndSaves()
    {
        var ingrediente = BuildActiveIngrediente();
        ingrediente.Desactivar(); // pre-inactivate
        _ingredientes.GetByIdAsync(ingrediente.Id, Arg.Any<CancellationToken>()).Returns(ingrediente);

        var act = async () => await _sut.Handle(new DesactivarIngredienteCommand(ingrediente.Id));
        await act.Should().NotThrowAsync();

        ingrediente.Activo.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-C02 — 404 when ingrediente does not exist.</summary>
    [Fact]
    public async Task Handle_IngredienteNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _ingredientes.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Ingrediente?)null);

        var act = async () => await _sut.Handle(new DesactivarIngredienteCommand(id));

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
