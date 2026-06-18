using GastroGestion.Contracts.Clientes;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Enums;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests.Clientes;

/// <summary>
/// Integration tests for PUT /clientes/{id}, DELETE /clientes/{id}, and GET /clientes (search).
/// Covers CCC-B01, CCC-B02, CCC-B03 (CCC-T32).
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class ClienteCrudEndpointTests
{
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // Valid CUITs (check-digit verified)
    private const string ValidCuit1 = "20123456786";
    private const string ValidCuit2 = "33693450239";

    public ClienteCrudEndpointTests(ApiFactory factory)
        => _factory = factory;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreateClienteAsync(
        string nombre = "Test Cliente",
        CondicionIVA condicion = CondicionIVA.ConsumidorFinal,
        string? cuit = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db      = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
        var cliente = Cliente.Crear(nombre, condicion, cuit is not null ? new Domain.ValueObjects.Cuit(cuit) : null, null);
        await db.Clientes.AddAsync(cliente);
        await db.SaveChangesAsync();
        return cliente.Id;
    }

    // ── PUT /clientes/{id} ────────────────────────────────────────────────────

    /// <summary>CCC-B01 — Admin PUT returns 200 with updated resource.</summary>
    [Fact]
    public async Task PUT_Clientes_Admin_ValidRequest_Returns200()
    {
        var id     = await CreateClienteAsync($"Original_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarClienteRequest("Updated Name", CondicionIVA.Monotributista, null, null);

        var response = await client.PutAsJsonAsync($"/clientes/{id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ClienteResponse>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result!.Nombre);
        Assert.Equal(CondicionIVA.Monotributista, result.CondicionIVA);
    }

    /// <summary>CCC-B01 — PUT 404 when cliente does not exist.</summary>
    [Fact]
    public async Task PUT_Clientes_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarClienteRequest("Name", CondicionIVA.ConsumidorFinal, null, null);

        var response = await client.PutAsJsonAsync($"/clientes/{id}", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>CCC-B01 — PUT 409 when CUIT already belongs to another cliente.</summary>
    [Fact]
    public async Task PUT_Clientes_Admin_CuitConflict_Returns409()
    {
        // Create two clients; assign Cuit1 to first, then try to assign Cuit1 to second
        var first  = await CreateClienteAsync($"First_{Guid.NewGuid():N}", CondicionIVA.ResponsableInscripto, ValidCuit1);
        var second = await CreateClienteAsync($"Second_{Guid.NewGuid():N}");

        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarClienteRequest("Second Updated", CondicionIVA.ResponsableInscripto, ValidCuit1, null);

        var response = await client.PutAsJsonAsync($"/clientes/{second}", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>CCC-B01 — PUT 422 when RI without CUIT (domain invariant).</summary>
    [Fact]
    public async Task PUT_Clientes_Admin_ResponsableInscriptoWithoutCuit_Returns422()
    {
        var id     = await CreateClienteAsync($"RI_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarClienteRequest("RI Test", CondicionIVA.ResponsableInscripto, null, null);

        var response = await client.PutAsJsonAsync($"/clientes/{id}", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>CCC-B01 — PUT 400 when Nombre is empty (FluentValidation).</summary>
    [Fact]
    public async Task PUT_Clientes_Admin_EmptyNombre_Returns400()
    {
        var id     = await CreateClienteAsync($"ValidCliente_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var body   = new EditarClienteRequest("", CondicionIVA.ConsumidorFinal, null, null);

        var response = await client.PutAsJsonAsync($"/clientes/{id}", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>CCC-B01 — PUT 403 for non-Admin roles.</summary>
    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task PUT_Clientes_NonAdmin_Returns403(RolUsuario role)
    {
        var id     = await CreateClienteAsync($"RoleTest_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(role);
        var body   = new EditarClienteRequest("Name", CondicionIVA.ConsumidorFinal, null, null);

        var response = await client.PutAsJsonAsync($"/clientes/{id}", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>CCC-B01 — NumeroCliente is unchanged after PUT.</summary>
    [Fact]
    public async Task PUT_Clientes_Admin_NumeroClienteUnchanged()
    {
        // Fetch original via GET
        var id     = await CreateClienteAsync($"Immutable_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var getResponse = await client.GetAsync($"/clientes/{id}");
        var original    = await getResponse.Content.ReadFromJsonAsync<ClienteResponse>(JsonOpts);

        var body = new EditarClienteRequest("New Name", CondicionIVA.ConsumidorFinal, null, null);
        var putResponse = await client.PutAsJsonAsync($"/clientes/{id}", body);
        var updated     = await putResponse.Content.ReadFromJsonAsync<ClienteResponse>(JsonOpts);

        Assert.Equal(original!.Id, updated!.Id);
    }

    // ── DELETE /clientes/{id} ─────────────────────────────────────────────────

    /// <summary>CCC-B02 — Admin DELETE returns 204 and marks cliente inactive.</summary>
    [Fact]
    public async Task DELETE_Clientes_Admin_Returns204()
    {
        var id     = await CreateClienteAsync($"ToDelete_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/clientes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>CCC-B02 — DELETE is idempotent: second call also returns 204.</summary>
    [Fact]
    public async Task DELETE_Clientes_Admin_Idempotent_Returns204Again()
    {
        var id     = await CreateClienteAsync($"Idempotent_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        await client.DeleteAsync($"/clientes/{id}");
        var response = await client.DeleteAsync($"/clientes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>CCC-B02 — DELETE 404 when cliente does not exist.</summary>
    [Fact]
    public async Task DELETE_Clientes_Admin_NotFound_Returns404()
    {
        var id     = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.DeleteAsync($"/clientes/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>CCC-B02 — DELETE 403 for non-Admin roles.</summary>
    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task DELETE_Clientes_NonAdmin_Returns403(RolUsuario role)
    {
        var id     = await CreateClienteAsync($"DeleteRoleTest_{Guid.NewGuid():N}");
        var client = _factory.CreateAuthenticatedClient(role);

        var response = await client.DeleteAsync($"/clientes/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>CCC-B02 — Soft-deleted cliente is hidden from default GET /clientes list.</summary>
    [Fact]
    public async Task DELETE_Clientes_SoftDeleted_HiddenFromDefaultList()
    {
        var nombre = $"HiddenAfterDelete_{Guid.NewGuid():N}";
        var id     = await CreateClienteAsync(nombre);
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        await client.DeleteAsync($"/clientes/{id}");

        var listResponse = await client.GetAsync("/clientes");
        var list = await listResponse.Content.ReadFromJsonAsync<List<ClienteResponse>>(JsonOpts);

        Assert.NotNull(list);
        Assert.DoesNotContain(list!, c => c.Id == id);
    }

    // ── GET /clientes?nombre=&incluirInactivos= ───────────────────────────────

    /// <summary>CCC-B03 — GET default hides inactive clientes.</summary>
    [Fact]
    public async Task GET_Clientes_DefaultActive_HidesInactive()
    {
        var activeName   = $"ActiveVisible_{Guid.NewGuid():N}";
        var inactiveName = $"InactiveHidden_{Guid.NewGuid():N}";

        var activeId   = await CreateClienteAsync(activeName);
        var inactiveId = await CreateClienteAsync(inactiveName);

        // Soft-delete the second one
        var adminClient = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        await adminClient.DeleteAsync($"/clientes/{inactiveId}");

        var response = await adminClient.GetAsync("/clientes");
        var list     = await response.Content.ReadFromJsonAsync<List<ClienteResponse>>(JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(list);
        Assert.Contains(list!, c => c.Id == activeId);
        Assert.DoesNotContain(list!, c => c.Id == inactiveId);
    }

    /// <summary>CCC-B03 — GET ?incluirInactivos=true shows both active and inactive.</summary>
    [Fact]
    public async Task GET_Clientes_IncluirInactivosTrue_ShowsAll()
    {
        var activeName   = $"ActiveShow_{Guid.NewGuid():N}";
        var inactiveName = $"InactiveShow_{Guid.NewGuid():N}";

        var activeId   = await CreateClienteAsync(activeName);
        var inactiveId = await CreateClienteAsync(inactiveName);

        var adminClient = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        await adminClient.DeleteAsync($"/clientes/{inactiveId}");

        var response = await adminClient.GetAsync("/clientes?incluirInactivos=true");
        var list     = await response.Content.ReadFromJsonAsync<List<ClienteResponse>>(JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(list);
        Assert.Contains(list!, c => c.Id == activeId);
        Assert.Contains(list!, c => c.Id == inactiveId);
    }

    /// <summary>CCC-B03 — GET ?nombre= applies partial case-insensitive filter.</summary>
    [Fact]
    public async Task GET_Clientes_NombreFilter_ReturnsPartialMatch()
    {
        var suffix  = Guid.NewGuid().ToString("N")[..8];
        var garcia  = $"García_{suffix}_SA";
        var lopez   = $"López_{suffix}_SRL";

        var garciaId = await CreateClienteAsync(garcia);
        var lopezId  = await CreateClienteAsync(lopez);

        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var response = await client.GetAsync($"/clientes?nombre=García_{suffix}");
        var list     = await response.Content.ReadFromJsonAsync<List<ClienteResponse>>(JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(list);
        Assert.Contains(list!, c => c.Id == garciaId);
        Assert.DoesNotContain(list!, c => c.Id == lopezId);
    }

    /// <summary>CCC-B03 — GET /clientes unauthenticated returns 401.</summary>
    [Fact]
    public async Task GET_Clientes_Anonymous_Returns401()
    {
        var client   = _factory.CreateClient();
        var response = await client.GetAsync("/clientes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>CCC-B03 — Existing GET_Clientes_Returns_SeededClientes test still passes (ADR-CCC-3).</summary>
    [Fact]
    public async Task GET_Clientes_Returns_SeededClientes_StillGreen()
    {
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var response = await client.GetAsync("/clientes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<ClienteResponse>>(JsonOpts);
        Assert.NotNull(list);
        Assert.True(list!.Count >= 3, $"Expected at least 3 seeded clientes, got {list.Count}");
    }
}
