using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Menus.DesactivarMenu;
using GastroGestion.Domain.Menus;
using NSubstitute;

namespace GastroGestion.Application.Tests.Menus;

/// <summary>Unit tests for DesactivarMenuHandler.</summary>
public sealed class DesactivarMenuHandlerTests
{
    private readonly IMenuRepository _menus = Substitute.For<IMenuRepository>();
    private readonly IUnitOfWork     _uow   = Substitute.For<IUnitOfWork>();
    private readonly DesactivarMenuHandler _sut;

    public DesactivarMenuHandlerTests()
        => _sut = new DesactivarMenuHandler(_menus, _uow);

    private static Menu BuildActiveMenu()
        => Menu.Crear("Test Menu", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7));

    [Fact]
    public async Task Handle_ActiveMenu_SetsActivoFalseAndSaves()
    {
        var menu = BuildActiveMenu();
        _menus.GetByIdAsync(menu.Id, Arg.Any<CancellationToken>()).Returns(menu);

        await _sut.Handle(new DesactivarMenuCommand(menu.Id));

        menu.Activo.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyInactiveMenu_IsIdempotentAndSaves()
    {
        var menu = BuildActiveMenu();
        menu.Desactivar();
        _menus.GetByIdAsync(menu.Id, Arg.Any<CancellationToken>()).Returns(menu);

        var act = async () => await _sut.Handle(new DesactivarMenuCommand(menu.Id));
        await act.Should().NotThrowAsync();

        menu.Activo.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MenuNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _menus.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Menu?)null);

        var act = async () => await _sut.Handle(new DesactivarMenuCommand(id));

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
