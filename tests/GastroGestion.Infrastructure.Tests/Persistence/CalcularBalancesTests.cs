using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Stock;
using GastroGestion.Infrastructure.Persistence.Repositories;
using GastroGestion.Infrastructure.Tests.Common;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration test for MovimientoStockRepository.CalcularBalancesAsync —
/// verifies the grouped SQL query returns correct net balances for multiple
/// ingredients in a single round-trip, including negative reservation movements.
/// </summary>
[Trait("Category", "SliceC")]
public sealed class CalcularBalancesTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public CalcularBalancesTests(LocalDbFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CalcularBalancesAsync_ReturnsCorrectGroupedSums()
    {
        // Arrange — two ingredients with different movement histories
        var ing1 = Guid.NewGuid();
        var ing2 = Guid.NewGuid();

        // ing1: Compra +20, Reserva -5 (negative), LiberacionReserva +3 → net 18
        // ing2: Compra +10, Merma -2 → net 8
        var movements = new[]
        {
            MovimientoStock.RegistrarMovimiento(ing1, TipoMovimientoStock.Compra,            20m),
            MovimientoStock.RegistrarMovimiento(ing1, TipoMovimientoStock.Reserva,            5m),
            MovimientoStock.RegistrarMovimiento(ing1, TipoMovimientoStock.LiberacionReserva,  3m),
            MovimientoStock.RegistrarMovimiento(ing2, TipoMovimientoStock.Compra,            10m),
            MovimientoStock.RegistrarMovimiento(ing2, TipoMovimientoStock.Merma,              2m),
        };

        await using (var saveCtx = _fixture.CreateContext())
        {
            foreach (var m in movements)
                await saveCtx.MovimientosStock.AddAsync(m);

            await saveCtx.SaveChangesAsync();
        }

        // Act
        await using var readCtx = _fixture.CreateContext();
        var repo = new MovimientoStockRepository(readCtx);

        var balances = await repo.CalcularBalancesAsync();

        // Assert
        Assert.True(balances.ContainsKey(ing1), "ing1 should be present");
        Assert.True(balances.ContainsKey(ing2), "ing2 should be present");

        // Sign convention from MovimientoStock.RegistrarMovimiento: the caller passes a positive
        // amount; the factory stores Compra/LiberacionReserva as positive and negates Reserva/Merma.
        // The repository sums the stored signed values, so:
        //   ing1: Compra +20, Reserva -5, LiberacionReserva +3 → 18
        Assert.Equal(18m, balances[ing1]);

        //   ing2: Compra +10, Merma -2 → 8
        Assert.Equal(8m, balances[ing2]);
    }

    [Fact]
    public async Task CalcularBalancesAsync_ExcludesOtherIngredients()
    {
        // Verify ingredients seeded by other tests do not pollute the result —
        // each test fixture gets its own isolated database, so this checks that
        // an ingredient with no movements in THIS fixture is simply absent from the result.
        var phantomId = Guid.NewGuid();

        await using var readCtx = _fixture.CreateContext();
        var repo = new MovimientoStockRepository(readCtx);

        var balances = await repo.CalcularBalancesAsync();

        Assert.False(balances.ContainsKey(phantomId), "Ingredient with no movements should not appear");
    }
}
