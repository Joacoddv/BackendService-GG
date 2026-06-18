using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Ingredientes.BuscarIngredientes;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Ingredientes;
using NSubstitute;

namespace GastroGestion.Application.Tests.Ingredientes;

/// <summary>
/// Unit tests for BuscarIngredientesHandler (CCC-T39, CCC-T50).
/// </summary>
public sealed class BuscarIngredientesHandlerTests
{
    private readonly IIngredienteRepository    _ingredientes = Substitute.For<IIngredienteRepository>();
    private readonly BuscarIngredientesHandler  _sut;

    public BuscarIngredientesHandlerTests()
        => _sut = new BuscarIngredientesHandler(_ingredientes);

    private static Ingrediente BuildIngrediente(string nombre = "Test")
        => Ingrediente.Crear(nombre, UnidadDeMedida.Gramo);

    /// <summary>CCC-C03 — default call passes incluirInactivos=false to repo.</summary>
    [Fact]
    public async Task Handle_DefaultQuery_PassesIncluirInactivosFalse()
    {
        _ingredientes.SearchAsync(null, false, Arg.Any<CancellationToken>())
                     .Returns((IReadOnlyList<Ingrediente>)new List<Ingrediente>());

        await _sut.Handle(new BuscarIngredientesQuery(null, false));

        await _ingredientes.Received(1)
            .SearchAsync(null, false, Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-C03 — incluirInactivos=true passes the flag through to repo.</summary>
    [Fact]
    public async Task Handle_IncluirInactivosTrue_PassesFlagToRepo()
    {
        _ingredientes.SearchAsync(null, true, Arg.Any<CancellationToken>())
                     .Returns((IReadOnlyList<Ingrediente>)new List<Ingrediente>());

        await _sut.Handle(new BuscarIngredientesQuery(null, true));

        await _ingredientes.Received(1)
            .SearchAsync(null, true, Arg.Any<CancellationToken>());
    }

    /// <summary>CCC-C03 — nombre filter is forwarded to repo unchanged.</summary>
    [Fact]
    public async Task Handle_WithNombreFilter_DelegatesToRepo()
    {
        var nombre   = "har";
        var expected = (IReadOnlyList<Ingrediente>)new List<Ingrediente> { BuildIngrediente("Harina 0000") };
        _ingredientes.SearchAsync(nombre, false, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.Handle(new BuscarIngredientesQuery(nombre, false));

        result.Should().HaveCount(1);
        await _ingredientes.Received(1)
            .SearchAsync(nombre, false, Arg.Any<CancellationToken>());
    }

    /// <summary>Handler returns the list that the repo returns — no filtering on its side.</summary>
    [Fact]
    public async Task Handle_ReturnsRepoResult_Unmodified()
    {
        var list = (IReadOnlyList<Ingrediente>)new List<Ingrediente>
        {
            BuildIngrediente("Sal"),
            BuildIngrediente("Pimienta"),
        };
        _ingredientes.SearchAsync(Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                     .Returns(list);

        var result = await _sut.Handle(new BuscarIngredientesQuery(null, false));

        result.Should().HaveCount(2);
    }
}
