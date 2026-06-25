using FluentAssertions;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Tests;

/// <summary>
/// Unit tests for Plato.CalcularMaxProducible.
/// Pure domain logic — no EF, no infrastructure.
/// </summary>
public class PlatoProducibleTests
{
    private static Dinero Precio()   => new(500m);
    private static Cantidad Kg(decimal v) => new(v, UnidadDeMedida.Kilogramo);

    // ── Empty recipe ─────────────────────────────────────────────────────────

    [Fact]
    public void EmptyRecipe_ReturnsZero()
    {
        var plato = Plato.Crear("Vacío", Precio(), AlicuotaIVA.General);

        var result = plato.CalcularMaxProducible(new Dictionary<Guid, decimal>());

        result.Should().Be(0);
    }

    // ── Single ingredient ────────────────────────────────────────────────────

    [Fact]
    public void SingleIngredient_ExactMultiple_ReturnsCorrectCount()
    {
        var ing = Guid.NewGuid();
        var plato = Plato.Crear("Pasta", Precio(), AlicuotaIVA.General);
        plato.AgregarLineaReceta(ing, Kg(0.5m));       // needs 0.5 kg each
        var balances = new Dictionary<Guid, decimal> { [ing] = 2.0m }; // 4 portions

        plato.CalcularMaxProducible(balances).Should().Be(4);
    }

    [Fact]
    public void SingleIngredient_NonExactDivision_Floors()
    {
        // 2.9 / 1.0 = 2.9 → floor = 2  (not 3)
        var ing = Guid.NewGuid();
        var plato = Plato.Crear("Bife", Precio(), AlicuotaIVA.General);
        plato.AgregarLineaReceta(ing, Kg(1.0m));
        var balances = new Dictionary<Guid, decimal> { [ing] = 2.9m };

        plato.CalcularMaxProducible(balances).Should().Be(2);
    }

    // ── Missing ingredient ───────────────────────────────────────────────────

    [Fact]
    public void MissingIngredient_ReturnsZero()
    {
        var plato = Plato.Crear("Pizza", Precio(), AlicuotaIVA.General);
        plato.AgregarLineaReceta(Guid.NewGuid(), Kg(0.3m));

        // balances dict is empty — ingredient not present
        plato.CalcularMaxProducible(new Dictionary<Guid, decimal>()).Should().Be(0);
    }

    [Fact]
    public void InsufficientBalance_ReturnsZero()
    {
        var ing = Guid.NewGuid();
        var plato = Plato.Crear("Empanadas", Precio(), AlicuotaIVA.General);
        plato.AgregarLineaReceta(ing, Kg(1.0m));
        var balances = new Dictionary<Guid, decimal> { [ing] = 0.5m }; // < required

        plato.CalcularMaxProducible(balances).Should().Be(0);
    }

    // ── Multiple ingredients — limiting factor ───────────────────────────────

    [Fact]
    public void MultipleIngredients_LimitingIngredientDeterminesResult()
    {
        var flour   = Guid.NewGuid();
        var cheese  = Guid.NewGuid();
        var sauce   = Guid.NewGuid();

        var plato = Plato.Crear("Pizza napolitana", Precio(), AlicuotaIVA.General);
        plato.AgregarLineaReceta(flour,  Kg(0.25m)); // needs 0.25 kg → 8 portions from 2 kg
        plato.AgregarLineaReceta(cheese, Kg(0.10m)); // needs 0.10 kg → 15 portions from 1.5 kg
        plato.AgregarLineaReceta(sauce,  Kg(0.20m)); // needs 0.20 kg → 5 portions from 1.0 kg  ← limiting

        var balances = new Dictionary<Guid, decimal>
        {
            [flour]  = 2.0m,
            [cheese] = 1.5m,
            [sauce]  = 1.0m,
        };

        plato.CalcularMaxProducible(balances).Should().Be(5);
    }

    [Fact]
    public void MultipleIngredients_OneMissing_ReturnsZero()
    {
        var ing1 = Guid.NewGuid();
        var ing2 = Guid.NewGuid(); // missing from balances

        var plato = Plato.Crear("Combo", Precio(), AlicuotaIVA.General);
        plato.AgregarLineaReceta(ing1, Kg(0.5m));
        plato.AgregarLineaReceta(ing2, Kg(0.3m));

        var balances = new Dictionary<Guid, decimal> { [ing1] = 5.0m };

        plato.CalcularMaxProducible(balances).Should().Be(0);
    }

    // ── Negative balance (over-committed) ────────────────────────────────────

    [Fact]
    public void NegativeBalance_ReturnsZero()
    {
        var ing = Guid.NewGuid();
        var plato = Plato.Crear("Milanesa", Precio(), AlicuotaIVA.General);
        plato.AgregarLineaReceta(ing, Kg(0.3m));
        var balances = new Dictionary<Guid, decimal> { [ing] = -1.0m }; // over-reserved

        plato.CalcularMaxProducible(balances).Should().Be(0);
    }
}
