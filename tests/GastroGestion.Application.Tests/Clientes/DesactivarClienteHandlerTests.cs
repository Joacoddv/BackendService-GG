using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Clientes.DesactivarCliente;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Enums;
using NSubstitute;

namespace GastroGestion.Application.Tests.Clientes;

/// <summary>
/// Unit tests for DesactivarClienteHandler (CCC-T18, CCC-T29).
/// </summary>
public sealed class DesactivarClienteHandlerTests
{
    private readonly IClienteRepository     _clientes = Substitute.For<IClienteRepository>();
    private readonly IUnitOfWork            _uow      = Substitute.For<IUnitOfWork>();
    private readonly DesactivarClienteHandler _sut;

    public DesactivarClienteHandlerTests()
        => _sut = new DesactivarClienteHandler(_clientes, _uow);

    private static Cliente BuildActiveCliente()
        => Cliente.Crear("Test Cliente", CondicionIVA.ConsumidorFinal, null, null);

    /// <summary>CCC-B02 happy path — Activo becomes false, SaveChanges called.</summary>
    [Fact]
    public async Task Handle_ActiveCliente_SetsActivoFalseAndSaves()
    {
        var cliente = BuildActiveCliente();
        _clientes.GetByIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        await _sut.Handle(new DesactivarClienteCommand(cliente.Id));

        cliente.Activo.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-B02 — idempotent: calling on already-inactive cliente still returns without error.</summary>
    [Fact]
    public async Task Handle_AlreadyInactiveCliente_IsIdempotentAndSaves()
    {
        var cliente = BuildActiveCliente();
        cliente.Desactivar(); // pre-inactivate
        _clientes.GetByIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        // Must not throw
        var act = async () => await _sut.Handle(new DesactivarClienteCommand(cliente.Id));
        await act.Should().NotThrowAsync();

        cliente.Activo.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-B02 — 404 when cliente does not exist.</summary>
    [Fact]
    public async Task Handle_ClienteNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _clientes.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var act = async () => await _sut.Handle(new DesactivarClienteCommand(id));

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
