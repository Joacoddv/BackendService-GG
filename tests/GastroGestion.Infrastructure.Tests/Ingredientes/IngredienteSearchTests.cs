using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Ingredientes;
using GastroGestion.Infrastructure.Persistence.Repositories;
using GastroGestion.Infrastructure.Tests.Common;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Ingredientes;

/// <summary>
/// Integration tests for IngredienteRepository.SearchAsync and NombreExistsForOtherAsync (CCC-T35, CCC-T51).
/// Uses LocalDbFixture for isolated per-class database.
/// </summary>
public sealed class IngredienteSearchTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public IngredienteSearchTests(LocalDbFixture fixture) => _fixture = fixture;

    private static Ingrediente CreateActive(string nombre, UnidadDeMedida unidad = UnidadDeMedida.Gramo)
        => Ingrediente.Crear(nombre, unidad);

    private static Ingrediente CreateInactive(string nombre)
    {
        var i = Ingrediente.Crear(nombre, UnidadDeMedida.Gramo);
        i.Desactivar();
        return i;
    }

    /// <summary>SearchAsync without nombre returns only active ingredientes by default.</summary>
    [Fact]
    public async Task SearchAsync_DefaultActive_ExcludesInactive()
    {
        var active   = CreateActive($"Active_{Guid.NewGuid():N}");
        var inactive = CreateInactive($"Inactive_{Guid.NewGuid():N}");

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Ingredientes.AddRangeAsync(active, inactive);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new IngredienteRepository(readCtx);
        var result = await repo.SearchAsync(null, incluirInactivos: false);

        Assert.Contains(result, i => i.Id == active.Id);
        Assert.DoesNotContain(result, i => i.Id == inactive.Id);
    }

    /// <summary>SearchAsync with incluirInactivos=true returns both active and inactive.</summary>
    [Fact]
    public async Task SearchAsync_IncluirInactivos_ReturnsBothActiveAndInactive()
    {
        var active   = CreateActive($"ActiveInc_{Guid.NewGuid():N}");
        var inactive = CreateInactive($"InactiveInc_{Guid.NewGuid():N}");

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Ingredientes.AddRangeAsync(active, inactive);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new IngredienteRepository(readCtx);
        var result = await repo.SearchAsync(null, incluirInactivos: true);

        Assert.Contains(result, i => i.Id == active.Id);
        Assert.Contains(result, i => i.Id == inactive.Id);
    }

    /// <summary>SearchAsync with nombre applies case-insensitive partial match.</summary>
    [Fact]
    public async Task SearchAsync_NombreFilter_PartialCaseInsensitiveMatch()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var harina = CreateActive($"Harina_{suffix}_0000");
        var azucar = CreateActive($"Azúcar_{suffix}_Morena");

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Ingredientes.AddRangeAsync(harina, azucar);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new IngredienteRepository(readCtx);
        var result = await repo.SearchAsync($"harina", incluirInactivos: false);

        Assert.Contains(result, i => i.Id == harina.Id);
        Assert.DoesNotContain(result, i => i.Id == azucar.Id);
    }

    /// <summary>NombreExistsForOtherAsync returns false when no other ingrediente has that nombre.</summary>
    [Fact]
    public async Task NombreExistsForOtherAsync_NoDuplicate_ReturnsFalse()
    {
        var nombre      = $"UniqueNombre_{Guid.NewGuid():N}";
        var ingrediente = CreateActive(nombre);

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Ingredientes.AddAsync(ingrediente);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new IngredienteRepository(readCtx);
        // Exclude self → should still return false (only this one has the name)
        var result = await repo.NombreExistsForOtherAsync(nombre, ingrediente.Id);

        Assert.False(result);
    }

    /// <summary>NombreExistsForOtherAsync returns true when a different ingrediente already holds the nombre.</summary>
    [Fact]
    public async Task NombreExistsForOtherAsync_DuplicateExists_ReturnsTrue()
    {
        var nombre = $"SharedNombre_{Guid.NewGuid():N}";
        var owner  = CreateActive(nombre);
        var other  = CreateActive($"OtherIngrediente_{Guid.NewGuid():N}");

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Ingredientes.AddRangeAsync(owner, other);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new IngredienteRepository(readCtx);
        // Ask "does 'nombre' exist for other (excludeId = other.Id)?" → yes, owned by 'owner'
        var result = await repo.NombreExistsForOtherAsync(nombre, other.Id);

        Assert.True(result);
    }
}
