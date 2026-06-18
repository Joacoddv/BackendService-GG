using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Domain.Tests;

public class IngredienteTests
{
    [Fact]
    public void Crear_WithValidData_CreatesActiveIngrediente()
    {
        var ingrediente = Ingrediente.Crear("Tomate", UnidadDeMedida.Kilogramo);

        ingrediente.Nombre.Should().Be("Tomate");
        ingrediente.UnidadBase.Should().Be(UnidadDeMedida.Kilogramo);
        ingrediente.Activo.Should().BeTrue();
        ingrediente.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Crear_WithEmptyNombre_ThrowsDomainException()
    {
        var act = () => Ingrediente.Crear("", UnidadDeMedida.Gramo);
        act.Should().Throw<DomainException>()
           .WithMessage("*Nombre*");
    }

    [Fact]
    public void Crear_WithWhitespaceName_ThrowsDomainException()
    {
        var act = () => Ingrediente.Crear("   ", UnidadDeMedida.Litro);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Desactivar_SetsActivoFalse()
    {
        var ingrediente = Ingrediente.Crear("Cebolla", UnidadDeMedida.Unidad);

        ingrediente.Desactivar();

        ingrediente.Activo.Should().BeFalse();
    }

    [Fact]
    public void Desactivar_AlreadyInactive_IsIdempotent()
    {
        var ingrediente = Ingrediente.Crear("Ajo", UnidadDeMedida.Gramo);
        ingrediente.Desactivar();

        var act = () => ingrediente.Desactivar();
        act.Should().NotThrow();
        ingrediente.Activo.Should().BeFalse();
    }

    [Fact]
    public void Ingrediente_UnidadDeMedida_UsesControlledVocabulary()
    {
        // All enum values should be creatable
        foreach (var unidad in Enum.GetValues<UnidadDeMedida>())
        {
            var ing = Ingrediente.Crear($"Ingrediente {unidad}", unidad);
            ing.UnidadBase.Should().Be(unidad);
        }
    }

    // ── ActualizarNombre (CCC-T33, CCC-T47) ──────────────────────────────────

    [Fact]
    public void ActualizarNombre_WithValidName_UpdatesNombre()
    {
        var ingrediente = Ingrediente.Crear("Original", UnidadDeMedida.Kilogramo);
        var originalUnidad  = ingrediente.UnidadBase;
        var originalActivo  = ingrediente.Activo;

        ingrediente.ActualizarNombre("Harina 0000");

        ingrediente.Nombre.Should().Be("Harina 0000");
        // UnidadBase and Activo must remain unchanged
        ingrediente.UnidadBase.Should().Be(originalUnidad);
        ingrediente.Activo.Should().Be(originalActivo);
    }

    [Fact]
    public void ActualizarNombre_LeavesUnidadBaseUnchanged()
    {
        var ingrediente = Ingrediente.Crear("Azúcar", UnidadDeMedida.Gramo);

        ingrediente.ActualizarNombre("Azúcar Morena");

        ingrediente.UnidadBase.Should().Be(UnidadDeMedida.Gramo);
    }

    [Fact]
    public void ActualizarNombre_EmptyNombre_ThrowsDomainException()
    {
        var ingrediente = Ingrediente.Crear("Tomate", UnidadDeMedida.Kilogramo);

        var act = () => ingrediente.ActualizarNombre("");

        act.Should().Throw<DomainException>().WithMessage("*Nombre*");
    }

    [Fact]
    public void ActualizarNombre_WhitespaceNombre_ThrowsDomainException()
    {
        var ingrediente = Ingrediente.Crear("Cebolla", UnidadDeMedida.Unidad);

        var act = () => ingrediente.ActualizarNombre("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ActualizarNombre_NullNombre_ThrowsDomainException()
    {
        var ingrediente = Ingrediente.Crear("Ajo", UnidadDeMedida.Gramo);

        var act = () => ingrediente.ActualizarNombre(null!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Desactivar_AlreadyInactiveAfterActualizarNombre_RemainsIdempotent()
    {
        var ingrediente = Ingrediente.Crear("Manteca", UnidadDeMedida.Gramo);
        ingrediente.Desactivar();
        ingrediente.ActualizarNombre("Manteca Premium");

        // Still inactive after name update
        ingrediente.Activo.Should().BeFalse();
    }
}
