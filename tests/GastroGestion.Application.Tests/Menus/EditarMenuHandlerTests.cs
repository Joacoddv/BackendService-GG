using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Menus.EditarMenu;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Menus;
using NSubstitute;

namespace GastroGestion.Application.Tests.Menus;

/// <summary>Unit tests for EditarMenuHandler. All collaborators are mocked with NSubstitute.</summary>
public sealed class EditarMenuHandlerTests
{
    private readonly IMenuRepository _menus = Substitute.For<IMenuRepository>();
    private readonly IUnitOfWork     _uow   = Substitute.For<IUnitOfWork>();
    private readonly EditarMenuHandler _sut;

    public EditarMenuHandlerTests()
        => _sut = new EditarMenuHandler(_menus, _uow);

    private static DateOnly FutureDate(int days = 7)
        => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(days);

    private static Menu BuildMenu(string nombre = "Menu del Día")
        => Menu.Crear(nombre, FutureDate());

    [Fact]
    public async Task Handle_HappyPath_UpdatesNombreAndFechaAndSaves()
    {
        var menu     = BuildMenu("Menu del Día");
        var newFecha = FutureDate(14);
        var cmd      = new EditarMenuCommand(menu.Id, "Menu Especial", newFecha);
        _menus.GetByIdAsync(menu.Id, Arg.Any<CancellationToken>()).Returns(menu);

        var result = await _sut.Handle(cmd);

        result.Nombre.Should().Be("Menu Especial");
        result.FechaVigencia.Should().Be(newFecha);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MenuNotFound_ThrowsNotFoundException()
    {
        var id  = Guid.NewGuid();
        var cmd = new EditarMenuCommand(id, "Nuevo", FutureDate());
        _menus.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Menu?)null);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyNombre_ThrowsDomainException()
    {
        var menu = BuildMenu();
        var cmd  = new EditarMenuCommand(menu.Id, "", FutureDate());
        _menus.GetByIdAsync(menu.Id, Arg.Any<CancellationToken>()).Returns(menu);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<DomainException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PastFechaVigencia_ThrowsDomainException()
    {
        var menu      = BuildMenu();
        var pastFecha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var cmd       = new EditarMenuCommand(menu.Id, "Menu Especial", pastFecha);
        _menus.GetByIdAsync(menu.Id, Arg.Any<CancellationToken>()).Returns(menu);

        var act = async () => await _sut.Handle(cmd);

        await act.Should().ThrowAsync<DomainException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
