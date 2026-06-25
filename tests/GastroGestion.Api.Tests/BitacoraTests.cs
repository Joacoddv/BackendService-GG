using GastroGestion.Contracts.Bitacora;
using GastroGestion.Domain.Bitacora;
using GastroGestion.Domain.Enums;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// Integration tests for the Bitacora (audit log) feature.
/// Covers:
/// (a) A mutating call writes a Bitacora row.
/// (b) GET /bitacora returns 403 for non-Administrador, 200 for Administrador.
/// (c) A policy-protected endpoint returns 403 for wrong role, 200 for right role.
/// (d) Failed login writes an anonymous Bitacora row.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class BitacoraTests
{
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // Seeded admin credentials — see DevDataSeeder
    private const string SeededAdminEmail    = "admin@gastrogestion.local";
    private const string SeededAdminPassword = "Admin1234!";

    public BitacoraTests(ApiFactory factory)
        => _factory = factory;

    // ── (b) GET /bitacora — access control ────────────────────────────────────

    [Fact]
    public async Task GET_Bitacora_Administrador_Returns200()
    {
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var response = await client.GetAsync("/bitacora");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BitacoraPageResponse>(JsonOpts);
        Assert.NotNull(body);
    }

    [Theory]
    [InlineData(RolUsuario.Mozo)]
    [InlineData(RolUsuario.Cajero)]
    [InlineData(RolUsuario.Cocinero)]
    public async Task GET_Bitacora_NonAdmin_Returns403(RolUsuario role)
    {
        var client   = _factory.CreateAuthenticatedClient(role);
        var response = await client.GetAsync("/bitacora");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_Bitacora_Anonymous_Returns401()
    {
        var client   = _factory.CreateClient();
        var response = await client.GetAsync("/bitacora");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── (c) Policy-protected endpoint returns 403 for wrong role ──────────────

    [Fact]
    public async Task PUT_Clientes_Mozo_Returns403()
    {
        // Just hit a SoloAdministrador-gated endpoint with a Mozo token
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Mozo);
        var response = await client.PutAsJsonAsync(
            $"/clientes/{Guid.NewGuid()}",
            new { nombre = "Test", condicionIva = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_OrdenesTrabajo_Cajero_Returns403()
    {
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Cajero);
        var response = await client.GetAsync("/ordenes-trabajo");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_OrdenesTrabajo_Cocinero_Returns200()
    {
        var client   = _factory.CreateAuthenticatedClient(RolUsuario.Cocinero);
        var response = await client.GetAsync("/ordenes-trabajo");

        // 200 (empty list is fine)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── (a) Mutating call writes a Bitacora row ───────────────────────────────

    [Fact]
    public async Task POST_Platos_WritesABitacoraEntry()
    {
        var adminClient = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        // Use a unique name so we can search for it in the Bitacora
        var uniqueName = $"BitacoraTestDish_{Guid.NewGuid():N}";

        await adminClient.PostAsJsonAsync("/platos",
            new GastroGestion.Contracts.Platos.CrearPlatoRequest(
                uniqueName, 500m, GastroGestion.Domain.Enums.AlicuotaIVA.General, []));

        // Give EF time to commit (within the same scope, this should already be committed)
        // Check via GET /bitacora
        var bitacoraResponse = await adminClient.GetAsync("/bitacora");
        bitacoraResponse.EnsureSuccessStatusCode();

        var page = await bitacoraResponse.Content.ReadFromJsonAsync<BitacoraPageResponse>(JsonOpts);
        Assert.NotNull(page);

        // The "Create dish" audit row must exist, with the actor's role recorded.
        var createDishEntry = page!.Items.FirstOrDefault(e => e.Accion == "Create dish");
        Assert.True(createDishEntry is not null, "Expected a 'Create dish' entry in the audit log.");
        Assert.Equal(RolUsuario.Administrador, createDishEntry!.Rol);
        Assert.NotEqual(Guid.Empty, createDishEntry.UsuarioId);
    }

    // ── (a') A FAILED mutating action (handler throws) is still audited ────────

    [Fact]
    public async Task FailedMutatingAction_StillWritesABitacoraEntry()
    {
        var adminClient = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        // PUT a non-existent client → the handler throws NotFoundException (mapped to 404).
        // The BitacoraFilter's try/finally must still record the attempt.
        var missingId = Guid.NewGuid();
        var response  = await adminClient.PutAsJsonAsync(
            $"/clientes/{missingId}",
            new { nombre = "Ghost", condicionIva = 0 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Read the full audit log as admin and look for the failed update against this id.
        var bitacoraResponse = await adminClient.GetAsync("/bitacora");
        bitacoraResponse.EnsureSuccessStatusCode();

        var page = await bitacoraResponse.Content.ReadFromJsonAsync<BitacoraPageResponse>(JsonOpts);
        Assert.NotNull(page);

        var failedUpdate = page!.Items.FirstOrDefault(e =>
            e.Accion == "Update client" &&
            e.Detalle != null &&
            e.Detalle.Contains(missingId.ToString(), StringComparison.OrdinalIgnoreCase));

        Assert.True(
            failedUpdate is not null,
            "Expected an 'Update client' audit row for the failed (404) mutation.");
        // The audited outcome reflects the failure, not a 2xx success.
        Assert.NotInRange(failedUpdate!.ResultadoHttp, 200, 299);
    }

    // ── (d) Failed login writes anonymous Bitacora row ────────────────────────

    [Fact]
    public async Task POST_Auth_Login_Failed_WritesAnonymousBitacoraEntry()
    {
        var anonymousClient = _factory.CreateClient();

        // Attempt a login with wrong password — this writes a "Login failed" entry
        var failResponse = await anonymousClient.PostAsJsonAsync("/auth/login",
            new GastroGestion.Contracts.Auth.LoginRequest(SeededAdminEmail, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, failResponse.StatusCode);

        // Verify the admin can read at least some bitacora entries (the write-path ran)
        var adminClient      = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var bitacoraResponse = await adminClient.GetAsync("/bitacora");
        bitacoraResponse.EnsureSuccessStatusCode();

        var page = await bitacoraResponse.Content.ReadFromJsonAsync<BitacoraPageResponse>(JsonOpts);
        Assert.NotNull(page);

        // If audit writing works, we should find a "Login failed" entry for this email
        var failedEntry = page!.Items.FirstOrDefault(e =>
            e.Accion == "Login failed" &&
            e.Email.Equals(SeededAdminEmail, StringComparison.OrdinalIgnoreCase) &&
            e.UsuarioId == Guid.Empty);

        // The entry should exist if the BitacoraWriter is working correctly
        Assert.True(
            failedEntry is not null,
            "Expected a 'Login failed' anonymous entry in the audit log.");
    }

    // ── (a) Successful login writes authenticated Bitacora row ────────────────

    [Fact]
    public async Task POST_Auth_Login_Success_WritesAuthenticatedBitacoraEntry()
    {
        var anonymousClient = _factory.CreateClient();

        var loginResponse = await anonymousClient.PostAsJsonAsync("/auth/login",
            new GastroGestion.Contracts.Auth.LoginRequest(SeededAdminEmail, SeededAdminPassword));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var adminClient      = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        var bitacoraResponse = await adminClient.GetAsync("/bitacora");
        bitacoraResponse.EnsureSuccessStatusCode();

        var page = await bitacoraResponse.Content.ReadFromJsonAsync<BitacoraPageResponse>(JsonOpts);
        Assert.NotNull(page);

        var loginEntry = page!.Items.FirstOrDefault(e =>
            e.Accion == "Login" &&
            e.Email.Equals(SeededAdminEmail, StringComparison.OrdinalIgnoreCase) &&
            e.UsuarioId != Guid.Empty);

        Assert.True(loginEntry is not null, "Expected a 'Login' authenticated entry in the audit log.");
    }
}
