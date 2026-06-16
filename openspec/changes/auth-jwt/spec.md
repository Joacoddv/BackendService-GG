# Delta Specification: auth-jwt (Phase 5 of 7)

**Last updated:** 2026-06-15
**Phase:** 5 of 7 in the .NET 8 strangler roadmap
**Status:** DRAFT — awaiting design phase
**Artifact store:** openspec
**Depends on:** Phase 4 spec (`openspec/specs/Api/spec.md`) — all REQ-01..REQ-20 remain in force unless explicitly superseded below.

---

## Scope Summary

This delta spec defines what MUST be true after the `auth-jwt` change is applied. It covers nine numbered requirement areas: the `Usuario` domain aggregate, password hashing infrastructure, the login use case, JWT token issuance, the login endpoint, endpoint protection, role extraction from JWT claim, initial admin seeding, and cross-cutting round-trip validation. Non-goals are stated explicitly at the end.

---

## Requirements Catalog — AUTH-01 to AUTH-09

---

### AUTH-01 — `Usuario` Aggregate (Domain Layer)

The `Usuario` class MUST exist in `GastroGestion.Domain` as a first-class aggregate root.

**AUTH-01.1 — Shape**

`Usuario` MUST extend `AggregateRoot`, carry private setters on all properties, expose a parameterless `private Usuario()` constructor for EF Core, and define the following properties:

| Property | Type | Constraint |
|---|---|---|
| `Email` | `string` | Unique per business rule; stored as-is |
| `NombreCompleto` | `string` | Non-empty |
| `Rol` | `RolUsuario` | Existing enum (`Administrador`, `Cajero`, `Mozo`, `Cocinero`) |
| `PasswordHash` | `string` | Opaque Base64 string; Domain treats as a black box |
| `Activo` | `bool` | Active/inactive flag |

`GastroGestion.Domain.csproj` MUST retain zero `<PackageReference>` elements after this change.

**AUTH-01.2 — Factory method**

`Usuario` MUST expose a static factory `Crear(string email, string nombreCompleto, RolUsuario rol, string passwordHash)` that:

- Throws `DomainException` when `email` is null or empty (after trimming).
- Throws `DomainException` when `email` does not match a basic valid-email shape (contains `@` with non-empty local and domain parts).
- Throws `DomainException` when `nombreCompleto` is null or empty (after trimming).
- Throws `DomainException` when `passwordHash` is null or empty.
- Returns a valid `Usuario` instance with `Activo = true` and a new `Guid` id on all-valid input.

The factory MUST NOT call any hashing library or reference any Infrastructure type.

**AUTH-01.3 — Scenarios**

**Scenario AUTH-01-A (happy path)**
Given valid `email`, `nombreCompleto`, `rol`, and non-empty `passwordHash`
When `Usuario.Crear(...)` is called
Then a `Usuario` instance is returned with `Activo = true`, the supplied field values, and a non-empty `Id`.

**Scenario AUTH-01-B (empty email)**
Given `email` is an empty string or whitespace
When `Usuario.Crear(...)` is called
Then a `DomainException` is thrown.

**Scenario AUTH-01-C (malformed email)**
Given `email` is the string `"notanemail"` (no `@` character)
When `Usuario.Crear(...)` is called
Then a `DomainException` is thrown.

**Scenario AUTH-01-D (empty name)**
Given `nombreCompleto` is null or whitespace
When `Usuario.Crear(...)` is called
Then a `DomainException` is thrown.

**Scenario AUTH-01-E (empty hash)**
Given `passwordHash` is null or empty
When `Usuario.Crear(...)` is called
Then a `DomainException` is thrown.

---

### AUTH-02 — Password Hashing Port and Infrastructure Implementation (Application + Infrastructure Layers)

**AUTH-02.1 — Port**

An `IPasswordHasher` port (or equivalent abstraction name) MUST exist in `GastroGestion.Application` (e.g., `Application/Abstractions/Security/`). It MUST expose at minimum:

- A method to hash a plain-text password and return the hashed string.
- A method to verify a plain-text password against a stored hash, returning a boolean or a verification result.

No type from `Microsoft.AspNetCore.Identity` or any Infrastructure namespace MAY appear in the port definition.

**AUTH-02.2 — Infrastructure implementation**

The concrete implementation MUST live in `GastroGestion.Infrastructure` and MUST be backed by `Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>` (PBKDF2-SHA256). The implementation MUST NOT be referenced directly from `GastroGestion.Application` or `GastroGestion.Domain`.

**AUTH-02.3 — DI registration**

The implementation MUST be registered in `Infrastructure/DependencyInjection.cs` so that the application-layer port resolves at runtime.

**AUTH-02.4 — Scenarios**

**Scenario AUTH-02-A (hash round-trip)**
Given a plain-text password string
When the Infrastructure hasher hashes it
And then verifies the same plain-text against the resulting hash
Then the verification MUST succeed.

**Scenario AUTH-02-B (wrong password)**
Given a plain-text password `"correct"` hashed to a stored value
When the verifier is called with `"wrong"` against that stored hash
Then the verification MUST fail.

**Scenario AUTH-02-C (domain independence)**
Given the `GastroGestion.Domain` project is compiled in isolation
When its project references are enumerated
Then zero references to `Microsoft.AspNetCore.Identity` or any Infrastructure namespace SHALL be present.

---

### AUTH-03 — Login Use Case (Application Layer)

**AUTH-03.1 — Repository port**

`IUsuarioRepository` MUST exist in `GastroGestion.Application/Abstractions/Persistence/` and MUST expose at minimum:

- `Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default)` — returns `null` when no user with that email exists.

**AUTH-03.2 — Token service port**

`ITokenService` MUST exist in `GastroGestion.Application` (e.g., `Application/Abstractions/Security/`) and MUST expose at minimum:

- A method that accepts a `Usuario` and returns a signed token string plus its expiry instant (e.g., as a `(string Token, DateTime ExpiresAt)` tuple or equivalent record).

No type from `System.IdentityModel.Tokens.Jwt` or any Infrastructure namespace MAY appear in the port definition.

**AUTH-03.3 — LoginHandler**

A `LoginHandler` class MUST exist in `GastroGestion.Application` following the one-handler-per-use-case convention. It MUST:

- Accept a `LoginCommand` (or equivalent command record) carrying `Email` and `Password`.
- Inject `IUsuarioRepository`, `ITokenService`, and `IPasswordHasher` (or the equivalent port) via the constructor.
- Call `IUsuarioRepository.GetByEmailAsync(email)`.
- On user not found: return a generic auth-failure result WITHOUT revealing that the email was not found.
- On user found but `Activo == false`: return the same generic auth-failure result WITHOUT revealing the account is inactive.
- On user found, active, but password verification fails: return the same generic auth-failure result WITHOUT revealing which part was wrong.
- On user found, active, and password verified: call `ITokenService` and return the token + expiry + minimal user info.

The handler MUST NOT throw an exception for failed credentials. It MUST return a discriminated result (e.g., a `OneOf`-style or `Result<T>` type, or a nullable success response) that the Api layer translates to HTTP 401.

**AUTH-03.4 — Contracts**

The following contract files MUST exist in `GastroGestion.Contracts`:

- `AuthRequests.cs` — sealed record `LoginRequest(string Email, string Password)`.
- `AuthResponses.cs` — sealed record `LoginResponse` containing at minimum: `Token` (string), `ExpiresAt` (DateTime UTC), `UserId` (Guid), `Rol` (string).
- `AuthValidators.cs` — `AbstractValidator<LoginRequest>` requiring both `Email` and `Password` to be non-empty.
- `AuthMappings.cs` — static extension methods mapping `LoginRequest` → `LoginCommand` (or equivalent).

**AUTH-03.5 — Scenarios**

**Scenario AUTH-03-A (successful login)**
Given a known active `Usuario` with email `"admin@restaurant.com"` and a correct password
When `LoginHandler.Handle` is called with matching credentials
Then the handler returns a success result containing a non-empty token string and an expiry in the future.

**Scenario AUTH-03-B (unknown email)**
Given no `Usuario` exists with email `"ghost@restaurant.com"`
When `LoginHandler.Handle` is called with that email
Then the handler returns a generic auth-failure result (not an exception, not a "user not found" message).

**Scenario AUTH-03-C (wrong password)**
Given a known active `Usuario` with email `"admin@restaurant.com"`
When `LoginHandler.Handle` is called with a wrong password
Then the handler returns the same generic auth-failure result as AUTH-03-B.

**Scenario AUTH-03-D (inactive user)**
Given a `Usuario` with `Activo = false`
When `LoginHandler.Handle` is called with correct credentials for that user
Then the handler returns the same generic auth-failure result as AUTH-03-B.

**Scenario AUTH-03-E (indistinguishable failure)**
Given scenarios AUTH-03-B, AUTH-03-C, and AUTH-03-D
Then the failure result returned in each case MUST be structurally identical (same type, same shape) such that the Api layer cannot distinguish between them and MUST NOT include a detail field that names the failure reason.

---

### AUTH-04 — JWT Token Issuance (Infrastructure Layer)

**AUTH-04.1 — Implementation**

The `ITokenService` implementation MUST exist in `GastroGestion.Infrastructure` and MUST use `JwtSecurityTokenHandler` (NOT `JsonWebTokenHandler`) to sign tokens.

**AUTH-04.2 — Token shape**

Every issued token MUST carry exactly these claims:

| Claim | Value |
|---|---|
| `sub` | `Usuario.Id.ToString()` |
| `email` | `Usuario.Email` |
| `ClaimTypes.Role` | `Usuario.Rol` enum name as a string (e.g., `"Administrador"`) |
| `iss` | Value of `Jwt:Issuer` from configuration |
| `aud` | Value of `Jwt:Audience` from configuration |
| `exp` | Issued time + **8 hours** (not 1 hour, not 24 hours) |

**AUTH-04.3 — Signing**

The token MUST be signed with the `SymmetricSecurityKey` derived from `Jwt:SigningKey` in configuration — the same key the existing `TokenValidationParameters` in `Program.cs` already uses. No new configuration keys are introduced for signing.

**AUTH-04.4 — DI registration**

The `ITokenService` implementation MUST be registered in `Infrastructure/DependencyInjection.cs`.

**AUTH-04.5 — Scenarios**

**Scenario AUTH-04-A (claim presence)**
Given a `Usuario` with `Id = X`, `Email = "e@r.com"`, `Rol = RolUsuario.Mozo`
When the token service issues a token
Then decoding the token reveals `sub = X.ToString()`, `email = "e@r.com"`, and a role claim whose value is `"Mozo"`.

**Scenario AUTH-04-B (expiry)**
Given a token issued at time T
When the token is decoded
Then the `exp` claim equals T + 8 hours (within a 5-second tolerance for clock drift).

**Scenario AUTH-04-C (validation pipeline round-trip)**
Given a token issued by the `ITokenService` implementation
When that token is presented to the existing `TokenValidationParameters` already wired in `Program.cs`
Then validation succeeds (issuer, audience, lifetime, and signature all pass).

**Scenario AUTH-04-D (tampered token)**
Given a token issued by the `ITokenService` implementation
When the token payload is modified without re-signing
Then the existing `TokenValidationParameters` MUST reject it (signature validation fails → 401 from the JWT bearer middleware).

---

### AUTH-05 — Login Endpoint (Api Layer)

**AUTH-05.1 — Route and method**

`POST /auth/login` MUST exist as a Minimal API endpoint. It MUST be marked `[AllowAnonymous]` (or the group-level equivalent) so it is reachable without a token.

**AUTH-05.2 — Success response**

On valid credentials, the endpoint MUST return HTTP 200 with a body matching `LoginResponse` (token, expiry, user id, role). The `Content-Type` MUST be `application/json`.

**AUTH-05.3 — Failure response**

On invalid credentials (any of the three failure cases from AUTH-03), the endpoint MUST return HTTP 401 with no body field that reveals which part of the credentials was wrong. A standard RFC 7807 `ProblemDetails` body is acceptable as long as the `detail` field is generic (e.g., `"Invalid credentials."`).

**AUTH-05.4 — Validation**

The endpoint MUST apply the `WithValidation<LoginRequest>()` filter. A request with a missing `Email` or missing `Password` field MUST return 400 before reaching the handler.

**AUTH-05.5 — Handler wiring**

`LoginHandler` MUST be injected directly into the endpoint delegate via DI, consistent with REQ-19. No mediator is used.

**AUTH-05.6 — Scenarios**

**Scenario AUTH-05-A (successful login)**
Given the system has a seeded active `Administrador` user
When `POST /auth/login` is called with the correct credentials for that user
Then the response is HTTP 200 with a JSON body containing a non-empty `token` field and an `expiresAt` field in the future.

**Scenario AUTH-05-B (wrong password)**
Given the system has a seeded active user
When `POST /auth/login` is called with the correct email but wrong password
Then the response is HTTP 401.

**Scenario AUTH-05-C (unknown email)**
Given no user exists with email `"unknown@restaurant.com"`
When `POST /auth/login` is called with that email
Then the response is HTTP 401.

**Scenario AUTH-05-D (missing fields)**
Given a request body with no `Email` property
When `POST /auth/login` is called
Then the response is HTTP 400 (`ValidationProblem`) before the handler is invoked.

**Scenario AUTH-05-E (401 body is non-revealing)**
Given an HTTP 401 response from `POST /auth/login`
When the response body is inspected
Then the `detail` field (if present) MUST NOT contain the words "email", "password", "user", "found", "inactive", "wrong", or any other term that differentiates between credential failure types.

---

### AUTH-06 — Endpoint Protection (Api Layer)

**AUTH-06.1 — Existing endpoints require authentication**

Every endpoint listed in the Phase-4 endpoint signatures table (REQ-15 through REQ-17, i.e., all routes under `/clientes`, `/ingredientes`, `/platos`, `/menus`, `/mesas`, `/pedidos`, `/facturas`, and `/stock`) MUST require a valid JWT bearer token after this change. Each `[AllowAnonymous]` attribute on those endpoints MUST be replaced with `[Authorize]` (or the group-level equivalent without an explicit `Roles=` parameter, except where AUTH-07 specifies otherwise).

**AUTH-06.2 — Health endpoint stays anonymous**

`GET /health` MUST remain reachable without a bearer token. Its response MUST continue to be HTTP 200.

**AUTH-06.3 — Login endpoint stays anonymous**

`POST /auth/login` MUST remain reachable without a bearer token (per AUTH-05.1).

**AUTH-06.4 — Authenticated callers unaffected**

A request that supplies a valid bearer token MUST receive the same response (status code and body shape) as it did when endpoints were `[AllowAnonymous]`. The `[AllowAnonymous]` → `[Authorize]` swap MUST NOT alter the functional behavior of any endpoint for authenticated requests.

**AUTH-06.5 — Scenarios**

**Scenario AUTH-06-A (unauthenticated request → 401)**
Given an endpoint previously marked `[AllowAnonymous]` (e.g., `GET /clientes`)
When a request is sent with no `Authorization` header
Then the response is HTTP 401.

**Scenario AUTH-06-B (authenticated request → original behavior)**
Given the same endpoint
When a request is sent with a valid `Authorization: Bearer <token>` header
Then the response is the same status code and body shape as it was when the endpoint was anonymous.

**Scenario AUTH-06-C (health stays open)**
Given `GET /health`
When a request is sent with no `Authorization` header
Then the response is HTTP 200.

**Scenario AUTH-06-D (all 25 endpoints covered)**
Given the full list of business endpoints (3 Cliente + 3 Ingrediente + 3 Plato + 3 Menu + 3 Mesa + 5 Pedido + 3 Factura + 2 Stock = 25)
When each is called without a bearer token
Then every one MUST return 401 (not 200, not 403, not 404 from a missing route).

---

### AUTH-07 — Role Extraction from JWT Claim — Pedido State Transition (Api Layer)

**AUTH-07.1 — Claim source**

The `POST /pedidos/{id}/transicion` endpoint MUST read `RolUsuario` from `HttpContext.User` via `ClaimTypes.Role`, NOT from the request body.

**AUTH-07.2 — Request DTO field removal**

`TransicionarEstadoRequest` in `GastroGestion.Contracts` MUST NOT contain a `Rol` field after this change. The record MUST carry only `EstadoNuevo` (or equivalent state field).

**AUTH-07.3 — Command construction**

The endpoint delegate MUST:

1. Extract the `ClaimTypes.Role` claim value from `HttpContext.User.FindFirst(ClaimTypes.Role)`.
2. Attempt to parse it to `RolUsuario` via `Enum.TryParse<RolUsuario>`.
3. If the claim is absent or unparseable: return HTTP 403 (`Forbidden`). (The request passed `[Authorize]` bearer validation, so the user IS authenticated — identity was established via the `sub` claim. A missing or corrupt role claim means the authenticated principal lacks a valid authorization attribute for this endpoint. Per RFC 7231 §6.5.3, 403 Forbidden is the correct response when the server understood the request but refuses to authorize it — 401 would incorrectly signal that re-authentication could help.)
4. On successful parse: build the existing `TransicionarEstadoPedidoCommand` with the parsed role and pass it to `TransicionarEstadoPedidoHandler`.

**AUTH-07.4 — Handler unchanged**

`TransicionarEstadoPedidoHandler` and the `Pedido.TransicionarEstado(EstadoPedido, RolUsuario)` domain method MUST NOT change. Domain-level role validation (invalid transition for role) continues to return 422 via `DomainException`.

**AUTH-07.5 — Scenarios**

**Scenario AUTH-07-A (role from token)**
Given an authenticated request with a JWT carrying `ClaimTypes.Role = "Mozo"`
When `POST /pedidos/{id}/transicion` is called with only `EstadoNuevo` in the body (no `Rol` field)
Then the handler receives a command with `Rol = RolUsuario.Mozo` and processes the transition normally.

**Scenario AUTH-07-B (missing role claim)**
Given an authenticated request whose JWT does not contain a `ClaimTypes.Role` claim
When `POST /pedidos/{id}/transicion` is called
Then the response is HTTP 403.

**Scenario AUTH-07-C (unparseable role claim)**
Given an authenticated request whose JWT contains `ClaimTypes.Role = "InvalidRole"`
When `POST /pedidos/{id}/transicion` is called
Then the response is HTTP 403.

**Scenario AUTH-07-D (Rol field rejected)**
Given a request body `{ "estadoNuevo": 1, "rol": 0 }` sent to `POST /pedidos/{id}/transicion`
When the request is deserialized
Then the `rol` field MUST be silently ignored (not bound to the command), because the DTO no longer declares that field. The role MUST come from the JWT claim only.

**Scenario AUTH-07-E (domain transition still validates)**
Given an authenticated `Cocinero` user
When `POST /pedidos/{id}/transicion` is called requesting a transition that `Cocinero` is not authorized to perform (per domain rules)
Then the response is HTTP 422 (`DomainException`) — the domain validation layer is unchanged.

---

### AUTH-08 — Initial Admin Seeding (Infrastructure + Api Layers)

**AUTH-08.1 — Trigger condition**

When the application starts in Development mode and the `Usuarios` table contains zero rows, the system MUST insert exactly one `Usuario` with `Rol = RolUsuario.Administrador` and `Activo = true`.

**AUTH-08.2 — Credentials source**

The seeded admin credentials (email and plain-text password) MUST be documented in one of the following, in order of preference:

1. `appsettings.Development.json` under a `Seed:AdminEmail` / `Seed:AdminPassword` key pair, OR
2. A clearly commented constant in the seeder class itself (acceptable for a known-clean-start system with no legacy users).

The chosen approach MUST be stated in the design artifact. The plain-text password MUST be hashed via the `IPasswordHasher` Infrastructure implementation before storage; it MUST NOT be stored as plain text in the database.

**AUTH-08.3 — Idempotency**

The seeder MUST check `Usuarios.AnyAsync()` before inserting. If any `Usuario` row already exists, the seeder MUST return without inserting, regardless of how many times it runs. Duplicate seeded admins MUST be impossible.

**AUTH-08.4 — Integration with existing seeder**

The admin seeding MUST integrate with the existing `DevDataSeeder` flow (per REQ-05) or be called from the same `IsDevelopment()` guard in `Program.cs`. The existing seeder's idempotency check (`Clientes.AnyAsync()`) is unaffected.

**AUTH-08.5 — Scenarios**

**Scenario AUTH-08-A (first run — table empty)**
Given the `Usuarios` table contains zero rows
When the application starts in Development mode
Then exactly one `Usuario` row is created, with `Rol = Administrador`, `Activo = true`, and a non-empty `PasswordHash`.

**Scenario AUTH-08-B (idempotent — table not empty)**
Given the `Usuarios` table already contains one or more rows
When the application starts in Development mode
Then no new `Usuario` rows are inserted (the seeder returns early).

**Scenario AUTH-08-C (seeded credentials work for login)**
Given the seeded admin user exists
When `POST /auth/login` is called with the documented seed credentials
Then the response is HTTP 200 with a valid token.

**Scenario AUTH-08-D (password stored hashed)**
Given the seeded `Usuario` row in the database
When the `PasswordHash` column is inspected
Then the value MUST NOT equal the plain-text password from the seed configuration.

---

### AUTH-09 — Cross-Cutting: Round-Trip Validation (Api Layer / Infrastructure Layer)

**AUTH-09.1 — Token validation pipeline unchanged**

The existing `TokenValidationParameters` block in `Program.cs` (issuer validation, audience validation, lifetime validation, signing key validation) MUST NOT be modified by this change. Tokens issued by the `ITokenService` implementation MUST be accepted by the existing validation pipeline without any configuration change.

**AUTH-09.2 — No ASP.NET Core Identity namespace in Domain or Application**

After this change, zero references to `Microsoft.AspNetCore.Identity` (or any sub-namespace) MUST appear in `GastroGestion.Domain.csproj` or `GastroGestion.Application.csproj` project files.

**AUTH-09.3 — Integration test infrastructure**

`ApiFactory` MUST expose a `GenerateTestToken(RolUsuario role)` method that uses `JwtSecurityTokenHandler` with the test signing key (`"TestSigningKeyForApiTestsMinimumLength32Chars"`), the test issuer (`"GastroGestion"`), and the test audience (`"GastroGestion"`) already injected at factory startup. Tokens generated by this method MUST pass the existing `TokenValidationParameters` used by the test host.

**AUTH-09.4 — Integration test migration**

All integration tests that call an endpoint now protected by `[Authorize]` MUST be updated to attach a bearer token obtained from `ApiFactory.GenerateTestToken(...)`. Tests for the Pedido state-transition path MUST no longer send `Rol` in the request body.

**AUTH-09.5 — No regressions**

After this change, `dotnet test` MUST pass with zero failures. The 222 tests that were green at the end of Phase 4 MUST remain green (plus any new tests added for auth).

**AUTH-09.6 — Scenarios**

**Scenario AUTH-09-A (issued token validates)**
Given a token issued by `ITokenService`
When `UseAuthentication()` + `UseAuthorization()` middleware processes a request carrying that token
Then the request is considered authenticated and reaches the endpoint handler.

**Scenario AUTH-09-B (expired token → 401)**
Given a token whose `exp` claim is in the past
When a protected endpoint is called with that token
Then the response is HTTP 401 (JWT bearer middleware rejects it before the endpoint is reached).

**Scenario AUTH-09-C (missing signing key → startup failure)**
Given `Jwt:SigningKey` is null or empty in configuration
When the application attempts to start
Then startup fails with `InvalidOperationException` (existing guard in `Program.cs` — MUST remain in place).

**Scenario AUTH-09-D (test suite passes)**
Given all integration tests have been updated to supply bearer tokens for protected endpoints
When `dotnet test` is executed
Then the exit code is 0 and no test reports a failure.

---

## Non-Goals (Explicitly Out of Scope for this Change)

The following items MUST NOT be implemented as part of `auth-jwt`. Implementing any of them would constitute scope creep and MUST be deferred to a later phase:

1. **Register / user CRUD endpoints** — no `POST /auth/register`, no admin user management surface.
2. **Refresh tokens** — no refresh-token table, rotation endpoint, or revocation mechanism.
3. **Second catalog / `GastroGestionSeguridadDbContext`** — the `Usuarios` table lives in the existing `GastroGestion` catalog and `GastroGestionDbContext` for Phase 5. Catalog separation is Phase 6 technical debt.
4. **Legacy data migration** — no import of `[dbo].[Usuario]` rows from the legacy stack, no HMACSHA512 verification fallback.
5. **Granular per-endpoint role policies** — beyond the Pedido state-transition path, no `[Authorize(Roles="...")]` policies are introduced. All other protected endpoints require authentication only.

---

## Single-Catalog Deviation (Recorded)

`openspec/config.yaml` describes a two-catalog architecture (`GastroGestion` for business data, `GastroGestion_Seguridad` for auth/users). This change intentionally deviates: the `Usuarios` table is added to the existing `GastroGestionDbContext` (single catalog). Rationale: no `GastroGestion_Seguridad` connection string exists in the .NET 8 appsettings; a second context adds disproportionate ceremony for zero Phase-5 benefit. This is Phase 6 technical debt, not an oversight.

---

## Dependency on Phase-4 Spec

All requirements from the Phase-4 Web API spec (`openspec/specs/Api/spec.md`, REQ-01..REQ-20) remain in force. This delta spec extends them. Specifically:

- REQ-04 (JWT pipeline wired, all Phase-4 endpoints anonymous) is superseded by AUTH-06: the pipeline remains wired, but endpoints transition from anonymous to protected.
- REQ-15 (`POST /pedidos/{id}/transicion` body includes `RolUsuario`) is superseded by AUTH-07: `Rol` is dropped from the request DTO; role comes from the JWT claim.
- REQ-05 (DevDataSeeder idempotent) is extended by AUTH-08: admin seeding is additive to the existing seeder.
