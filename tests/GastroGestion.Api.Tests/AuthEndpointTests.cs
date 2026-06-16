using GastroGestion.Contracts.Auth;
using GastroGestion.Contracts.Pedidos;
using GastroGestion.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// Integration tests for the auth and role-claim paths.
/// Covers AUTH-05.6 (POST /auth/login HTTP scenarios) and AUTH-07-B/C (missing/bogus role claim → 403).
/// All tests tagged [Trait("Category","Integration")] — requires LocalDB.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class AuthEndpointTests
{
    private readonly HttpClient _anonymousClient;
    private readonly ApiFactory _factory;

    // Seeded admin credentials — matches appsettings.Development.json Seed:AdminEmail / Seed:AdminPassword
    // and the dev fallback documented in DevDataSeeder (ADR-9).
    private const string SeededAdminEmail    = "admin@gastrogestion.local";
    private const string SeededAdminPassword = "Admin1234!";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AuthEndpointTests(ApiFactory factory)
    {
        _factory        = factory;
        _anonymousClient = factory.CreateClient(); // no auth header
    }

    // ── AUTH-05-A: successful login returns 200 + non-empty token ────────────

    [Fact]
    public async Task POST_Auth_Login_ValidCredentials_Returns200WithToken()
    {
        var request = new LoginRequest(SeededAdminEmail, SeededAdminPassword);

        var response = await _anonymousClient.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken),
            "AccessToken must be non-empty on successful login.");
        Assert.True(body.ExpiresAtUtc > DateTime.UtcNow,
            "ExpiresAtUtc must be in the future.");
        Assert.NotEqual(Guid.Empty, body.UsuarioId);
    }

    // ── AUTH-05-B: wrong password → 401 ────────────────────────────────────

    [Fact]
    public async Task POST_Auth_Login_WrongPassword_Returns401()
    {
        var request = new LoginRequest(SeededAdminEmail, "WrongPassword999!");

        var response = await _anonymousClient.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── AUTH-05-C: unknown email → 401 ─────────────────────────────────────

    [Fact]
    public async Task POST_Auth_Login_UnknownEmail_Returns401()
    {
        var request = new LoginRequest("unknown@restaurant.com", "SomePassword123!");

        var response = await _anonymousClient.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── AUTH-05-D: missing Email field → 400 (validation) ──────────────────

    [Fact]
    public async Task POST_Auth_Login_MissingEmail_Returns400()
    {
        // Send a JSON body with no "email" property — validator fires before handler
        var payload = new { password = SeededAdminPassword };

        var response = await _anonymousClient.PostAsJsonAsync("/auth/login", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── AUTH-05-D (variant): missing Password field → 400 (validation) ──────

    [Fact]
    public async Task POST_Auth_Login_MissingPassword_Returns400()
    {
        var payload = new { email = SeededAdminEmail };

        var response = await _anonymousClient.PostAsJsonAsync("/auth/login", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── AUTH-05-E: 401 detail must not reveal credential failure type ────────

    [Fact]
    public async Task POST_Auth_Login_FailureResponse_DetailIsNonRevealing()
    {
        var request = new LoginRequest(SeededAdminEmail, "BadPassword!");

        var response = await _anonymousClient.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // The detail must not expose which part of the credential check failed (AUTH-05-E)
        foreach (var forbidden in new[] { "email", "password", "user", "found", "inactive", "wrong" })
        {
            Assert.DoesNotContain(forbidden, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── AUTH-07-B: authenticated with no role claim → 403 ──────────────────

    [Fact]
    public async Task POST_Pedidos_Transicion_NoRoleClaim_Returns403()
    {
        // Arrange: create a valid pedido with a confirmed line so we can attempt a transition.
        // Use a full Administrador client for setup.
        using var adminClient = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var platoResponse = await adminClient.PostAsJsonAsync("/platos",
            new GastroGestion.Contracts.Platos.CrearPlatoRequest("PlatoRoleB", 500m, AlicuotaIVA.General, []));
        platoResponse.EnsureSuccessStatusCode();
        var platoId = await platoResponse.Content.ReadFromJsonAsync<Guid>();

        var pedidoResponse = await adminClient.PostAsJsonAsync("/pedidos",
            new CrearPedidoRequest(TipoPedido.TakeAway, null, null, null));
        pedidoResponse.EnsureSuccessStatusCode();
        var pedidoId = await pedidoResponse.Content.ReadFromJsonAsync<Guid>();

        var lineaResponse = await adminClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/lineas",
            new AgregarLineaRequest(platoId, 1, null));
        lineaResponse.EnsureSuccessStatusCode();
        var lineaId = await lineaResponse.Content.ReadFromJsonAsync<Guid>();

        await adminClient.PostAsync($"/pedidos/{pedidoId}/lineas/{lineaId}/confirmar-precio", null);

        // Act: use a client whose JWT has NO role claim
        using var noRoleClient = _factory.CreateAuthenticatedClientWithoutRole();
        var transicionRequest = new TransicionarEstadoRequest(EstadoPedido.Preparandose);
        var response = await noRoleClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/transicion", transicionRequest);

        // Assert: role claim is absent → 403 Forbidden (not 401 — user IS authenticated)
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── AUTH-07-C: authenticated with unparseable role claim → 403 ──────────

    [Fact]
    public async Task POST_Pedidos_Transicion_BogusRoleClaim_Returns403()
    {
        // Arrange: create a valid pedido with a confirmed line.
        using var adminClient = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var platoResponse = await adminClient.PostAsJsonAsync("/platos",
            new GastroGestion.Contracts.Platos.CrearPlatoRequest("PlatoRoleC", 500m, AlicuotaIVA.General, []));
        platoResponse.EnsureSuccessStatusCode();
        var platoId = await platoResponse.Content.ReadFromJsonAsync<Guid>();

        var pedidoResponse = await adminClient.PostAsJsonAsync("/pedidos",
            new CrearPedidoRequest(TipoPedido.TakeAway, null, null, null));
        pedidoResponse.EnsureSuccessStatusCode();
        var pedidoId = await pedidoResponse.Content.ReadFromJsonAsync<Guid>();

        var lineaResponse = await adminClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/lineas",
            new AgregarLineaRequest(platoId, 1, null));
        lineaResponse.EnsureSuccessStatusCode();
        var lineaId = await lineaResponse.Content.ReadFromJsonAsync<Guid>();

        await adminClient.PostAsync($"/pedidos/{pedidoId}/lineas/{lineaId}/confirmar-precio", null);

        // Act: use a client whose JWT has an invalid/non-enum role value
        using var bogusRoleClient = _factory.CreateAuthenticatedClientWithBogusRole("InvalidRole");
        var transicionRequest = new TransicionarEstadoRequest(EstadoPedido.Preparandose);
        var response = await bogusRoleClient.PostAsJsonAsync(
            $"/pedidos/{pedidoId}/transicion", transicionRequest);

        // Assert: role claim is present but unparseable → 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
