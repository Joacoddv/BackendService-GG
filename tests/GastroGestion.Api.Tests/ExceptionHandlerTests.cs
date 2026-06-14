using GastroGestion.Api.ErrorHandling;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// Integration tests that exercise GastroGestionExceptionHandler over the real HTTP stack
/// for the three exception types it maps: ConflictException (409), DomainException (422),
/// and NotFoundException (404).
///
/// Uses a focused TestServer built entirely within the test project — no LocalDB, no
/// DevDataSeeder, no JWT pipeline. Only the real GastroGestionExceptionHandler and three
/// test-only throwing endpoints are registered. Production Program.cs is untouched.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ExceptionHandlerTests : IAsyncLifetime
{
    private IHost?     _host;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        // Register exactly the same services that Program.cs uses for error handling.
                        services.AddRouting();
                        services.AddProblemDetails();
                        services.AddExceptionHandler<GastroGestionExceptionHandler>();
                        services.AddLogging();
                    })
                    .Configure(app =>
                    {
                        // Same middleware ordering as Program.cs: exception handler first.
                        app.UseExceptionHandler();
                        app.UseRouting();

                        // Test-only throwing endpoints — never exist in production.
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/__test/throw-conflict",
                                () => { throw new ConflictException("duplicate entity — test probe"); });

                            endpoints.MapGet("/__test/throw-domain",
                                () => { throw new DomainException("invariant violated — test probe"); });

                            endpoints.MapGet("/__test/throw-notfound",
                                () => { throw new NotFoundException("entity not found — test probe"); });
                        });
                    });
            })
            .StartAsync();

        _client = _host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    // ── 409 Conflict ────────────────────────────────────────────────────────

    [Fact]
    public async Task ThrowConflict_Returns409_WithProblemJsonContentType()
    {
        var response = await _client!.GetAsync("/__test/throw-conflict");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ThrowConflict_Returns_WellFormedProblemDetails()
    {
        var response = await _client!.GetAsync("/__test/throw-conflict");
        var body     = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();

        Assert.NotNull(body);
        Assert.Equal(409, body!.Status);
        Assert.False(string.IsNullOrWhiteSpace(body.Title));
    }

    // ── 422 Unprocessable Entity ─────────────────────────────────────────────

    [Fact]
    public async Task ThrowDomain_Returns422_WithProblemJsonContentType()
    {
        var response = await _client!.GetAsync("/__test/throw-domain");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ThrowDomain_Returns_WellFormedProblemDetails()
    {
        var response = await _client!.GetAsync("/__test/throw-domain");
        var body     = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();

        Assert.NotNull(body);
        Assert.Equal(422, body!.Status);
        Assert.False(string.IsNullOrWhiteSpace(body.Title));
    }

    // ── 404 Not Found (thrown, not routing miss) ─────────────────────────────

    [Fact]
    public async Task ThrowNotFound_Returns404_WithProblemJsonContentType()
    {
        var response = await _client!.GetAsync("/__test/throw-notfound");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ThrowNotFound_Returns_WellFormedProblemDetails()
    {
        var response = await _client!.GetAsync("/__test/throw-notfound");
        var body     = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();

        Assert.NotNull(body);
        Assert.Equal(404, body!.Status);
        Assert.False(string.IsNullOrWhiteSpace(body.Title));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Minimal DTO for deserialising the RFC 7807 ProblemDetails response body.</summary>
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
