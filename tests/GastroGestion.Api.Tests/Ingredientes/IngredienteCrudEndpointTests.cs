using GastroGestion.Contracts.Ingredientes;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Ingredientes;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests.Ingredientes;

/// <summary>
/// Integration tests for PUT /ingredientes/{id}, DELETE /ingredientes/{id}, and GET /ingredientes (search).
/// Covers CCC-C01, CCC-C02, CCC-C03 (CCC-T52).
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class IngredienteCrudEndpointTests
{
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public IngredienteCrudEndpointTests(ApiFactory factory)
        => _factory = factory;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(Guid Id, UnidadDeMedida UnidadBase)> CreateIngredienteAsync(
        string nombre,
        UnidadDeMedida unidad = UnidadDeMedida.Kilogramo)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db          = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
        var ingrediente = Ingrediente.Crear(nombre, unidad);
        await db.Ingredientes.AddAsync(ingrediente);
        await db.SaveChangesAsync();
        return (ingrediente.Id, ingrediente.UnidadBase);
    }

    // ── PUT /ingredientes/{id} ────────────────────────────────────────────────

    /// <summary>CCC-C01 — Admin PUT returns 200 with updated Nombre.</summary>
    [Fact]
    public async Task PUT_Ingredientes_Admin_ValidRequest_Returns200()
    {
        var (id, _) = await CreateIngredienteAsync($"Original_{Guid.NewGuid():N}", UnidadDeMedida.Gramo);
        var client  = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body    = new EditarIngredienteRequest("Harina 0000");

        var response = await client.PutAsJsonAsync($"/ingredientes/{id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<IngredienteResponse>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal("Harina 0000", result!.Nombre);
    }

    /// <summary>CCC-C01 — UnidadBase is unchanged after PUT (ADR-CCC-1 immutability).</summary>
    [Fact]
    public async Task PUT_Ingredientes_Admin_UnidadBaseUnchangedAfterEdit()
    {
        var (id, originalUnidad) = await CreateIngredienteAsync(
            $"Aceite_{Guid.NewGuid():N}", UnidadDeMedida.Litro);
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        // Edit only the Nombre — no UnidadBase field in request
        var body     = new EditarIngredienteRequest("Aceite de Oliva");
        var response = await client.PutAsJsonAsync($"/ingredientes/{id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<IngredienteResponse>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal(originalUnidad, result!.UnidadBase);
    }

    /// <summary>CCC-C01 — PUT 404 when ingrediente does not exist.</summary>
    [Fact]
    public async Task PUT_Ingredientes_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarIngredienteRequest("Nombre Nuevo");

        var response = await client.PutAsJsonAsync($"/ingredientes/{id}", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>CCC-C01 — PUT 409 when Nombre already belongs to another ingrediente.</summary>
    [Fact]
    public async Task PUT_Ingredientes_Admin_NombreConflict_Returns409()
    {
        var suffix   = Guid.NewGuid().ToString("N")[..8];
        var (_, _)   = await CreateIngredienteAsync($"Sal_{suffix}");
        var (id2, _) = await CreateIngredienteAsync($"Pimienta_{suffix}");

        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        // Try to rename second one to first one's name
        var body   = new EditarIngredienteRequest($"Sal_{suffix}");

        var response = await client.PutAsJsonAsync($"/ingredientes/{id2}", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>CCC-C01 — PUT 400 when Nombre is empty (FluentValidation).</summary>
    [Fact]
    public async Task PUT_Ingredientes_Admin_EmptyNombre_Returns400()
    {
        var (id, _) = await CreateIngredienteAsync($"ValidIngrediente_{Guid.NewGuid():N}");
        var client  = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body    = new EditarIngredienteRequest("");

        var response = await client.PutAsJsonAsync($"/ingredientes/{id}", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>CCC-C01 — PUT 403 for non-Admin roles.</summary>
    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task PUT_Ingredientes_NonAdmin_Returns403(RolUsuario role)
    {
        var (id, _) = await CreateIngredienteAsync($"RoleTest_{Guid.NewGuid():N}");
        var client  = _factory.CreateAuthenticatedClient(role);
        var body    = new EditarIngredienteRequest("Nuevo Nombre");

        var response = await client.PutAsJsonAsync($"/ingredientes/{id}", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>CCC-C01 — Confirm UnidadBase is unchanged via GET after PUT (end-to-end).</summary>
    [Fact]
    public async Task PUT_Ingredientes_Admin_UnidadBaseConfirmedViaGetAfterEdit()
    {
        var (id, originalUnidad) = await CreateIngredienteAsync(
            $"Harina_{Guid.NewGuid():N}", UnidadDeMedida.Kilogramo);
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        // PUT to change Nombre
        var putBody     = new EditarIngredienteRequest("Harina de Trigo 0000");
        var putResponse = await client.PutAsJsonAsync($"/ingredientes/{id}", putBody);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // GET the entity and verify UnidadBase unchanged
        var getResponse = await client.GetAsync($"/ingredientes/{id}");
        var result      = await getResponse.Content.ReadFromJsonAsync<IngredienteResponse>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal("Harina de Trigo 0000", result!.Nombre);
        Assert.Equal(originalUnidad, result.UnidadBase);
    }

    // ── DELETE /ingredientes/{id} ─────────────────────────────────────────────

    /// <summary>CCC-C02 — Admin DELETE returns 204.</summary>
    [Fact]
    public async Task DELETE_Ingredientes_Admin_Returns204()
    {
        var (id, _) = await CreateIngredienteAsync($"ToDelete_{Guid.NewGuid():N}");
        var client  = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/ingredientes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>CCC-C02 — DELETE is idempotent: second call also returns 204.</summary>
    [Fact]
    public async Task DELETE_Ingredientes_Admin_Idempotent_Returns204Again()
    {
        var (id, _) = await CreateIngredienteAsync($"Idempotent_{Guid.NewGuid():N}");
        var client  = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        await client.DeleteAsync($"/ingredientes/{id}");
        var response = await client.DeleteAsync($"/ingredientes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>CCC-C02 — DELETE 404 when ingrediente does not exist.</summary>
    [Fact]
    public async Task DELETE_Ingredientes_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/ingredientes/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>CCC-C02 — DELETE 403 for non-Admin roles.</summary>
    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task DELETE_Ingredientes_NonAdmin_Returns403(RolUsuario role)
    {
        var (id, _) = await CreateIngredienteAsync($"DeleteRoleTest_{Guid.NewGuid():N}");
        var client  = _factory.CreateAuthenticatedClient(role);

        var response = await client.DeleteAsync($"/ingredientes/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>CCC-C02 — Soft-deleted ingrediente is hidden from default GET list.</summary>
    [Fact]
    public async Task DELETE_Ingredientes_SoftDeleted_HiddenFromDefaultList()
    {
        var nombre  = $"HiddenAfterDelete_{Guid.NewGuid():N}";
        var (id, _) = await CreateIngredienteAsync(nombre);
        var client  = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        await client.DeleteAsync($"/ingredientes/{id}");

        var listResponse = await client.GetAsync("/ingredientes");
        var list = await listResponse.Content.ReadFromJsonAsync<List<IngredienteResponse>>(JsonOpts);

        Assert.NotNull(list);
        Assert.DoesNotContain(list!, i => i.Id == id);
    }

    // ── GET /ingredientes?nombre=&incluirInactivos= ───────────────────────────

    /// <summary>CCC-C03 — GET default hides inactive ingredientes.</summary>
    [Fact]
    public async Task GET_Ingredientes_DefaultActive_HidesInactive()
    {
        var activeName   = $"ActiveVisible_{Guid.NewGuid():N}";
        var inactiveName = $"InactiveHidden_{Guid.NewGuid():N}";

        var (activeId, _)   = await CreateIngredienteAsync(activeName);
        var (inactiveId, _) = await CreateIngredienteAsync(inactiveName);

        var adminClient = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        await adminClient.DeleteAsync($"/ingredientes/{inactiveId}");

        var response = await adminClient.GetAsync("/ingredientes");
        var list     = await response.Content.ReadFromJsonAsync<List<IngredienteResponse>>(JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(list);
        Assert.Contains(list!, i => i.Id == activeId);
        Assert.DoesNotContain(list!, i => i.Id == inactiveId);
    }

    /// <summary>CCC-C03 — GET ?incluirInactivos=true shows both active and inactive.</summary>
    [Fact]
    public async Task GET_Ingredientes_IncluirInactivosTrue_ShowsAll()
    {
        var activeName   = $"ActiveShow_{Guid.NewGuid():N}";
        var inactiveName = $"InactiveShow_{Guid.NewGuid():N}";

        var (activeId, _)   = await CreateIngredienteAsync(activeName);
        var (inactiveId, _) = await CreateIngredienteAsync(inactiveName);

        var adminClient = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        await adminClient.DeleteAsync($"/ingredientes/{inactiveId}");

        var response = await adminClient.GetAsync("/ingredientes?incluirInactivos=true");
        var list     = await response.Content.ReadFromJsonAsync<List<IngredienteResponse>>(JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(list);
        Assert.Contains(list!, i => i.Id == activeId);
        Assert.Contains(list!, i => i.Id == inactiveId);
    }

    /// <summary>CCC-C03 — GET ?nombre= applies partial case-insensitive filter.</summary>
    [Fact]
    public async Task GET_Ingredientes_NombreFilter_ReturnsPartialMatch()
    {
        var suffix       = Guid.NewGuid().ToString("N")[..8];
        var harinaName   = $"Harina_{suffix}";
        var azucarName   = $"Azúcar_{suffix}";

        var (harinaId, _) = await CreateIngredienteAsync(harinaName);
        var (azucarId, _) = await CreateIngredienteAsync(azucarName);

        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var response = await client.GetAsync($"/ingredientes?nombre=Harina_{suffix}");
        var list     = await response.Content.ReadFromJsonAsync<List<IngredienteResponse>>(JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(list);
        Assert.Contains(list!, i => i.Id == harinaId);
        Assert.DoesNotContain(list!, i => i.Id == azucarId);
    }

    /// <summary>CCC-C03 — GET /ingredientes unauthenticated returns 401.</summary>
    [Fact]
    public async Task GET_Ingredientes_Anonymous_Returns401()
    {
        var client   = _factory.CreateClient();
        var response = await client.GetAsync("/ingredientes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>CCC-C03 — Seeded ingredientes visible in default GET (ADR-CCC-3 regression guard).</summary>
    [Fact]
    public async Task GET_Ingredientes_Returns_SeededIngredientes_StillGreen()
    {
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var response = await client.GetAsync("/ingredientes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<IngredienteResponse>>(JsonOpts);
        Assert.NotNull(list);
        Assert.True(list!.Count >= 5, $"Expected at least 5 seeded ingredientes, got {list.Count}");
    }
}
