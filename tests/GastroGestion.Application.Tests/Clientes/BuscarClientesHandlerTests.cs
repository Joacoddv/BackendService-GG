using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Clientes.BuscarClientes;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Enums;
using NSubstitute;

namespace GastroGestion.Application.Tests.Clientes;

/// <summary>
/// Unit tests for BuscarClientesHandler (CCC-T19, CCC-T30).
/// </summary>
public sealed class BuscarClientesHandlerTests
{
    private readonly IClienteRepository   _clientes = Substitute.For<IClienteRepository>();
    private readonly BuscarClientesHandler _sut;

    public BuscarClientesHandlerTests()
        => _sut = new BuscarClientesHandler(_clientes);

    private static Cliente BuildCliente(string nombre = "Test")
        => Cliente.Crear(nombre, CondicionIVA.ConsumidorFinal, null, null);

    /// <summary>CCC-B03 — default call passes incluirInactivos=false to repo.</summary>
    [Fact]
    public async Task Handle_DefaultQuery_PassesIncluirInactivosFalse()
    {
        _clientes.SearchAsync(null, false, Arg.Any<CancellationToken>())
                 .Returns((IReadOnlyList<Cliente>)new List<Cliente>());

        await _sut.Handle(new BuscarClientesQuery(null, false));

        await _clientes.Received(1)
            .SearchAsync(null, false, Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-B03 — incluirInactivos=true passes the flag through to repo.</summary>
    [Fact]
    public async Task Handle_IncluirInactivosTrue_PassesFlagToRepo()
    {
        _clientes.SearchAsync(null, true, Arg.Any<CancellationToken>())
                 .Returns((IReadOnlyList<Cliente>)new List<Cliente>());

        await _sut.Handle(new BuscarClientesQuery(null, true));

        await _clientes.Received(1)
            .SearchAsync(null, true, Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-B03 — nombre filter is forwarded to repo unchanged.</summary>
    [Fact]
    public async Task Handle_WithNombreFilter_DelegatesToRepo()
    {
        var nombre  = "garc";
        var expected = (IReadOnlyList<Cliente>)new List<Cliente> { BuildCliente("García SA") };
        _clientes.SearchAsync(nombre, false, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.Handle(new BuscarClientesQuery(nombre, false));

        result.Should().HaveCount(1);
        await _clientes.Received(1)
            .SearchAsync(nombre, false, Arg.Any<CancellationToken>());
    }

    /// <summary>Handler returns the list that the repo returns — no filtering on its side.</summary>
    [Fact]
    public async Task Handle_ReturnsRepoResult_Unmodified()
    {
        var list = (IReadOnlyList<Cliente>)new List<Cliente>
        {
            BuildCliente("Alpha"),
            BuildCliente("Beta"),
        };
        _clientes.SearchAsync(Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                 .Returns(list);

        var result = await _sut.Handle(new BuscarClientesQuery(null, false));

        result.Should().HaveCount(2);
    }
}
