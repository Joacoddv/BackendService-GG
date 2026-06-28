using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Tests;

public class PlatoTests
{
    private static Dinero PrecioValido() => new(500m);
    private static Cantidad CantidadValida() => new(0.2m, UnidadDeMedida.Kilogramo);

    [Fact]
    public void Crear_WithValidData_CreatesPlato()
    {
        var plato = Plato.Crear("Milanesa napolitana", PrecioValido(), AlicuotaIVA.General);

        plato.Nombre.Should().Be("Milanesa napolitana");
        plato.PrecioBase.Monto.Should().Be(500m);
        plato.AlicuotaIVA.Should().Be(AlicuotaIVA.General);
        plato.Activo.Should().BeTrue();
        plato.LineasReceta.Should().BeEmpty();
    }

    [Fact]
    public void Crear_WithEmptyNombre_ThrowsDomainException()
    {
        var act = () => Plato.Crear("", PrecioValido(), AlicuotaIVA.General);
        act.Should().Throw<DomainException>()
           .WithMessage("*Nombre*");
    }

    [Fact]
    public void Crear_WithNullPrecioBase_ThrowsArgumentNullException()
    {
        var act = () => Plato.Crear("Ensalada", null!, AlicuotaIVA.Exento);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AgregarLineaReceta_AddsToRecipe()
    {
        var plato = Plato.Crear("Pasta", PrecioValido(), AlicuotaIVA.General);
        var ingredienteId = Guid.NewGuid();

        plato.AgregarLineaReceta(ingredienteId, UnidadDeMedida.Kilogramo, CantidadValida());

        plato.LineasReceta.Should().HaveCount(1);
        plato.LineasReceta[0].IngredienteId.Should().Be(ingredienteId);
    }

    [Fact]
    public void EliminarLineaReceta_RemovesFromRecipe()
    {
        var plato = Plato.Crear("Pasta", PrecioValido(), AlicuotaIVA.General);
        plato.AgregarLineaReceta(Guid.NewGuid(), UnidadDeMedida.Kilogramo, CantidadValida());
        var lineaId = plato.LineasReceta[0].Id;

        plato.EliminarLineaReceta(lineaId);

        plato.LineasReceta.Should().BeEmpty();
    }

    [Fact]
    public void ActualizarPrecio_UpdatesPrecioBase()
    {
        var plato = Plato.Crear("Bife", PrecioValido(), AlicuotaIVA.General);
        var nuevoPrecio = new Dinero(750m);

        plato.ActualizarPrecio(nuevoPrecio);

        plato.PrecioBase.Monto.Should().Be(750m);
    }

    [Fact]
    public void LineaReceta_PlatoReferenciadoId_IsNullByDefault()
    {
        // Sub-recipe seam: PlatoReferenciadoId is null in v1
        var plato = Plato.Crear("Combo", PrecioValido(), AlicuotaIVA.General);
        plato.AgregarLineaReceta(Guid.NewGuid(), UnidadDeMedida.Kilogramo, CantidadValida());

        plato.LineasReceta[0].PlatoReferenciadoId.Should().BeNull();
    }

    [Fact]
    public void AgregarLineaReceta_UnitMismatch_ThrowsDomainException()
    {
        var plato = Plato.Crear("Pasta", PrecioValido(), AlicuotaIVA.General);

        // Recipe line quantity in Gramo while the ingredient base unit is Kilogramo → rejected.
        var act = () => plato.AgregarLineaReceta(
            Guid.NewGuid(),
            UnidadDeMedida.Kilogramo,
            new Cantidad(200m, UnidadDeMedida.Gramo));

        act.Should().Throw<DomainException>();
        plato.LineasReceta.Should().BeEmpty();
    }

    [Fact]
    public void Desactivar_SetsActivoFalse()
    {
        var plato = Plato.Crear("Sopa", PrecioValido(), AlicuotaIVA.Exento);
        plato.Desactivar();
        plato.Activo.Should().BeFalse();
    }

    [Fact]
    public void Desactivar_AlreadyInactive_IsIdempotent()
    {
        var plato = Plato.Crear("Sopa", PrecioValido(), AlicuotaIVA.Exento);
        plato.Desactivar();
        var act = () => plato.Desactivar();
        act.Should().NotThrow();
        plato.Activo.Should().BeFalse();
    }
}
