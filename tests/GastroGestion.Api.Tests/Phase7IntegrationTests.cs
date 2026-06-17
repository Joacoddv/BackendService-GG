using GastroGestion.Domain.Enums;
using System.Net;
using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// Phase 7 integration tests: CORS policy and KitchenHub authorization.
///
/// SignalR WebSocket upgrade test limitation:
/// WebApplicationFactory uses an in-memory transport that does not support a true WebSocket upgrade.
/// Hub authorization is verified via the SignalR negotiate endpoint (POST /hubs/kitchen/negotiate),
/// which runs the full auth pipeline and returns 401/403 before any WebSocket handshake.
/// This covers the security contract without requiring the Microsoft.AspNetCore.SignalR.Client package.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.CollectionName)]
public sealed class Phase7IntegrationTests
{
    private readonly ApiFactory _factory;

    public Phase7IntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // ── CORS ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// CORS-01: Preflight request from a configured allowed origin returns
    /// Access-Control-Allow-Origin with that exact origin.
    /// AllowCredentials() requires an explicit origin echo, not a wildcard.
    /// </summary>
    [Fact]
    public async Task CORS_Preflight_AllowedOrigin_ReturnsAccessControlHeader()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/ordenes-trabajo");
        request.Headers.Add("Origin", "https://localhost:7173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        var response = await client.SendAsync(request);

        // Preflight returns 204 No Content or 200 OK — both are valid CORS preflight responses.
        Assert.True(
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 204 or 200 for CORS preflight, got {(int)response.StatusCode}.");

        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Missing Access-Control-Allow-Origin header on CORS preflight response.");

        var allowOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        Assert.Equal("https://localhost:7173", allowOrigin);
    }

    /// <summary>
    /// CORS-02: Actual GET request from an allowed origin returns
    /// Access-Control-Allow-Origin in the response headers.
    /// Uses an authenticated client so the endpoint returns 200, not 401.
    /// </summary>
    [Fact]
    public async Task CORS_ActualRequest_AllowedOrigin_ReturnsAccessControlHeader()
    {
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);
        client.DefaultRequestHeaders.Add("Origin", "https://localhost:7173");

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Missing Access-Control-Allow-Origin header on CORS actual request.");
    }

    /// <summary>
    /// CORS-03: Request from an origin NOT in AllowedOrigins does NOT receive
    /// an Access-Control-Allow-Origin header (browser will block the request).
    /// </summary>
    [Fact]
    public async Task CORS_Request_DisallowedOrigin_NoAccessControlHeader()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", "https://evil.example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.False(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Disallowed origin must NOT receive Access-Control-Allow-Origin header.");
    }

    // ── KitchenHub authorization (via negotiate endpoint) ────────────────────

    /// <summary>
    /// HUB-01: Unauthenticated negotiate request to /hubs/kitchen → 401 Unauthorized.
    /// The [Authorize] attribute on KitchenHub is enforced before the WebSocket upgrade.
    /// </summary>
    [Fact]
    public async Task KitchenHub_Negotiate_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/hubs/kitchen/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// HUB-02: Negotiate with a valid Mozo token → 403 Forbidden.
    /// The hub is restricted to Cocinero and Administrador only.
    /// </summary>
    [Fact]
    public async Task KitchenHub_Negotiate_WrongRole_Mozo_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Mozo);

        var response = await client.PostAsync("/hubs/kitchen/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// HUB-03: Negotiate with a valid Cocinero token → 200 OK (hub accepts the connection).
    /// </summary>
    [Fact]
    public async Task KitchenHub_Negotiate_Cocinero_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Cocinero);

        var response = await client.PostAsync("/hubs/kitchen/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// HUB-04: Negotiate with a valid Administrador token → 200 OK (hub accepts the connection).
    /// </summary>
    [Fact]
    public async Task KitchenHub_Negotiate_Administrador_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(RolUsuario.Administrador);

        var response = await client.PostAsync("/hubs/kitchen/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// HUB-05: Negotiate with a token passed via ?access_token= query parameter → 200 OK.
    /// This is the SignalR WebSocket path: browsers cannot set Authorization header on WS upgrade.
    /// The OnMessageReceived event handler in AddJwtBearer extracts the token from the query string.
    /// </summary>
    [Fact]
    public async Task KitchenHub_Negotiate_QueryStringToken_Cocinero_Returns200()
    {
        // No Authorization header — token is in query string (SignalR browser/Blazor flow)
        var client = _factory.CreateClient();
        var token  = _factory.GenerateTestToken(RolUsuario.Cocinero);
        var url    = $"/hubs/kitchen/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(token)}";

        var response = await client.PostAsync(url, null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
