# Web API Specification — GastroGestion

**Last updated:** 2026-06-17  
**Phase:** 4–5 of 7 in the .NET 8 strangler roadmap  
**Scope:** GastroGestion Web API layer — .NET 8 Minimal API endpoints, request/response contracts, middleware, authentication shape, JWT token issuance, authorization enforcement.

---

## Overview

The Web API layer exposes the persisted Phase-3 domain and completed application use cases over HTTP via ASP.NET Core Minimal APIs. The contract is REST + RFC 7807 problem details; all endpoints operate on DTOs (never domain aggregates). Authentication is now fully active (Phase 5): JWT tokens are issued by `POST /auth/login`, most endpoints require `[Authorize]`, and role information is extracted from the JWT claim for authorization decisions. Error handling is centralized via `IExceptionHandler` + `AddProblemDetails()`.

**Status:** Phase 4 complete — 3 slices (Foundation, Catalogue, Transactional+Fiscal+Stock) delivered via PRs #9, #10, #11 to main. Phase 5 (auth-jwt) complete — 2 stacked PRs (PR #12, PR #13) merged to main. Phase 6 (ordentrabajo-workflow) complete — 2 stacked PRs (PR #14, PR #15) merged to main. Catalog CRUD + Cocineros (catalog-crud-and-cocineros) complete — 3 stacked PRs (#19, #20, #21) merged to main. All 30 Phase-4 tasks + 22 Phase-5 tasks + 18 Phase-6 tasks + 52 catalog-crud tasks (CCC-T01..T52) complete. Test suite: 413 tests green (179 domain + 53 app + 46 infra + 135 api). Verification: all three PRs PASS/PASS-WITH-WARNINGS, 0 CRITICAL.

---

## Requirements Catalog — REQ-01 to REQ-20

### REQ-01 — W-01: `IEfectivoPrecioService` is async; Domain stays zero-dependency

`IEfectivoPrecioService` (in `GastroGestion.Domain.Services`) declares an async method signature returning `Task<(Dinero Precio, PorcentajeIVA IVA)>`. The application-layer implementation (`EfectivoPrecioService`) awaits repository calls with no `.GetAwaiter().GetResult()` or `.Result` access. `GastroGestion.Domain.csproj` retains zero `<PackageReference>` and zero `<ProjectReference>` elements (`Task` is BCL).

**Locked signature:**
```csharp
public interface IEfectivoPrecioService
{
    Task<(Dinero Precio, PorcentajeIVA IVA)> ResolverPrecioEfectivoAsync(
        Guid platoId, DateOnly fecha, CancellationToken ct = default);
}
```

---

### REQ-02 — RFC 7807 ProblemDetails error mapping

`GastroGestionExceptionHandler : IExceptionHandler` registered via `AddExceptionHandler<>()` + `AddProblemDetails()` translates domain and application exceptions to RFC 7807 responses:

| Exception type | HTTP status | ProblemDetails title |
|---|---|---|
| `ConflictException` | 409 Conflict | "Business rule conflict" |
| `NotFoundException` | 404 Not Found | "Resource not found" |
| `DomainException` | 422 Unprocessable Entity | "Domain rule violation" |
| `ValidationException` | 422 Unprocessable Entity | "Validation failed" |
| `ForbiddenException` | 403 Forbidden | "Forbidden" |
| Any other unhandled exception | 500 Internal Server Error | "An unexpected error occurred" |

No endpoint contains a try/catch for these types. `app.UseExceptionHandler()` is registered first in the middleware pipeline.

---

### REQ-03 — FluentValidation endpoint filter → 400 ValidationProblem

A reusable `WithValidation<T>()` endpoint filter applies `AbstractValidator<T>` to request DTOs and short-circuits to `ValidationProblem` (400) before handler execution on validation failures. Validators are registered via `AddValidatorsFromAssemblyContaining<T>()` scanning the `Contracts` assembly.

---

### REQ-04 — JWT authentication pipeline wired; all Phase-4 endpoints anonymous

`Program.cs` calls `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`, `UseAuthentication()`, and `UseAuthorization()`. All Phase-4 endpoints carry `[AllowAnonymous]`. No endpoint requires a bearer token in this phase.

---

### REQ-05 — DevDataSeeder seeds realistic dataset in Development only; idempotent

`DevDataSeeder` in `GastroGestion.Infrastructure` is called from `Program.cs` after auto-migrate, ONLY when `app.Environment.IsDevelopment()` is true. On first run, it creates:
- 3 Clientes (ConsumidorFinal, ResponsableInscripto with valid CUIT, ExentoIVA)
- 5 Ingredientes with varied `UnidadDeMedida`
- 3 Platos with `LineaReceta` entries
- 1 Menu with `FechaVigencia = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)` (computed at runtime) and one `MenuItem` with `PrecioOverride`
- 4 Mesas with varied capacities
- 1 Salon-type Pedido and 1 TakeAway-type Pedido (each with at least one LineaPedido with confirmed price)
- 1 TicketInterno Factura created from the TakeAway Pedido

All entities created via domain factory methods (no raw SQL, no bypassing factories). On second run, the seeder returns without inserting if `Clientes.AnyAsync()` returns true (idempotent).

---

### REQ-06 — Health endpoint + Swagger dev-only

A health endpoint returns 200 OK at `GET /health`. Swagger UI is served only when `app.Environment.IsDevelopment()` is true. `Microsoft.AspNetCore.OpenApi` is NOT present in `GastroGestion.Api.csproj` (Swashbuckle 6.6.2 only).

---

### REQ-07 — Api.Tests project with WebApplicationFactory + LocalDB smoke test

`tests/GastroGestion.Api.Tests/` is a valid xUnit project referencing `WebApplicationFactory<Program>` and LocalDB. It contains smoke tests (health, seeder boot, ProblemDetails shape) all tagged `[Trait("Category","Integration")]`.

---

### REQ-08 — All repository ports gain GetAllAsync; implementations load full owned graph

Every repository port in `GastroGestion.Application.Abstractions.Persistence` for the catalogue aggregates (`IClienteRepository`, `IIngredienteRepository`, `IPlatoRepository`, `IMenuRepository`, `IMesaRepository`) adds `Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)`. Each EF Core implementation uses the same eager-include strategy as the existing `GetByIdAsync` (full owned-entity graph loaded without lazy-loading).

---

### REQ-09 — Cliente endpoints: POST (create) + GET (by id) + GET (all)

**`/clientes` RouteGroupBuilder:**
- `POST /clientes` — validates request DTO with `ClienteValidator`, calls `CrearClienteHandler`, returns 201 Created with Location header. Invalid CUIT for `ResponsableInscripto` returns 422 (DomainException from `Cliente.Crear`).
- `GET /clientes/{id}` — calls `GetClienteByIdHandler`; returns 200 OK with `ClienteResponse` or 404 Not Found.
- `GET /clientes` — calls `GetAllClientesHandler`; returns 200 OK with `IReadOnlyList<ClienteResponse>`.

No domain aggregate type (`Cliente`) appears in any request or response contract — only Contracts DTOs cross the HTTP boundary.

---

### REQ-10 — Ingrediente endpoints: POST (create) + GET (by id) + GET (all)

**`/ingredientes` RouteGroupBuilder:**
- `POST /ingredientes` — validates with `IngredienteValidator`; returns 201 Created. Blank name returns 422.
- `GET /ingredientes/{id}` — returns 200 OK with `IngredienteResponse` or 404.
- `GET /ingredientes` — returns 200 OK with array.

---

### REQ-11 — Plato endpoints: POST (create) + GET (by id) + GET (all)

**`/platos` RouteGroupBuilder:**
- `POST /platos` — validates with `PlatoValidator`; returns 201 Created. Negative `PrecioBase` or blank `NombrePlato` returns 422. Response includes `Receta` lines.
- `GET /platos/{id}` — returns 200 OK with `PlatoResponse` or 404.
- `GET /platos` — returns 200 OK with array.

---

### REQ-12 — Menu endpoints: POST (create) + GET (by id) + GET (all)

**`/menus` RouteGroupBuilder:**
- `POST /menus` — validates with `MenuValidator`; returns 201 Created. Past `FechaVigencia` returns 422 (DomainException from `Menu.Crear`). **Parameter name is `FechaVigencia`, not `fechaMenu`.**
- `GET /menus/{id}` — returns 200 OK with `MenuResponse` or 404.
- `GET /menus` — returns 200 OK with array.

---

### REQ-13 — Mesa endpoints: POST (create) + GET (by id) + GET (all)

**`/mesas` RouteGroupBuilder:**
- `POST /mesas` — validates with `MesaValidator`; returns 201 Created. Zero or negative `Capacidad` returns 422.
- `GET /mesas/{id}` — returns 200 OK with `MesaResponse` or 404.
- `GET /mesas` — returns 200 OK with array.

---

### REQ-14 — Catalogue GET-all endpoints return seeded data together

After DevDataSeeder has run in Development, GET-all calls to each catalogue endpoint return at least the seeded entities for their respective aggregate types, confirming the full pipeline (seeder → repository → handler → endpoint → DTO) is wired correctly.

---

### REQ-15 — Pedido lifecycle endpoints

**`/pedidos` RouteGroupBuilder:**
- `POST /pedidos` — creates a Pedido via `CrearPedidoHandler`. For `TipoPedido.Salon`, a null `MesaId` returns 422. Returns 201 Created.
- `POST /pedidos/{id}/lineas` — adds a line via `AgregarLineaHandler`. Returns 201 Created (line id). Pedido not found returns 404.
- `POST /pedidos/{id}/lineas/{lineaId}/confirmar-precio` — resolves price via the now-async `IEfectivoPrecioService` through `ConfirmarPrecioLineaHandler`. Returns 204 No Content. Second confirmation attempt returns 422 (DomainException from set-once invariant).
- `POST /pedidos/{id}/transicion` — transitions state via `TransicionarEstadoPedidoHandler`. Request body includes `RolUsuario` (temporary security seam — Phase 5 closes via JWT claim). Invalid transition or unauthorized role returns 422. Returns 200 OK.
- `GET /pedidos/{id}` — returns 200 OK with `PedidoResponse` or 404.

DTOs in `GastroGestion.Contracts` only; `Pedido` aggregate is never serialized directly.

---

### REQ-16 — Factura endpoints: POST (create) + register payment + GET (by id)

**`/facturas` RouteGroupBuilder:**
- `POST /facturas` — wires the existing `CrearFacturaHandler`; multi-client Pedidos return 409 Conflict (ConflictException); Pedido with no confirmed lines returns 409; returns 201 Created with Location header.
- `POST /facturas/{id}/pagos` — registers a payment via `RegistrarPagoHandler`; attempting to pay a Pagada/Cancelada Factura returns 422; returns 200 OK.
- `GET /facturas/{id}` — returns 200 OK with `FacturaResponse` or 404.

---

### REQ-17 — Stock endpoints: register movement + GET balance

**`/stock` RouteGroupBuilder:**
- `POST /stock/movimientos` — creates a `MovimientoStock` via `RegistrarMovimientoStockHandler` using the domain factory; returns 201 Created. Append-only constraint is preserved (no update/delete path exists).
- `GET /stock/balance/{ingredienteId}` — returns the current net balance via `IMovimientoStockRepository.CalcularBalanceAsync`; returns 200 OK with a numeric balance value. `IngredienteId` not found (zero movements) returns 0 (not 404), consistent with a zero-balance interpretation.

---

### REQ-18 — Domain aggregates are never serialized over the wire

No endpoint response directly serializes a domain aggregate type (`Cliente`, `Pedido`, `Factura`, `Mesa`, `Plato`, `Menu`, `Ingrediente`, `MovimientoStock`). All request and response contracts use types from `GastroGestion.Contracts`. Mapping is performed via hand-written static extension methods; no AutoMapper dependency exists.

---

### REQ-19 — No mediator in the application layer

Handler classes are injected directly into endpoint delegates via DI. No `IMediator`, `ISender`, or equivalent mediator interface is present in any handler call path. The `CrearFacturaHandler` precedent (registered as `AddScoped<CrearFacturaHandler>()`) is replicated for all new handlers.

---

### REQ-20 — Integration test suite covers all three slices

`tests/GastroGestion.Api.Tests/` contains test classes covering:
- Slice 1: health check, exception mapping, validation filter.
- Slice 2: at minimum one happy-path and one error-path test per catalogue endpoint group.
- Slice 3: at minimum one happy-path and one error-path test for Pedido lifecycle, Factura creation, and stock balance.

All tests are tagged `[Trait("Category","Integration")]` and pass with `dotnet test` against LocalDB.

---

## PHASE-5 Requirements Catalog — AUTH-01 to AUTH-09

### AUTH-01 — `Usuario` Aggregate (Domain Layer)

The `Usuario` class is a first-class aggregate root in `GastroGestion.Domain`. It carries private setters on all properties and a parameterless `private Usuario()` constructor for EF Core.

| Property | Type | Constraint |
|---|---|---|
| `Email` | `string` | Unique per business rule; stored as-is |
| `NombreCompleto` | `string` | Non-empty |
| `Rol` | `RolUsuario` | Existing enum (`Administrador`, `Cajero`, `Mozo`, `Cocinero`) |
| `PasswordHash` | `string` | Opaque Base64 string; Domain treats as a black box |
| `Activo` | `bool` | Active/inactive flag |

`GastroGestion.Domain.csproj` retains zero `<PackageReference>` elements. The aggregate exposes a static factory `Crear(email, nombreCompleto, rol, passwordHash)` that validates email (non-empty, contains `@` with non-empty parts), `nombreCompleto` (non-empty), and `passwordHash` (non-empty). Returns a valid instance with `Activo = true` and a new `Guid` id on success; throws `DomainException` on validation failure.

---

### AUTH-02 — Password Hashing Port and Infrastructure Implementation

An `IPasswordHasher` port exists in `GastroGestion.Application` (e.g., `Application/Abstractions/Security/`). It exposes:
- `string Hash(Usuario usuario, string plainPassword)` — hash a plain-text password.
- `bool Verify(Usuario usuario, string hashedPassword, string providedPassword)` — verify a plain-text password against a stored hash.

No type from `Microsoft.AspNetCore.Identity` or any Infrastructure namespace appears in the port definition. The concrete implementation in `GastroGestion.Infrastructure` is backed by `Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>` (PBKDF2-SHA256). It is registered in `Infrastructure/DependencyInjection.cs` so the application-layer port resolves at runtime. Zero references to `Microsoft.AspNetCore.Identity` appear in `GastroGestion.Domain.csproj` or `GastroGestion.Application.csproj`.

---

### AUTH-03 — Login Use Case (Application Layer)

`IUsuarioRepository` exists in `GastroGestion.Application/Abstractions/Persistence/` and exposes:
- `Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default)` — returns `null` when no user with that email exists.
- `Task<bool> AnyAsync(CancellationToken ct = default)` — returns true if any usuario exists.
- `Task AddAsync(Usuario usuario, CancellationToken ct = default)` — adds a usuario.

`ITokenService` exists in `GastroGestion.Application` (e.g., `Application/Abstractions/Security/`) and exposes a method that accepts a `Usuario` and returns a signed token string plus its expiry instant (e.g., as a `(string Token, DateTime ExpiresAt)` tuple or equivalent record). No type from `System.IdentityModel.Tokens.Jwt` or any Infrastructure namespace appears in the port definition.

A `LoginHandler` class in `GastroGestion.Application` accepts a `LoginCommand` (carrying `Email` and `Password`), injects `IUsuarioRepository`, `ITokenService`, and `IPasswordHasher` via the constructor, and:
- Calls `IUsuarioRepository.GetByEmailAsync(email)`.
- On user not found: returns a generic auth-failure result without revealing the email was not found.
- On user found but `Activo == false`: returns the same generic auth-failure result without revealing the account is inactive.
- On user found, active, but password verification fails: returns the same generic auth-failure result without revealing which part was wrong.
- On user found, active, and password verified: calls `ITokenService` and returns the token + expiry + minimal user info.

The handler does NOT throw an exception for failed credentials; it returns a discriminated result that the Api layer translates to HTTP 401.

---

### AUTH-04 — JWT Token Issuance (Infrastructure Layer)

The `ITokenService` implementation exists in `GastroGestion.Infrastructure` and uses `JwtSecurityTokenHandler` (NOT `JsonWebTokenHandler`) to sign tokens. Every issued token carries exactly these claims:

| Claim | Value |
|---|---|
| `sub` | `Usuario.Id.ToString()` |
| `email` | `Usuario.Email` |
| `ClaimTypes.Role` | `Usuario.Rol` enum name as a string (e.g., `"Administrador"`) |
| `iss` | Value of `Jwt:Issuer` from configuration |
| `aud` | Value of `Jwt:Audience` from configuration |
| `exp` | Issued time + **8 hours** (not 1 hour, not 24 hours) |

The token is signed with the `SymmetricSecurityKey` derived from `Jwt:SigningKey` in configuration — the same key the existing `TokenValidationParameters` in `Program.cs` already uses. No new configuration keys are introduced for signing. The implementation is registered in `Infrastructure/DependencyInjection.cs`.

---

### AUTH-05 — Login Endpoint (Api Layer)

`POST /auth/login` exists as a Minimal API endpoint, marked `[AllowAnonymous]` so it is reachable without a token. On valid credentials, the endpoint returns HTTP 200 with a body matching `LoginResponse` (token, expiry, user id, role) in `application/json` format. On invalid credentials (any of the three failure cases), the endpoint returns HTTP 401 with no body field that reveals which part of the credentials was wrong. A standard RFC 7807 `ProblemDetails` body is acceptable as long as the `detail` field is generic (e.g., `"Invalid credentials."`).

The endpoint applies the `WithValidation<LoginRequest>()` filter. A request with a missing `Email` or missing `Password` field returns 400 before reaching the handler. `LoginHandler` is injected directly into the endpoint delegate via DI, consistent with REQ-19. No mediator is used.

---

### AUTH-06 — Endpoint Protection (Api Layer)

Every endpoint from Phase-4 (all routes under `/clientes`, `/ingredientes`, `/platos`, `/menus`, `/mesas`, `/pedidos`, `/facturas`, and `/stock`) requires a valid JWT bearer token after this change. Each `[AllowAnonymous]` attribute on those endpoints is replaced with `[Authorize]` (or the group-level equivalent without an explicit `Roles=` parameter, except where AUTH-07 specifies otherwise). `GET /health` remains reachable without a bearer token (response HTTP 200). `POST /auth/login` remains reachable without a bearer token. A request that supplies a valid bearer token receives the same response (status code and body shape) as it did when endpoints were `[AllowAnonymous]`. The `[AllowAnonymous]` → `[Authorize]` swap does NOT alter the functional behavior of any endpoint for authenticated requests.

---

### AUTH-07 — Role Extraction from JWT Claim — Pedido State Transition (Api Layer)

The `POST /pedidos/{id}/transicion` endpoint reads `RolUsuario` from `HttpContext.User` via `ClaimTypes.Role`, NOT from the request body. `TransicionarEstadoRequest` in `GastroGestion.Contracts` does NOT contain a `Rol` field after this change; it carries only `EstadoNuevo`.

The endpoint delegate:
1. Extracts the `ClaimTypes.Role` claim value from `HttpContext.User.FindFirst(ClaimTypes.Role)`.
2. Attempts to parse it to `RolUsuario` via `Enum.TryParse<RolUsuario>`.
3. If the claim is absent or unparseable: returns HTTP **403** (`Forbidden`). (The request passed `[Authorize]` bearer validation, so the user IS authenticated — identity is established via the `sub` claim. A missing or corrupt role claim means the authenticated principal lacks a valid authorization attribute for this endpoint — per RFC 7231 §6.5.3, 403 Forbidden is the correct response when the server understood the request but refuses to authorize it.)
4. On successful parse: builds the existing `TransicionarEstadoPedidoCommand` with the parsed role and passes it to `TransicionarEstadoPedidoHandler`.

`TransicionarEstadoPedidoHandler` and the `Pedido.TransicionarEstado(EstadoPedido, RolUsuario)` domain method do NOT change. Domain-level role validation (invalid transition for role) continues to return 422 via `DomainException`. A request body `{ "estadoNuevo": 1, "rol": 0 }` has the `rol` field silently ignored because the DTO no longer declares that field; role comes from the JWT claim only.

---

### AUTH-08 — Initial Admin Seeding (Infrastructure + Api Layers)

When the application starts in Development mode and the `Usuarios` table contains zero rows, the system inserts exactly one `Usuario` with `Rol = RolUsuario.Administrador` and `Activo = true`. The seeded admin credentials (email and plain-text password) are documented in `appsettings.Development.json` under `Seed:AdminEmail` / `Seed:AdminPassword`. The plain-text password is hashed via the `IPasswordHasher` Infrastructure implementation before storage; it is NOT stored as plain text in the database.

The seeder checks `Usuarios.AnyAsync()` before inserting. If any `Usuario` row already exists, the seeder returns without inserting, regardless of how many times it runs. Duplicate seeded admins are impossible. The admin seeding integrates with the existing `DevDataSeeder` flow (per REQ-05) or is called from the same `IsDevelopment()` guard in `Program.cs`. The existing seeder's idempotency check (`Clientes.AnyAsync()`) is unaffected.

---

### AUTH-09 — Cross-Cutting: Round-Trip Validation (Api Layer / Infrastructure Layer)

The existing `TokenValidationParameters` block in `Program.cs` (issuer validation, audience validation, lifetime validation, signing key validation) is NOT modified by this change. Tokens issued by the `ITokenService` implementation are accepted by the existing validation pipeline without any configuration change. After this change, zero references to `Microsoft.AspNetCore.Identity` (or any sub-namespace) appear in `GastroGestion.Domain.csproj` or `GastroGestion.Application.csproj` project files.

`ApiFactory` exposes a `GenerateTestToken(RolUsuario role)` method that uses `JwtSecurityTokenHandler` with the test signing key (`"TestSigningKeyForApiTestsMinimumLength32Chars"`), the test issuer (`"GastroGestion"`), and the test audience (`"GastroGestion"`) already injected at factory startup. Tokens generated by this method pass the existing `TokenValidationParameters` used by the test host. All integration tests that call an endpoint now protected by `[Authorize]` attach a bearer token obtained from `ApiFactory.GenerateTestToken(...)`. Tests for the Pedido state-transition path do NOT send `Rol` in the request body.

After this change, `dotnet test` passes with zero failures. The 222 tests that were green at the end of Phase 4 remain green (plus new tests added for auth total to 245).

---

## PHASE-6 Requirements Catalog — OT-01 to OT-06 (OrdenTrabajo Workflow)

### OT-01 — Generate Work Orders `POST /pedidos/{pedidoId}/ordenes-trabajo`

The system MUST expose a command that generates one `OrdenTrabajo` per `LineaPedido` for a given `Pedido`, all-or-nothing, as an explicit mozo action (not triggered automatically).

Access is restricted to users with role `Mozo` or `Administrador` (enforced at the Application layer via `ClaimTypes.Role`).

The endpoint returns `204 NoContent` on success.

**Error contracts:**
- `404` — Pedido not found.
- `409` — OrdenesTrabajo already exist for this Pedido.
- `422` — Any `LineaPedido` has no confirmed price, OR any referenced `PlatoId` has an empty `LineasReceta` snapshot (early failure with `ProblemDetails` body naming the offending `PlatoId`).
- `401` / `403` — Unauthenticated or unauthorized role.

#### Scenario OT-01-A: Happy path — all lines priced, all recipes populated

- GIVEN a `Pedido` with priced lines whose Platos have non-empty recipes
- WHEN a mozo calls `POST /pedidos/{pedidoId}/ordenes-trabajo`
- THEN one `OrdenTrabajo` per line is created with `Estado = Creada`
- AND returns HTTP 204 NoContent

#### Scenario OT-01-B: Failure — line without confirmed price

- GIVEN a `Pedido` where at least one `LineaPedido` has no confirmed price
- WHEN a mozo calls the endpoint
- THEN the system returns HTTP 422 with a `ProblemDetails` body

#### Scenario OT-01-C: Failure — Plato with empty recipe

- GIVEN a `Pedido` where a referenced `PlatoId` has an empty `LineasReceta`
- WHEN a mozo calls the endpoint
- THEN the system returns HTTP 422 describing the offending `PlatoId`

#### Scenario OT-01-D: Failure — Pedido not found

- GIVEN a `pedidoId` that does not exist
- THEN the system returns HTTP 404

#### Scenario OT-01-E: Failure — OTs already generated

- GIVEN a `Pedido` that already has `OrdenesTrabajo`
- THEN the system returns HTTP 409

---

### OT-02 — Assign Cook `PATCH /pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero`

The system MUST allow users with role `COCINERO` or `ADMINISTRADOR` to assign a cook (`legajoId`) to an `OrdenTrabajo`, transitioning it from `Creada` to `Preparandose`.

Role is read from `ClaimTypes.Role` at the Application layer. Users with any other role receive HTTP 403.

Returns `200 Ok` with an `OrdenTrabajoResponse` body.

**Error contracts:** `404` Pedido or OT not found; `422` OT not in `Creada`; `403` unauthorized role; `401` unauthenticated.

#### Scenario OT-02-A: Happy path

- GIVEN an `OrdenTrabajo` in state `Creada`
- WHEN a `COCINERO` calls the assign endpoint with a valid `CocineroLegajoId`
- THEN `Estado` transitions to `Preparandose`, `CocineroAsignado` is set
- AND returns HTTP 200

#### Scenario OT-02-B: Failure — wrong role

- GIVEN an authenticated user with role `MOZO`
- THEN the system returns HTTP 403

#### Scenario OT-02-C: Failure — OT not in Creada

- GIVEN an `OrdenTrabajo` in state `Preparandose` or `Lista`
- THEN the system returns HTTP 422

---

### OT-03 — Mark Order Ready `PATCH /pedidos/{pedidoId}/ordenes-trabajo/{otId}/lista`

The system MUST allow users with role `COCINERO` or `ADMINISTRADOR` to mark an `OrdenTrabajo` as ready, transitioning it from `Preparandose` to `Lista`.

After the transition, if ALL `OrdenesTrabajo` for the owning `Pedido` are `Lista` AND the `Pedido` is NOT a Salon-type order, the system MUST automatically advance the `Pedido` to `ListoParaEntregar` (domain invariant).

Returns `200 Ok` with an `OrdenTrabajoResponse` body.

**Error contracts:** `404` Pedido or OT not found; `422` OT not in `Preparandose`; `403` unauthorized role; `401` unauthenticated.

#### Scenario OT-03-A: Happy path — OT marked ready, Pedido not yet complete

- GIVEN an `OrdenTrabajo` in state `Preparandose` and at least one sibling OT still in progress
- WHEN a `COCINERO` calls the ready endpoint
- THEN `Estado` transitions to `Lista` and the `Pedido` state is unchanged
- AND returns HTTP 200

#### Scenario OT-03-B: Happy path — last OT, Pedido auto-advances

- GIVEN the last non-`Lista` OT for a non-Salon `Pedido`
- WHEN a `COCINERO` calls the ready endpoint
- THEN the OT transitions to `Lista` AND the `Pedido` transitions to `ListoParaEntregar`
- AND returns HTTP 200

---

### OT-04 — Kitchen Board Read `GET /ordenes-trabajo?estado={EstadoOT?}`

The system MUST expose a flat projection of all `OrdenesTrabajo` across all `Pedidos`.

The `estado` query parameter is optional; when omitted, all non-`Cancelada` orders are returned.

Access is restricted to users with role `COCINERO` or `ADMINISTRADOR`.

The query projects directly from the `PedidoOrdenesTrabajo` table — it does NOT load full `Pedido` aggregates.

The flat projection includes: `OtId`, `PedidoId`, `PedidoTipo`, `PlatoId`, `LineaPedidoId`, `Estado` (serialized as string per convention W-03), `CocineroAsignadoLegajoId`.

Returns `200 Ok` with `IReadOnlyList<OrdenTrabajoBoardResponse>`.

**Error contracts:** `401` unauthenticated; `403` unauthorized role; `400` invalid `?estado=` value.

---

### OT-05 — Realtime OT State Push (SignalR) `[PR2]`

The system pushes real-time notifications to a `"kitchen"` SignalR group whenever an `OrdenTrabajo` changes state (assigned or ready). The hub is mapped at `/hubs/kitchen`.

**Payload** (`OrdenTrabajoBoardResponse`): `OtId`, `PedidoId`, `PedidoTipo`, `PlatoId`, `LineaPedidoId`, `Estado` (string), `CocineroAsignadoLegajoId`.

**Client method pushed:** `"OtChanged"`.

The REST board endpoint (OT-04) remains unchanged and serves as the reconnection-recovery path for kitchen clients.

**Layering:**
- `IKitchenNotifier` port in the Application layer (`Application/Abstractions/Realtime/`).
- `KitchenHub : Hub` in the API layer (`Api/Hubs/`).
- `SignalRKitchenNotifier : IKitchenNotifier` adapter in the API layer (`Api/Realtime/`).
- Mutation handlers (`AsignarCocineroHandler`, `MarcarOrdenTrabajoListaHandler`) call the port **after** `SaveChangesAsync`.

#### Scenario OT-05-A: State change pushes delta to kitchen group

- GIVEN a kitchen client connected to the hub
- WHEN an OT transitions state via any OT mutation endpoint
- THEN the hub broadcasts the state-change delta to the `"kitchen"` group

#### Scenario OT-05-B: Client reconnect recovers via REST

- GIVEN a kitchen client that reconnected after a hub disconnect
- WHEN it calls `GET /ordenes-trabajo`
- THEN it receives the current full board state

---

### OT-06 — Authentication and Role Enforcement (Cross-Cutting)

All OT endpoints require a valid JWT bearer token. Unauthenticated requests return HTTP 401.

Enum values in all OT responses are serialized as strings (project convention W-03 — global `JsonStringEnumConverter`). This applies to `EstadoOT` and `TipoPedido` in all OT response DTOs.

`GET /pedidos/{id}` and `PedidoResponse` are byte-for-byte unchanged after this change.

---

## Catalog CRUD + Cocineros Requirements — CCC-A01, CCC-B01..B03, CCC-C01..C03

> **Change:** `catalog-crud-and-cocineros` — 3 chained PRs (#19 commit 8251125, #20 commit 60bd611, #21 commit b3af61e) merged to main 2026-06-17.

### Status code clarification (verified behavior, authoritative)

- **401 Unauthorized**: returned by the ASP.NET Core auth middleware for requests that carry no valid JWT bearer token (before the endpoint handler runs). This is the actual behavior for all endpoints using `.RequireAuthorization()`.
- **403 Forbidden**: returned by the manual `ClaimTypes.Role` gate inside the endpoint delegate for requests that ARE authenticated but whose role claim is missing, unparseable, or insufficient. The spec text for CCC-A01 originally said "unauthenticated → 403"; the implementation correctly returns 401 from the framework. 403 is reserved exclusively for role-gate failures on authenticated callers.
- **400 Bad Request**: returned by the `WithValidation<T>()` FluentValidation endpoint filter for empty-field or format violations (fires before the handler).
- **422 Unprocessable Entity**: returned by `GastroGestionExceptionHandler` for `DomainException` (domain-rule violations, e.g., `ResponsableInscripto` without CUIT) and `ValidationException`.

---

### CCC-A01 — List Active Cocineros

The system MUST expose `GET /usuarios/cocineros` returning active users whose role is `Cocinero`. The response MUST include each user's `id` and `nombreCompleto`. Access is restricted to callers whose role is `Cocinero` or `Administrador`. Authenticated callers with any other role MUST receive `403`. Unauthenticated callers receive `401` from the framework auth middleware.

#### Scenario: Admin or Cocinero retrieves cocinero list

- GIVEN a valid `Administrador` or `Cocinero` token
- WHEN `GET /usuarios/cocineros` is called
- THEN response is `200 OK` with an array of `{ id, nombreCompleto }` for each active cocinero

#### Scenario: Mozo, Cajero, missing role, or unparseable role is rejected

- GIVEN an authenticated token with role `Mozo`, `Cajero`, no role claim, or an unparseable role claim
- WHEN `GET /usuarios/cocineros` is called
- THEN response is `403 Forbidden` with ProblemDetails body

#### Scenario: Unauthenticated caller

- GIVEN no Authorization header
- WHEN `GET /usuarios/cocineros` is called
- THEN response is `401 Unauthorized` (ASP.NET auth middleware, before role gate)

#### Scenario: Inactive cocineros excluded

- GIVEN a `Usuario` with `Rol == Cocinero` and `Activo == false`
- WHEN `GET /usuarios/cocineros` is called with an `Administrador` token
- THEN that user does NOT appear in the response array

---

### CCC-B01 — Edit Cliente

The system MUST allow an `Administrador` to update a cliente's `Nombre`, `Email`, `Cuit`, and `CondicionIVA` via `PUT /clientes/{id}`. `NumeroCliente` MUST NOT change regardless of request content. A `Cuit` that conflicts with another cliente MUST produce `409`. Empty `Nombre` field returns `400` (FluentValidation filter). Domain-rule violations (e.g., `ResponsableInscripto` without `Cuit`) return `422`. Non-admin authenticated callers MUST receive `403`.

#### Scenario: Admin edits valid cliente

- GIVEN a cliente with `id=5` exists and the supplied `Cuit` is unique
- WHEN `PUT /clientes/5` is called with an `Administrador` token and valid body
- THEN response is `200 OK` with updated resource; `NumeroCliente` is unchanged

#### Scenario: Cliente not found → 404

#### Scenario: Cuit conflict with another cliente → 409

#### Scenario: ResponsableInscripto without Cuit → 422 (DomainException)

#### Scenario: Empty Nombre field → 400 (FluentValidation filter)

#### Scenario: Non-admin caller → 403

---

### CCC-B02 — Soft-Delete Cliente

The system MUST soft-delete a cliente via `DELETE /clientes/{id}`, setting `Activo = false` and returning `204`. The operation MUST be idempotent. Non-admin callers MUST receive `403`. A non-existent id MUST return `404`. After deletion the cliente MUST be hidden from default list results.

#### Scenario: Admin soft-deletes active cliente → 204, Activo becomes false

#### Scenario: Idempotent — already inactive → 204 (no error)

#### Scenario: Not found → 404

#### Scenario: Non-admin → 403

---

### CCC-B03 — Search/List Clientes

The system MUST support `GET /clientes?nombre=&incluirInactivos=`. By default only active clientes are returned. When `?incluirInactivos=true` is supplied, inactive clientes are also returned. The `nombre` parameter applies a case-insensitive partial match. The endpoint requires authentication.

**Routing note:** `GET /clientes` is backed by `BuscarClientesHandler` (not `GetAllClientesHandler`). `GetAllAsync` is left intact for backward compatibility; the list endpoint calls `SearchAsync` with default parameters.

#### Scenario: Default list excludes inactive

#### Scenario: incluirInactivos=true shows all

#### Scenario: nombre filter applies case-insensitive partial match

#### Scenario: Unauthenticated → 401

---

### CCC-C01 — Edit Ingrediente (Nombre Only)

The system MUST allow an `Administrador` to update an ingrediente's `Nombre` via `PUT /ingredientes/{id}`. `UnidadBase` MUST NOT be changed — it is structurally absent from `EditarIngredienteRequest` (immutability by contract). A `Nombre` conflict with another ingrediente MUST produce `409`. Empty `Nombre` returns `400` (FluentValidation). Non-admin callers MUST receive `403`.

#### Scenario: Admin edits Nombre → 200; UnidadBase unchanged (confirmed via GET after PUT)

#### Scenario: UnidadBase cannot be supplied — structurally absent from request DTO

#### Scenario: Ingrediente not found → 404

#### Scenario: Nombre conflict → 409

#### Scenario: Empty Nombre → 400 (FluentValidation filter)

#### Scenario: Non-admin → 403

---

### CCC-C02 — Soft-Delete Ingrediente

The system MUST soft-delete an ingrediente via `DELETE /ingredientes/{id}`, returning `204`. The operation MUST be idempotent. Non-admin callers MUST receive `403`. A non-existent id MUST return `404`.

#### Scenario: Admin soft-deletes active ingrediente → 204

#### Scenario: Idempotent — already inactive → 204

#### Scenario: Not found → 404

#### Scenario: Non-admin → 403

---

### CCC-C03 — Search/List Ingredientes

The system MUST support `GET /ingredientes?nombre=&incluirInactivos=` with the same behavior as CCC-B03 applied to ingredientes.

**Routing note:** `GET /ingredientes` is backed by `BuscarIngredientesHandler`. `GetAllAsync` is left intact.

#### Scenario: Default list excludes inactive

#### Scenario: incluirInactivos=true shows all

#### Scenario: nombre partial filter applied

#### Scenario: Unauthenticated → 401

---

## Known Open Items / Phase-7 Follow-ups (CARRY FORWARD — DO NOT DROP)

### 1. Register / User CRUD endpoints (deferred to Phase 7)

**Status:** Phase 5 introduced login and JWT issuance. Phase 6 (kitchen workflow) did not address user management. Admin user is seeded in Development only.

**Open work:** `POST /auth/register` endpoint for end-user self-registration and admin user-management surface (`POST /usuarios`, `DELETE /usuarios/{id}`, etc.) remain out of scope. These will be added in Phase 7 (Blazor/backend completion) when the `GastroGestion_Seguridad` dedicated catalog is introduced.

### 3. ConfirmarPrecioLinea returns 204 (decision)

**Issue:** Spec scenario 15-C originally said "200 with price body." Implementation returns 204 No Content (command pattern).

**Canonical decision: 204 is correct.** Price is readable via separate GET.

---

### 4. Validator-vs-domain status codes (Phase-4 deferred)

**Issue:** Some invalid inputs (negative price, past menu date, zero capacity, Salon without MesaId) return 400 via the FluentValidation layer rather than 422 from the domain.

**Intentional per design §6c (friendlier boundary checks).** Canonical note: "400 (validator) or 422 (domain if validator bypassed)."

---

### 5. IngredienteValidator whitespace gap (W-2 from Phase-4 PR2 verify)

**Issue:** `NotEmpty()` allows whitespace-only names through to the domain (→ 422). 

**Minor tightening:** `.Must(s => !string.IsNullOrWhiteSpace(s))` gives deterministic 400. Non-blocking; address in a future maintenance slice.

---

### 6. EF MultipleCollectionInclude on PedidoRepository.GetByIdAsync (S-01 from Phase-4 PR3)

**Issue:** EF Core logs a warning at runtime: "Compiling a query which loads related collections for more than one collection navigation... no QuerySplittingBehavior has been configured."

**Recommendation:** Consider `.AsSplitQuery()` when traffic warrants, to avoid the Cartesian explosion warning on multi-collection includes.

---

### 6. CA1848 logger pattern (informational)

**Issue:** `GastroGestionExceptionHandler` uses non-cached logger message.

**No functional impact.** Address in a future maintenance pass.

---

## Endpoint Signatures (TypedResults summary)

| Route | Verb | Request DTO | Response Type | Status codes | Auth |
|-------|------|-------------|---------------|--------------|------|
| `/auth/login` | POST | `LoginRequest` | `Ok<LoginResponse>` | 200, 400, 401 | Anonymous |
| `/clientes` | POST | `CrearClienteRequest` | `Created<Guid>` | 201, 400, 409, 422 | Required |
| `/clientes/{id:guid}` | GET | — | `Ok<ClienteResponse>` | 200, 401, 404 | Required |
| `/clientes` | GET | `?nombre=&incluirInactivos=` | `Ok<List<ClienteResponse>>` | 200, 401 | Required |
| `/clientes/{id:guid}` | PUT | `EditarClienteRequest` | `Ok<ClienteResponse>` | 200, 400, 401, 403, 404, 409, 422 | Required (Administrador) |
| `/clientes/{id:guid}` | DELETE | — | `NoContent` | 204, 401, 403, 404 | Required (Administrador) |
| `/ingredientes` | POST | `CrearIngredienteRequest` | `Created<Guid>` | 201, 400, 401, 422 | Required |
| `/ingredientes/{id:guid}` | GET | — | `Ok<IngredienteResponse>` | 200, 401, 404 | Required |
| `/ingredientes` | GET | `?nombre=&incluirInactivos=` | `Ok<List<IngredienteResponse>>` | 200, 401 | Required |
| `/ingredientes/{id:guid}` | PUT | `EditarIngredienteRequest` | `Ok<IngredienteResponse>` | 200, 400, 401, 403, 404, 409, 422 | Required (Administrador) |
| `/ingredientes/{id:guid}` | DELETE | — | `NoContent` | 204, 401, 403, 404 | Required (Administrador) |
| `/usuarios/cocineros` | GET | — | `Ok<List<CocineroResponse>>` | 200, 401, 403 | Required (Cocinero, Administrador) |
| `/platos` | POST | `CrearPlatoRequest` | `Created<Guid>` | 201, 400, 401, 422 | Required |
| `/platos/{id:guid}` | GET | — | `Ok<PlatoResponse>` | 200, 401, 404 | Required |
| `/platos` | GET | — | `Ok<List<PlatoResponse>>` | 200, 401 | Required |
| `/menus` | POST | `CrearMenuRequest` | `Created<Guid>` | 201, 400, 401, 422 | Required |
| `/menus/{id:guid}` | GET | — | `Ok<MenuResponse>` | 200, 401, 404 | Required |
| `/menus` | GET | — | `Ok<List<MenuResponse>>` | 200, 401 | Required |
| `/mesas` | POST | `CrearMesaRequest` | `Created<Guid>` | 201, 400, 401, 422 | Required |
| `/mesas/{id:guid}` | GET | — | `Ok<MesaResponse>` | 200, 401, 404 | Required |
| `/mesas` | GET | — | `Ok<List<MesaResponse>>` | 200, 401 | Required |
| `/pedidos` | POST | `CrearPedidoRequest` | `Created<Guid>` | 201, 400, 401, 404, 422 | Required |
| `/pedidos/{id:guid}/lineas` | POST | `AgregarLineaRequest` | `Created<Guid>` | 201, 400, 401, 404, 422 | Required |
| `/pedidos/{id:guid}/lineas/{lineaId:guid}/confirmar-precio` | POST | — | `NoContent` | 204, 401, 404, 422 | Required |
| `/pedidos/{id:guid}/transicion` | POST | `TransicionarEstadoRequest` | `Ok<PedidoResponse>` | 200, 401, 403, 404, 422 | Required |
| `/pedidos/{id:guid}` | GET | — | `Ok<PedidoResponse>` | 200, 401, 404 | Required |
| `/facturas` | POST | `CrearFacturaRequest` | `Created<Guid>` | 201, 401, 409 | Required |
| `/facturas/{id:guid}/pagos` | POST | `RegistrarPagoRequest` | `Ok<FacturaResponse>` | 200, 401, 404, 422 | Required |
| `/facturas/{id:guid}` | GET | — | `Ok<FacturaResponse>` | 200, 401, 404 | Required |
| `/stock/movimientos` | POST | `RegistrarMovimientoStockRequest` | `Created<Guid>` | 201, 400, 401, 422 | Required |
| `/stock/balance/{ingredienteId:guid}` | GET | — | `Ok<BalanceStockResponse>` | 200, 401 | Required |
| `/health` | GET | — | 200 OK | 200 | Anonymous |
| `/pedidos/{pedidoId:guid}/ordenes-trabajo` | POST | — | `NoContent` | 204, 401, 403, 404, 409, 422 | Required (Mozo, Administrador) |
| `/pedidos/{pedidoId:guid}/ordenes-trabajo/{otId:guid}/asignar-cocinero` | PATCH | `AsignarCocineroRequest` | `Ok<OrdenTrabajoResponse>` | 200, 401, 403, 404, 422 | Required (Cocinero, Administrador) |
| `/pedidos/{pedidoId:guid}/ordenes-trabajo/{otId:guid}/lista` | PATCH | — | `Ok<OrdenTrabajoResponse>` | 200, 401, 403, 404, 422 | Required (Cocinero, Administrador) |
| `/ordenes-trabajo` | GET | `?estado={EstadoOT?}` | `Ok<IReadOnlyList<OrdenTrabajoBoardResponse>>` | 200, 400, 401, 403 | Required (Cocinero, Administrador) |
| `/hubs/kitchen` | SignalR | — | Server pushes `"OtChanged"` with `OrdenTrabajoBoardResponse` | — | Required |

---

## Development vs Production

- **Development:** DevDataSeeder runs (creating Clientes, Ingredientes, etc.); admin Usuario is seeded; Swagger UI enabled; `/health` and `/auth/login` reachable without token; other endpoints require bearer token.
- **Production:** Seeder does not run; Swagger UI disabled; all protected endpoints require a valid JWT bearer token; `/health` and `/auth/login` remain anonymous; no token refresh or revocation (8-hour tokens only).

---

## Delivery Status

**Phase 4 complete:** WA-01 through WA-30 all [x] marked complete.
- **PR #9** (Slice 1 — Foundation): merged to main @ commit fd44ab6.
- **PR #10** (Slice 2 — Catalogue): merged to main.
- **PR #11** (Slice 3 — Transactional+Fiscal+Stock): merged to main.
- **Test suite:** 222 integration tests passing (0 failures).
- **Verification:** Phase 4 PASS WITH WARNINGS — 0 CRITICAL, 3 documented deviations (204 response, validator vs domain status codes, enum integers), 2 suggestions (JsonStringEnumConverter, MultipleCollectionInclude).

**Phase 5 complete:** AJ-01 through AJ-22 all [x] marked complete.
- **PR #12** (PR1 — Additive Auth Foundation): merged to main @ commit 9e4835b.
- **PR #13** (PR2 — Lockdown + Test Migration): merged to main @ commit f7724c8.
- **Test suite:** 245 integration tests passing (0 failures, +23 new tests from Phase 5).
- **Verification:** Phase 5 PASS WITH WARNINGS — AUTH-07.3 adjudication resolved (401→403 corrected in this spec), role-claim test gaps deferred to Phase 6, login endpoint test gaps deferred to Phase 6.

**Phase 6 complete:** OW-01 through OW-18 all [x] marked complete.
- **PR #14** (PR1 — Core workflow + REST board): merged to main @ commit 3e2d533.
- **PR #15** (PR2 — Realtime SignalR layer): merged to main.
- **Test suite:** 270 tests passing (0 failures, +25 new tests from Phase 6).
- **Verification:** Phase 6 PR1 PASS (re-verify after remediation). Phase 6 PR2 PASS.

**Catalog CRUD + Cocineros complete:** CCC-T01 through CCC-T52 all [x] marked complete (52 tasks).
- **PR #19** (PR A — Cocineros list): merged to main @ commit 8251125. Verdict: PASS WITH WARNINGS (W-01: anon→401 not 403, correct behavior; spec corrected here).
- **PR #20** (PR B — Cliente CRUD): merged to main @ commit 60bd611. Verdict: PASS WITH WARNINGS (2 warnings: NumeroCliente not in response body; no anon test for PUT/DELETE).
- **PR #21** (PR C — Ingrediente CRUD): merged to main @ commit b3af61e. Verdict: PASS — 0 CRITICAL, 1 WARNING (W-01: empty Nombre → 400 not 422; correct behavior per FluentValidation layer; spec corrected here).
- **Test suite:** 413 tests passing (0 failures, +143 new tests across all three PRs).
- **Spec corrections applied:** (1) unauthenticated callers → 401 (not 403) on all `.RequireAuthorization()` endpoints; 403 is exclusively for authenticated callers whose role is missing/wrong. (2) Empty-field validation → 400 from FluentValidation endpoint filter; 422 is reserved for DomainException/ValidationException domain-rule violations.

---

## Next Phase

Phase 7 (Blazor frontend) will build the kitchen dashboard UI, the order management screens, and the admin panel, consuming the REST API and the SignalR kitchen hub established in Phase 6. The catalog-crud-and-cocineros change UNBLOCKS the frontend wave: Slice C2 (asignar-cocinero picker consuming `GET /usuarios/cocineros`) and Client/Ingrediente CRUD UI screens.
