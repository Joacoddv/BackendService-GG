using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Ingredientes;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;
using GastroGestion.Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for Plato aggregate round-trip persistence.
/// Covers REQ-03 Scenario 03-B: PrecioBase (Dinero) + LineaReceta with Cantidad.
/// </summary>
[Trait("Category", "SliceA")]
public sealed class PlatoRoundTripTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public PlatoRoundTripTests(LocalDbFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task PlatoWithLineasReceta_RoundTrips()
    {
        // Arrange
        var ing1 = Ingrediente.Crear("Harina100", UnidadDeMedida.Gramo);
        var ing2 = Ingrediente.Crear("Huevo100", UnidadDeMedida.Unidad);
        var ing3 = Ingrediente.Crear("Sal100", UnidadDeMedida.Gramo);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Ingredientes.AddAsync(ing1);
            await saveCtx.Ingredientes.AddAsync(ing2);
            await saveCtx.Ingredientes.AddAsync(ing3);
            await saveCtx.SaveChangesAsync();
        }

        var plato = Plato.Crear(
            "Pizza Napolitana",
            new Dinero(1200m, Moneda.ARS),
            AlicuotaIVA.General);

        plato.AgregarLineaReceta(ing1.Id, ing1.UnidadBase, new Cantidad(200, UnidadDeMedida.Gramo));
        plato.AgregarLineaReceta(ing2.Id, ing2.UnidadBase, new Cantidad(2, UnidadDeMedida.Unidad));
        plato.AgregarLineaReceta(ing3.Id, ing3.UnidadBase, new Cantidad(5, UnidadDeMedida.Gramo));

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Platos.AddAsync(plato);
            await saveCtx.SaveChangesAsync();
        }

        // Assert — reload in fresh context
        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Platos.FirstOrDefaultAsync(p => p.Id == plato.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("Pizza Napolitana", reloaded.Nombre);

        // REQ-04 Scenario 04-B — Dinero nested column names survive round-trip
        Assert.Equal(1200m, reloaded.PrecioBase.Monto);
        Assert.Equal(Moneda.ARS, reloaded.PrecioBase.Moneda);
        Assert.Equal(AlicuotaIVA.General, reloaded.AlicuotaIVA);

        // REQ-03 Scenario 03-B — recipe lines present with correct Cantidad
        Assert.Equal(3, reloaded.LineasReceta.Count);

        var lineaHarina = reloaded.LineasReceta.First(l => l.IngredienteId == ing1.Id);
        Assert.Equal(200m, lineaHarina.Cantidad.Valor);
        Assert.Equal(UnidadDeMedida.Gramo, lineaHarina.Cantidad.Unidad);
    }
}
