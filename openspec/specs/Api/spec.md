# Web API Specification — GastroGestion

**Last updated:** 2026-06-15  
**Phase:** 4–5 of 7 in the .NET 8 strangler roadmap  
**Scope:** GastroGestion Web API layer — .NET 8 Minimal API endpoints, request/response contracts, middleware, authentication shape, JWT token issuance, authorization enforcement.

---

## Overview

The Web API layer exposes the persisted Phase-3 domain and completed application use cases over HTTP via ASP.NET Core Minimal APIs. The contract is REST + RFC 7807 problem details; all endpoints operate on DTOs (never domain aggregates). Authentication is now fully active (Phase 5): JWT tokens are issued by `POST /auth/login`, most endpoints require `[Authorize]`, and role information is extracted from the JWT claim for authorization decisions. Error handling is centralized via `IExceptionHandler` + `AddProblemDetails()`.

**Status:** Phase 4 complete — 3 slices (Foundation, Catalogue, Transactional+Fiscal+Stock) delivered via PRs #9, #10, #11 to main. Phase 5 (auth-jwt) complete — 2 stacked PRs (PR #12, PR #13) merged to main. All 30 Phase-4 tasks (WA-01..WA-30) + 22 Phase-5 tasks (AJ-01..AJ-22) complete. Test suite: 245 tests green (160 domain + 6 app + 33 infra + 46 api). Verification: Phase 5 PASS WITH WARNINGS (401→403 adjudication resolved to 403, spec corrected, test gaps noted for future). Phase-6 follow-ups captured below.

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

## Known Open Items / Phase-6 Follow-ups (CARRY FORWARD — DO NOT DROP)

### 1. Register / User CRUD endpoints (deferred to Phase 6)

**Status:** Phase 5 introduced login and JWT issuance. Admin user is seeded in Development only.

**Open work:** `POST /auth/register` endpoint for end-user self-registration and admin user-management surface (`POST /usuarios`, `DELETE /usuarios/{id}`, etc.) are out of scope for Phase 5. These will be added in Phase 6 when the `GastroGestion_Seguridad` dedicated catalog is introduced.

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
| `/clientes` | GET | — | `Ok<List<ClienteResponse>>` | 200, 401 | Required |
| `/ingredientes` | POST | `CrearIngredienteRequest` | `Created<Guid>` | 201, 400, 401, 422 | Required |
| `/ingredientes/{id:guid}` | GET | — | `Ok<IngredienteResponse>` | 200, 401, 404 | Required |
| `/ingredientes` | GET | — | `Ok<List<IngredienteResponse>>` | 200, 401 | Required |
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

---

## Next Phase

Phase 6 will separate the `Usuarios` table into a dedicated `GastroGestion_Seguridad` catalog and `GastroGestion_SeguridadDbContext`, introduce refresh tokens and token revocation, add a user registration endpoint (`POST /auth/register`), and implement admin user-management endpoints. Test gaps from Phase 5 (AUTH-05.6 and AUTH-07-B/C HTTP integration tests) will also be addressed in Phase 6.
