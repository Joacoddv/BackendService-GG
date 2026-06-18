using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Ingredientes.EditarIngrediente;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Ingredientes;
using NSubstitute;

namespace GastroGestion.Application.Tests.Ingredientes;

/// <summary>
/// Unit tests for EditarIngredienteHandler (CCC-T37, CCC-T48).
/// All collaborators are mocked with NSubstitute — no database involved.
/// </summary>
public sealed class EditarIngredienteHandlerTests
{
    private readonly IIngredienteRepository _ingredientes = Substitute.For<IIngredienteRepository>();
    private readonly IUnitOfWork            _uow          = Substitute.For<IUnitOfWork>();
    private readonly EditarIngredienteHandler _sut;

    public EditarIngredienteHandlerTests()
        => _sut = new EditarIngredienteHandler(_ingredientes, _uow);

    private static Ingrediente BuildIngrediente(
        string nombre = "Harina",
        UnidadDeMedida unidad = UnidadDeMedida.Kilogramo)
        => Ingrediente.Crear(nombre, unidad);

    /// <summary>CCC-C01 happy path — updated Nombre returned, SaveChanges called once.</summary>
    [Fact]
    public async Task Handle_HappyPath_UpdatesNombreAndSaves()
    {
        var ingrediente = BuildIngrediente("Harina 000");
        var cmd         = new EditarIngredienteCommand(ingrediente.Id, "Harina 0000");

        _ingredientes.GetByIdAsync(ingrediente.Id, Arg.Any<CancellationToken>()).Returns(ingrediente);
        _ingredientes.NombreExistsForOtherAsync(cmd.Nombre, cmd.Id, Arg.Any<CancellationToken>())
                     .Returns(false);

        var result = await _sut.Handle(cmd);

        result.Nombre.Should().Be("Harina 0000");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-C01 — UnidadBase must remain unchanged after edit.</summary>
    [Fact]
    public async Task Handle_UnidadBaseUnchanged_AfterUpdate()
    {
        var ingrediente    = BuildIngrediente("Aceite", UnidadDeMedida.Litro);
        var originalUnidad = ingrediente.UnidadBase;
        var cmd            = new EditarIngredienteCommand(ingrediente.Id, "Aceite de Oliva");

        _ingredientes.GetByIdAsync(ingrediente.Id, Arg.Any<CancellationToken>()).Returns(ingrediente);
        _ingredientes.NombreExistsForOtherAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns(false);

        var result = await _sut.Handle(cmd);

        result.UnidadBase.Should().Be(originalUnidad);
    }

    /// <summary>CCC-C01 — 404 when ingrediente not found.</summary>
    [Fact]
    public async Task Handle_IngredienteNotFound_ThrowsNotFoundException()
    {
        var id  = Guid.NewGuid();
        var cmd = new EditarIngredienteCommand(id, "Nuevo Nombre");
        _ingredientes.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Ingrediente?)null);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>CCC-C01 — 409 when Nombre conflicts with another ingrediente.</summary>
    [Fact]
    public async Task Handle_NombreConflict_ThrowsConflictException()
    {
        var ingrediente = BuildIngrediente("Sal");
        var cmd         = new EditarIngredienteCommand(ingrediente.Id, "Pimienta");

        _ingredientes.GetByIdAsync(ingrediente.Id, Arg.Any<CancellationToken>()).Returns(ingrediente);
        _ingredientes.NombreExistsForOtherAsync("Pimienta", ingrediente.Id, Arg.Any<CancellationToken>())
                     .Returns(true);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<ConflictException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-C01 — 422 when domain rejects blank Nombre.</summary>
    [Fact]
    public async Task Handle_EmptyNombre_ThrowsDomainException()
    {
        var ingrediente = BuildIngrediente("Tomate");
        var cmd         = new EditarIngredienteCommand(ingrediente.Id, "");

        _ingredientes.GetByIdAsync(ingrediente.Id, Arg.Any<CancellationToken>()).Returns(ingrediente);
        _ingredientes.NombreExistsForOtherAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns(false);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<DomainException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
