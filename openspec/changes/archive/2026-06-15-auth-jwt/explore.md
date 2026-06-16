# Exploration: auth-jwt (Phase 5 of 7)

## Current State

### JWT Pipeline (already wired — Program.cs:39-66)

`AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` with full `TokenValidationParameters`:
`ValidateIssuer=true`, `ValidateAudience=true`, `ValidateLifetime=true`, `ValidateIssuerSigningKey=true`.
`ValidIssuer`/`ValidAudience` both read `"GastroGestion"` from appsettings.
`IssuerSigningKey` = `SymmetricSecurityKey(UTF8(Jwt:SigningKey))`.

Startup guard (Program.cs:40-47): throws `InvalidOperationException` if `Jwt:SigningKey` is null/empty — fail-fast.
`AddAuthorization()` registered. `UseAuthentication()` + `UseAuthorization()` in pipeline after exception handler, before endpoints.
Swagger has Bearer security definition and global security requirement (Program.cs:70-94).

No `RequireAuthorization()` globally or per group. All ~25 endpoints carry individual `[AllowAnonymous]`.

### appsettings Config Shape

```json
// appsettings.json
"Jwt": { "Issuer": "GastroGestion", "Audience": "GastroGestion", "SigningKey": "" }
"ConnectionStrings": { "GastroGestion": "" }

// appsettings.Development.json
"ConnectionStrings": { "GastroGestion": "Server=(localdb)\\mssqllocaldb;Database=GastroGestion;..." }
```

No `GastroGestion_Seguridad` connection string in the .NET 8 appsettings. Must be added if Option A (second DbContext) is chosen.

---

## Findings

### Finding 1 — PHASE-5 Marker: PedidoRequests.cs:26

File: `src/GastroGestion.Contracts/Pedidos/PedidoRequests.cs:26`
Current: XML doc marks it as "PHASE-5 seam: Rol is supplied from the request body."
`TransicionarEstadoRequest(EstadoPedido EstadoNuevo, RolUsuario Rol)` — `Rol` is a body field.
Must become: `TransicionarEstadoRequest(EstadoPedido EstadoNuevo)` — drop `Rol` entirely.

### Finding 2 — PHASE-5 Marker: PedidoEndpoints.cs:61

File: `src/GastroGestion.Api/Endpoints/PedidoEndpoints.cs:61`
Current: `// PHASE-5: replace body-supplied Rol with JWT claim (User.FindFirst(ClaimTypes.Role))`
          `await handler.Handle(request.ToCommand(id), ct);`
Must become: extract `ClaimTypes.Role` from `HttpContext.User`, parse to `RolUsuario`, build command.
The endpoint delegate needs `HttpContext` injected (available as a Minimal API parameter).

### Finding 3 — PHASE-5 Marker: TransicionarEstadoPedidoHandler.cs:22

File: `src/GastroGestion.Application/Pedidos/TransicionarEstadoPedido/TransicionarEstadoPedidoHandler.cs:22`
Current: `// PHASE-5: replace body-supplied Rol with JWT claim`
          `pedido.TransicionarEstado(cmd.EstadoNuevo, cmd.Rol);`
The handler itself does not change. The change is upstream: `cmd.Rol` will be populated from the JWT claim (extracted at the endpoint) rather than from the request body. The command record stays the same.

### Finding 4 — Test seam markers (TransactionalEndpointTests.cs:175, :194)

Two test methods mark the seam: `// PHASE-5 seam: Rol from body`. These tests pass `RolUsuario` in the JSON body. Phase 5 must rewrite them to attach a JWT bearer token instead.

### Finding 5 — AllowAnonymous Coverage (all ~25 endpoints, individual attributes)

Every `MapPost`/`MapGet` lambda carries `[AllowAnonymous]` inline (not group-level `.AllowAnonymous()`). Endpoints affected:
- ClienteEndpoints: 3 endpoints
- IngredienteEndpoints: 3 endpoints
- PlatoEndpoints: 3 endpoints
- MenuEndpoints: 3 endpoints
- MesaEndpoints: 3 endpoints
- PedidoEndpoints: 5 endpoints
- FacturaEndpoints: 3 endpoints
- StockEndpoints: 2 endpoints

`/health` has no `[AllowAnonymous]` and should remain unauthenticated (health checks bypass auth by default).

### Finding 6 — RolUsuario enum (Domain)

File: `src/GastroGestion.Domain/Enums/RolUsuario.cs`
Values: `Administrador=0`, `Cajero=1`, `Mozo=2`, `Cocinero=3`.
Only `TransicionarEstadoPedido` consumes the role today. Domain validates allowed transitions per role.

### Finding 7 — Domain aggregate pattern (from Cliente)

File: `src/GastroGestion.Domain/Clientes/Cliente.cs`
Pattern: `AggregateRoot` base → private setters → EF Core `private Cliente() {}` → private constructor → static `Crear(...)` factory with invariant validation → `DomainException` for violations → `Guid.NewGuid()` as identity. No framework dependencies.

`Usuario` aggregate MUST mirror this exactly.

### Finding 8 — EF Core persistence pattern

Port: `IClienteRepository` in `src/GastroGestion.Application/Abstractions/Persistence/`.
Implementation: `ClienteRepository` in `src/GastroGestion.Infrastructure/Persistence/Repositories/`.
Config: `ClienteConfiguration : IEntityTypeConfiguration<Cliente>` — registered via `ApplyConfigurationsFromAssembly`.
DbContext: `GastroGestionDbContext` — single context, 8 aggregate DbSets. No second DbContext today.
DI: `services.AddDbContext<GastroGestionDbContext>` in `Infrastructure/DependencyInjection.cs`.
Migration: `dotnet ef migrations add <Name> --project src/GastroGestion.Infrastructure --startup-project src/GastroGestion.Api`.

### Finding 9 — Contracts pattern (4 files per aggregate)

`{Aggregate}Requests.cs` — sealed records. `{Aggregate}Responses.cs` — sealed records. `{Aggregate}Validators.cs` — `AbstractValidator<T>`. `{Aggregate}Mappings.cs` — static `ToCommand`/`ToResponse` extensions. No AutoMapper. Validators scanned via `AddValidatorsFromAssemblyContaining<CrearClienteRequest>()`.

### Finding 10 — Use-case handler pattern

One handler class per use case. Constructor injection of repository + IUnitOfWork. `Handle(Command, CancellationToken)` method. Registered as `services.AddScoped<XyzHandler>()` in `Application/DependencyInjection.cs`. Injected directly into Minimal API endpoint delegates.

### Finding 11 — Test pattern (ApiFactory)

`ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime` — uses `GastroGestion_ApiTests` isolated LocalDB. Injects `Jwt:SigningKey = "TestSigningKeyForApiTestsMinimumLength32Chars"` at startup. Disposes via `EnsureDeletedAsync()`. Shared via `IntegrationTestCollection` fixture. Tests tagged `[Trait("Category","Integration")]`.

Auth tests will need a `GenerateTestToken(RolUsuario role)` helper on `ApiFactory` using `JwtSecurityTokenHandler` with the test signing key, issuer, and audience.

### Finding 12 — Legacy GastroGestion_Seguridad schema

Legacy `[dbo].[Usuario]` columns: `IdUsuario` (Guid), `Numero_Usuario` (int), `Mail` (string/unique), `Nombre`, `Apellido`, `Fecha_Alta` (DateTime), `PasswordHash` (byte[]), `PasswordSalt` (byte[]), `Estado` (bool), `Idioma` (string).
No `Rol` column. Uses HMACSHA512 with separate salt byte[] columns. Role was added at the API layer from external source.
The legacy Patente/Familia permission model does not map to the 4-value `RolUsuario` enum.

Data migration from legacy: not feasible without re-entering passwords and manually assigning roles. Seed initial admin instead.

---

## Risks

1. **Test suite breaks on AllowAnonymous removal**: All 222 integration tests call endpoints without a token. Token-generation helper must ship in the same PR as the first batch of protected endpoints.
2. **TransicionarEstadoRequest breaking change**: Dropping the `Rol` field changes the request shape. Explicit in the proposal.
3. **Single-catalog deviation**: Documented; Usuarios go into GastroGestion for Phase 5. Phase 6 may need to move the table.
4. **PasswordHasher in Infrastructure only**: Domain stores `string PasswordHash`. Hashing happens in `LoginHandler` or a domain service — keep it out of the aggregate to preserve the zero-dependency rule.
5. **JwtSecurityTokenHandler vs JsonWebTokenHandler**: Use `JwtSecurityTokenHandler` (consistent with existing wiring); the newer `JsonWebTokenHandler` requires different claim parsing and could introduce subtle bugs.
