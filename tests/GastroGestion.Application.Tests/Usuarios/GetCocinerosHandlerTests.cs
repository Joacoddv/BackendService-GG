using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Usuarios.GetCocineros;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Usuarios;
using NSubstitute;

namespace GastroGestion.Application.Tests.Usuarios;

/// <summary>
/// Unit tests for GetCocinerosHandler (CCC-A01).
/// All collaborators are mocked with NSubstitute — no database involved.
/// </summary>
public sealed class GetCocinerosHandlerTests
{
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly GetCocinerosHandler _sut;

    public GetCocinerosHandlerTests()
        => _sut = new GetCocinerosHandler(_usuarios);

    private static Usuario BuildCocinero(string nombre = "Cocinero Test", bool activo = true)
    {
        var u = Usuario.Crear($"{nombre.Replace(" ", ".")}@test.local", nombre, RolUsuario.Cocinero, "hash");
        if (!activo)
            u.Desactivar();
        return u;
    }

    private static Usuario BuildUsuario(RolUsuario rol, string nombre = "Other")
        => Usuario.Crear($"{nombre.Replace(" ", ".")}@test.local", nombre, rol, "hash");

    /// <summary>
    /// CCC-A01 — Handler delegates to repo with RolUsuario.Cocinero and returns the list.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsActiveCocineros_FromRepository()
    {
        var cocinero1 = BuildCocinero("Cocinero Uno");
        var cocinero2 = BuildCocinero("Cocinero Dos");
        var expected  = (IReadOnlyList<Usuario>)new List<Usuario> { cocinero1, cocinero2 };

        _usuarios.GetByRolAsync(RolUsuario.Cocinero, Arg.Any<CancellationToken>())
                 .Returns(expected);

        var result = await _sut.Handle(new GetCocinerosQuery());

        result.Should().HaveCount(2);
        result.Should().Contain(cocinero1);
        result.Should().Contain(cocinero2);
        await _usuarios.Received(1).GetByRolAsync(RolUsuario.Cocinero, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// CCC-A01 — Repository already filters inactive; handler returns whatever the repo provides.
    /// This test verifies the handler does NOT second-guess the repo filter.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoActiveCocineros()
    {
        _usuarios.GetByRolAsync(RolUsuario.Cocinero, Arg.Any<CancellationToken>())
                 .Returns((IReadOnlyList<Usuario>)new List<Usuario>());

        var result = await _sut.Handle(new GetCocinerosQuery());

        result.Should().BeEmpty();
    }

    /// <summary>
    /// CCC-A01 — Handler calls repo with exactly RolUsuario.Cocinero (not Administrador or other roles).
    /// </summary>
    [Fact]
    public async Task Handle_CallsRepositoryWithCocineroRole()
    {
        _usuarios.GetByRolAsync(Arg.Any<RolUsuario>(), Arg.Any<CancellationToken>())
                 .Returns((IReadOnlyList<Usuario>)new List<Usuario>());

        await _sut.Handle(new GetCocinerosQuery());

        await _usuarios.Received(1).GetByRolAsync(
            Arg.Is<RolUsuario>(r => r == RolUsuario.Cocinero),
            Arg.Any<CancellationToken>());
        await _usuarios.DidNotReceive().GetByRolAsync(
            Arg.Is<RolUsuario>(r => r != RolUsuario.Cocinero),
            Arg.Any<CancellationToken>());
    }
}
