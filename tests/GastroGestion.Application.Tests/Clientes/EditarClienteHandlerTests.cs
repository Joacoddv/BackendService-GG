using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Clientes.EditarCliente;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using NSubstitute;

namespace GastroGestion.Application.Tests.Clientes;

/// <summary>
/// Unit tests for EditarClienteHandler (CCC-T17, CCC-T28).
/// All collaborators are mocked with NSubstitute — no database involved.
/// </summary>
public sealed class EditarClienteHandlerTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IUnitOfWork        _uow      = Substitute.For<IUnitOfWork>();
    private readonly EditarClienteHandler _sut;

    // Valid CUIT 20-12345678-6
    private const string ValidCuitString = "20123456786";

    public EditarClienteHandlerTests()
        => _sut = new EditarClienteHandler(_clientes, _uow);

    private static Cliente BuildCliente(
        string nombre = "Original",
        CondicionIVA condicion = CondicionIVA.ConsumidorFinal)
        => Cliente.Crear(nombre, condicion, null, null);

    /// <summary>CCC-B01 happy path — updated fields returned, SaveChanges called once.</summary>
    [Fact]
    public async Task Handle_HappyPath_UpdatesClienteAndSaves()
    {
        var cliente = BuildCliente();
        var cmd     = new EditarClienteCommand(
            cliente.Id, "Updated Name", CondicionIVA.Monotributista, null, null);

        _clientes.GetByIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _clientes.CuitExistsForOtherAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(false);

        var result = await _sut.Handle(cmd);

        result.Nombre.Should().Be("Updated Name");
        result.CondicionIVA.Should().Be(CondicionIVA.Monotributista);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>NumeroCliente must remain unchanged after edit.</summary>
    [Fact]
    public async Task Handle_NumeroClienteUnchanged_AfterUpdate()
    {
        var cliente = BuildCliente();
        var originalNumero = cliente.NumeroCliente;
        var cmd = new EditarClienteCommand(
            cliente.Id, "New Name", CondicionIVA.ConsumidorFinal, null, null);

        _clientes.GetByIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var result = await _sut.Handle(cmd);

        result.NumeroCliente.Should().Be(originalNumero);
    }

    /// <summary>CCC-B01 — 404 when cliente not found.</summary>
    [Fact]
    public async Task Handle_ClienteNotFound_ThrowsNotFoundException()
    {
        var id  = Guid.NewGuid();
        var cmd = new EditarClienteCommand(id, "Name", CondicionIVA.ConsumidorFinal, null, null);
        _clientes.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>CCC-B01 — 409 when CUIT conflicts with another cliente.</summary>
    [Fact]
    public async Task Handle_CuitConflict_ThrowsConflictException()
    {
        var cliente = BuildCliente();
        var cmd     = new EditarClienteCommand(
            cliente.Id, "Name", CondicionIVA.ResponsableInscripto, ValidCuitString, null);

        _clientes.GetByIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _clientes.CuitExistsForOtherAsync(ValidCuitString, cliente.Id, Arg.Any<CancellationToken>())
                 .Returns(true);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<ConflictException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-B01 — 422 when RI without CUIT (domain validation).</summary>
    [Fact]
    public async Task Handle_ResponsableInscriptoWithoutCuit_ThrowsDomainException()
    {
        var cliente = BuildCliente();
        var cmd     = new EditarClienteCommand(
            cliente.Id, "Name", CondicionIVA.ResponsableInscripto, null, null);

        _clientes.GetByIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _clientes.CuitExistsForOtherAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(false);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<DomainException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
