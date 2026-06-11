using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Menus;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Tests;

public class MenuTests
{
    private static DateOnly FutureDate() =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

    private static DateOnly PastDate() =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

    private static DateOnly TodayDate() =>
        DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Crear_WithFutureDate_CreatesMenu()
    {
        var menu = Menu.Crear("Menu del Lunes", FutureDate());

        menu.Nombre.Should().Be("Menu del Lunes");
        menu.FechaVigencia.Should().Be(FutureDate());
        menu.Activo.Should().BeTrue();
        menu.Items.Should().BeEmpty();
    }

    [Fact]
    public void Crear_WithPastDate_ThrowsDomainException()
    {
        var act = () => Menu.Crear("Menu del pasado", PastDate());
        act.Should().Throw<DomainException>()
           .WithMessage("*future date*");
    }

    [Fact]
    public void Crear_WithTodayDate_ThrowsDomainException()
    {
        // Today is not strictly in the future
        var act = () => Menu.Crear("Menu de hoy", TodayDate());
        act.Should().Throw<DomainException>()
           .WithMessage("*future date*");
    }

    [Fact]
    public void Crear_WithEmptyNombre_ThrowsDomainException()
    {
        var act = () => Menu.Crear("", FutureDate());
        act.Should().Throw<DomainException>()
           .WithMessage("*Nombre*");
    }

    [Fact]
    public void AgregarItem_WithNullOverride_IsPermitted()
    {
        var menu = Menu.Crear("Menu semanal", FutureDate());
        var platoId = Guid.NewGuid();

        menu.AgregarItem(platoId, null);

        menu.Items.Should().HaveCount(1);
        menu.Items[0].PrecioOverride.Should().BeNull();
    }

    [Fact]
    public void AgregarItem_WithPriceOverride_StoresOverride()
    {
        var menu = Menu.Crear("Especial", FutureDate());
        var precio = new Dinero(350m);

        menu.AgregarItem(Guid.NewGuid(), precio);

        menu.Items[0].PrecioOverride!.Monto.Should().Be(350m);
    }

    [Fact]
    public void AgregarItem_WithNegativePriceOverride_ThrowsDomainException()
    {
        var menu = Menu.Crear("Especial", FutureDate());

        // Dinero itself throws on negative monto — validates REQ-06 price override rule
        var act = () => menu.AgregarItem(Guid.NewGuid(), new Dinero(-10m));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EliminarItem_RemovesFromList()
    {
        var menu = Menu.Crear("Especial", FutureDate());
        menu.AgregarItem(Guid.NewGuid(), null);
        var itemId = menu.Items[0].Id;

        menu.EliminarItem(itemId);

        menu.Items.Should().BeEmpty();
    }

    [Fact]
    public void Desactivar_SetsActivoFalse()
    {
        var menu = Menu.Crear("Menu test", FutureDate());
        menu.Desactivar();
        menu.Activo.Should().BeFalse();
    }
}
