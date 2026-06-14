using GastroGestion.Domain.Mesas;
using GastroGestion.Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for Mesa aggregate round-trip persistence.
/// Covers REQ-03 Scenario 03-C and REQ-09 Scenario 09-B (RowVersion non-empty after first save).
/// </summary>
[Trait("Category", "SliceA")]
public sealed class MesaRoundTripTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public MesaRoundTripTests(LocalDbFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Mesa_RowVersionIsNonEmpty_AfterFirstSave()
    {
        // Arrange
        var mesa = Mesa.Crear(numero: 1, capacidad: 4);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Mesas.AddAsync(mesa);
            await saveCtx.SaveChangesAsync();
        }

        // Assert — RowVersion is store-generated; must be non-null and non-empty after first save
        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Mesas.FirstOrDefaultAsync(m => m.Id == mesa.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(1, reloaded.Numero);
        Assert.Equal(4, reloaded.Capacidad);
        Assert.True(reloaded.Activa);

        // REQ-09 Scenario 09-B
        Assert.NotNull(reloaded.RowVersion);
        Assert.NotEmpty(reloaded.RowVersion);
    }

    [Fact]
    public async Task Mesa_RoundTrips_WithAllProperties()
    {
        // Arrange
        var mesa = Mesa.Crear(numero: 5, capacidad: 6);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Mesas.AddAsync(mesa);
            await saveCtx.SaveChangesAsync();
        }

        // Assert
        await using var readCtx = _fixture.CreateContext();
        var reloaded = await readCtx.Mesas.FirstOrDefaultAsync(m => m.Id == mesa.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(5, reloaded.Numero);
        Assert.Equal(6, reloaded.Capacidad);
        Assert.Equal(GastroGestion.Domain.Enums.EstadoMesa.Libre, reloaded.Estado);
        Assert.True(reloaded.Activa);
        Assert.Null(reloaded.PedidoActivoId);
    }
}
