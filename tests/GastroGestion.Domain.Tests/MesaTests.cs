using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Mesas;

namespace GastroGestion.Domain.Tests;

public class MesaTests
{
    [Fact]
    public void Crear_WithPositiveCapacidad_CreatesMesa()
    {
        var mesa = Mesa.Crear(1, 4);

        mesa.Numero.Should().Be(1);
        mesa.Capacidad.Should().Be(4);
        mesa.Estado.Should().Be(EstadoMesa.Libre);
        mesa.Activa.Should().BeTrue();
        mesa.PedidoActivoId.Should().BeNull();
    }

    [Fact]
    public void Crear_WithZeroCapacidad_ThrowsDomainException()
    {
        var act = () => Mesa.Crear(1, 0);
        act.Should().Throw<DomainException>()
           .WithMessage("*Capacidad*greater than zero*");
    }

    [Fact]
    public void Crear_WithNegativeCapacidad_ThrowsDomainException()
    {
        var act = () => Mesa.Crear(1, -5);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Crear_WithZeroNumero_ThrowsDomainException()
    {
        var act = () => Mesa.Crear(0, 4);
        act.Should().Throw<DomainException>()
           .WithMessage("*Numero*greater than zero*");
    }

    [Fact]
    public void AsignarPedido_WhenLibre_AssignsPedidoAndSetsOcupada()
    {
        var mesa = Mesa.Crear(1, 4);
        var pedidoId = Guid.NewGuid();

        mesa.AsignarPedido(pedidoId);

        mesa.PedidoActivoId.Should().Be(pedidoId);
        mesa.Estado.Should().Be(EstadoMesa.Ocupada);
    }

    [Fact]
    public void AsignarPedido_WhenAlreadyOcupada_ThrowsDomainException()
    {
        var mesa = Mesa.Crear(2, 2);
        mesa.AsignarPedido(Guid.NewGuid());

        var act = () => mesa.AsignarPedido(Guid.NewGuid());
        act.Should().Throw<DomainException>()
           .WithMessage("*already has an active Pedido*");
    }

    [Fact]
    public void LiberarPedido_ReturnsToLibre()
    {
        var mesa = Mesa.Crear(3, 6);
        mesa.AsignarPedido(Guid.NewGuid());

        mesa.LiberarPedido();

        mesa.PedidoActivoId.Should().BeNull();
        mesa.Estado.Should().Be(EstadoMesa.Libre);
    }

    [Fact]
    public void Desactivar_WhenPedidoActivo_ThrowsDomainException()
    {
        var mesa = Mesa.Crear(4, 4);
        mesa.AsignarPedido(Guid.NewGuid());

        var act = () => mesa.Desactivar();
        act.Should().Throw<DomainException>()
           .WithMessage("*active Pedido*");
    }

    [Fact]
    public void Desactivar_WhenLibre_SetsActivaFalse()
    {
        var mesa = Mesa.Crear(5, 2);

        mesa.Desactivar();

        mesa.Activa.Should().BeFalse();
    }

    [Fact]
    public void Desactivar_AlreadyInactive_IsIdempotent()
    {
        var mesa = Mesa.Crear(6, 4);
        mesa.Desactivar();

        var act = () => mesa.Desactivar();
        act.Should().NotThrow();
        mesa.Activa.Should().BeFalse();
    }

    [Fact]
    public void Mesa_RowVersion_IsInitializedAsEmptyArray()
    {
        var mesa = Mesa.Crear(7, 4);
        // Plain property — EF Core configures it as concurrency token in phase 3
        mesa.RowVersion.Should().NotBeNull();
        mesa.RowVersion.Should().BeEmpty();
    }
}
