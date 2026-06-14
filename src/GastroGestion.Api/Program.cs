using GastroGestion.Application;
using GastroGestion.Infrastructure;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// --- Application and Infrastructure layers ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- Health checks ---
builder.Services.AddHealthChecks();

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Startup guard: JWT signing key must be configured ---
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    // Fail fast with a clear message — never silently proceed with a missing key
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. " +
        "Set it via user-secrets (dev) or the Jwt__SigningKey environment variable (CI/prod).");
}

var app = builder.Build();

// --- Auto-migrate on startup in Development ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
    await db.Database.MigrateAsync();
}

// --- Middleware pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.MapHealthChecks("/health");

app.Run();
