using FluentValidation;
using GastroGestion.Api.Endpoints;
using GastroGestion.Api.ErrorHandling;
using GastroGestion.Api.Hubs;
using GastroGestion.Api.Realtime;
using GastroGestion.Application;
using GastroGestion.Application.Abstractions.Realtime;
using GastroGestion.Contracts.Clientes;
using GastroGestion.Infrastructure;
using GastroGestion.Infrastructure.Persistence;
using GastroGestion.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// 2. Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Health checks
builder.Services.AddHealthChecks();

// 4. RFC 7807 ProblemDetails + exception handler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GastroGestionExceptionHandler>();

// 5. FluentValidation — scan the Contracts assembly
builder.Services.AddValidatorsFromAssemblyContaining<CrearClienteRequest>();

// 5c. SignalR — realtime kitchen board (OT-05, ADR-003)
builder.Services.AddSignalR();
builder.Services.AddScoped<IKitchenNotifier, SignalRKitchenNotifier>();

// 5b. Serialize enums as strings globally (W-03 — better Swagger DX; still accepts integers on input)
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// 6. JWT authentication + authorization
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    // Fail fast with a clear message — never silently proceed with a missing key
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. " +
        "Set it via user-secrets (dev) or the Jwt__SigningKey environment variable (CI/prod).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSigningKey))
        };
    });

builder.Services.AddAuthorization();

// 7. Swagger / OpenAPI (Swashbuckle only — Microsoft.AspNetCore.OpenApi removed)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'",
        Name        = "Authorization",
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Middleware pipeline:

// 1. Exception handler MUST be first so it wraps the entire pipeline
app.UseExceptionHandler();

// 2. Dev-only: auto-migrate then seed
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();
    await db.Database.MigrateAsync();
    await DevDataSeeder.SeedAsync(scope.ServiceProvider);
}

// 3. Dev-only: Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. Request logging
app.UseSerilogRequestLogging();

// 5. Authentication + authorization
app.UseAuthentication();
app.UseAuthorization();

// 6. Health endpoint
app.MapHealthChecks("/health");

// 7. Endpoint groups
app.MapAuthEndpoints();
app.MapClienteEndpoints();
app.MapIngredienteEndpoints();
app.MapPlatoEndpoints();
app.MapMenuEndpoints();
app.MapMesaEndpoints();
app.MapPedidoEndpoints();
app.MapOrdenTrabajoEndpoints();
app.MapHub<KitchenHub>("/hubs/kitchen");
app.MapFacturaEndpoints();
app.MapStockEndpoints();

// 8. Run
app.Run();

// Expose Program class for WebApplicationFactory<Program> in test projects.
public partial class Program { }
