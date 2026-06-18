using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;
using GastroGestion.Infrastructure.Persistence.Repositories;
using GastroGestion.Infrastructure.Tests.Common;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Clientes;

/// <summary>
/// Integration tests for ClienteRepository.SearchAsync and CuitExistsForOtherAsync (CCC-T15, CCC-T31).
/// Uses LocalDbFixture for isolated per-class database.
/// </summary>
public sealed class ClienteSearchTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public ClienteSearchTests(LocalDbFixture fixture) => _fixture = fixture;

    // Valid CUITs (check-digit verified)
    private const string Cuit1 = "20123456786"; // 20-12345678-6
    private const string Cuit2 = "33693450239"; // 33-69345023-9

    private static Cliente CreateActive(string nombre, string? cuit = null)
    {
        var cuitObj = cuit is not null ? new Cuit(cuit) : null;
        return Cliente.Crear(nombre, cuit is not null ? CondicionIVA.ResponsableInscripto : CondicionIVA.ConsumidorFinal, cuitObj, null);
    }

    private static Cliente CreateInactive(string nombre)
    {
        var c = Cliente.Crear(nombre, CondicionIVA.ConsumidorFinal, null, null);
        c.Desactivar();
        return c;
    }

    /// <summary>SearchAsync without nombre returns only active clientes by default.</summary>
    [Fact]
    public async Task SearchAsync_DefaultActive_ExcludesInactive()
    {
        var active   = CreateActive($"Active_{Guid.NewGuid():N}");
        var inactive = CreateInactive($"Inactive_{Guid.NewGuid():N}");

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Clientes.AddRangeAsync(active, inactive);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new ClienteRepository(readCtx);
        var result = await repo.SearchAsync(null, incluirInactivos: false);

        Assert.Contains(result, c => c.Id == active.Id);
        Assert.DoesNotContain(result, c => c.Id == inactive.Id);
    }

    /// <summary>SearchAsync with incluirInactivos=true returns both active and inactive.</summary>
    [Fact]
    public async Task SearchAsync_IncluirInactivos_ReturnsBothActiveAndInactive()
    {
        var active   = CreateActive($"ActiveInc_{Guid.NewGuid():N}");
        var inactive = CreateInactive($"InactiveInc_{Guid.NewGuid():N}");

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Clientes.AddRangeAsync(active, inactive);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new ClienteRepository(readCtx);
        var result = await repo.SearchAsync(null, incluirInactivos: true);

        Assert.Contains(result, c => c.Id == active.Id);
        Assert.Contains(result, c => c.Id == inactive.Id);
    }

    /// <summary>SearchAsync with nombre applies case-insensitive partial match.</summary>
    [Fact]
    public async Task SearchAsync_NombreFilter_PartialCaseInsensitiveMatch()
    {
        var suffix  = Guid.NewGuid().ToString("N")[..8];
        var garcia  = CreateActive($"García_{suffix}_SA");
        var lopez   = CreateActive($"López_{suffix}_SRL");

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Clientes.AddRangeAsync(garcia, lopez);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new ClienteRepository(readCtx);
        var result = await repo.SearchAsync($"garc", incluirInactivos: false);

        Assert.Contains(result, c => c.Id == garcia.Id);
        Assert.DoesNotContain(result, c => c.Id == lopez.Id);
    }

    /// <summary>CuitExistsForOtherAsync returns false when no other cliente has that CUIT.</summary>
    [Fact]
    public async Task CuitExistsForOtherAsync_NoDuplicate_ReturnsFalse()
    {
        var cliente = CreateActive($"RI_{Guid.NewGuid():N}", Cuit1);

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Clientes.AddAsync(cliente);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new ClienteRepository(readCtx);
        var result = await repo.CuitExistsForOtherAsync(Cuit1, cliente.Id);

        Assert.False(result);
    }

    /// <summary>CuitExistsForOtherAsync returns true when a different cliente already holds the CUIT.</summary>
    [Fact]
    public async Task CuitExistsForOtherAsync_DuplicateExists_ReturnsTrue()
    {
        var owner = CreateActive($"Owner_{Guid.NewGuid():N}", Cuit2);
        var other = CreateActive($"Other_{Guid.NewGuid():N}");

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Clientes.AddRangeAsync(owner, other);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = _fixture.CreateContext();
        var repo   = new ClienteRepository(readCtx);
        // Ask "does cuit2 exist for other (excludeId = other.Id)?" → yes, owned by owner
        var result = await repo.CuitExistsForOtherAsync(Cuit2, other.Id);

        Assert.True(result);
    }
}
