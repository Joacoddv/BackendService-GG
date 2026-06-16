# Tasks: auth-jwt (Phase 5 of 7)

**Last updated:** 2026-06-16
**Status:** PR1 COMPLETE — PR2 PENDING
**Delivery strategy:** Chained PRs — Stacked to main (2 PRs)
**Spec:** openspec/changes/auth-jwt/spec.md
**Design:** openspec/changes/auth-jwt/design.md

---

## PR Slicing

| Slice | Description | Merge target |
|-------|-------------|--------------|
| PR1   | Additive auth foundation — domain, ports, infra adapters, EF, seeder, login endpoint | main |
| PR2   | Lockdown + test migration — RequireAuthorization on groups, role-claim seam, test helpers, 3 test file fixes | main (after PR1 merged) |

PR1 is fully additive (zero existing behavior changes). PR2 removes `[AllowAnonymous]` and introduces the role-from-claim seam — all 222 existing tests are fixed in PR2 before the PR is opened.

---

## PR1 — Additive Auth Foundation

### [x] AJ-01 · Domain · PR1
**`Usuario` aggregate**

Create `src/GastroGestion.Domain/Usuarios/Usuario.cs`.

- Extends `AggregateRoot`, private setters on all properties, EF-ctor `private Usuario()`.
- Properties: `Email (string)`, `NombreCompleto (string)`, `Rol (RolUsuario)`, `PasswordHash (string)`, `Activo (bool)`.
- Static factory `Crear(email, nombreCompleto, rol, passwordHash)`:
  - Throws `DomainException` when email is null/whitespace.
  - Throws `DomainException` when email does not contain `@` with non-empty parts.
  - Throws `DomainException` when `nombreCompleto` is null/whitespace.
  - Throws `DomainException` when `passwordHash` is null/whitespace.
  - Returns instance with `Activo = true` and a new `Guid` id.
- Zero `<PackageReference>` additions to `GastroGestion.Domain.csproj`.

**Satisfies:** AUTH-01.1, AUTH-01.2, AUTH-01.3 (scenarios A–E)
**Verifiable:** Unit tests for all 5 scenarios (happy path + 4 guard throws) pass; `dotnet build GastroGestion.Domain.csproj` succeeds with zero new references.

---

### [x] AJ-02 · Application · PR1
**Application security ports + `AuthenticationFailedException`**

1. Create `src/GastroGestion.Application/Abstractions/Security/IPasswordHasher.cs`:
   - `string Hash(Usuario usuario, string plainPassword)`.
   - `bool Verify(Usuario usuario, string hashedPassword, string providedPassword)`.
2. Create `src/GastroGestion.Application/Abstractions/Security/ITokenIssuer.cs`:
   - `AccessToken Issue(Usuario usuario)`.
   - Companion `record AccessToken(string Value, DateTime ExpiresAtUtc)` in same file.
3. Create `src/GastroGestion.Application/Common/Exceptions/AuthenticationFailedException.cs`:
   - Extends `Exception` (sibling of existing `NotFoundException`).
   - Used by `LoginHandler` to signal any credential failure.
4. Confirm zero `Microsoft.AspNetCore.Identity` or Infrastructure namespace references in `Application.csproj`.

**Satisfies:** AUTH-02.1, AUTH-03.2, AUTH-09.2
**Verifiable:** Project builds; no identity namespace in `Application.csproj`; types exist at the declared paths.

---

### [x] AJ-03 · Application · PR1
**`IUsuarioRepository` persistence port**

Create `src/GastroGestion.Application/Abstractions/Persistence/IUsuarioRepository.cs`:

- `Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default)`.
- `Task<bool> AnyAsync(CancellationToken ct = default)`.
- `Task AddAsync(Usuario usuario, CancellationToken ct = default)`.

**Satisfies:** AUTH-03.1, AUTH-08.3
**Verifiable:** File exists at path; Application project builds.

*AJ-02 and AJ-03 can run in parallel.*

---

### [x] AJ-04 · Application · PR1
**Login command, result, and handler**

1. Create `src/GastroGestion.Application/Auth/Login/LoginCommand.cs` — `sealed record LoginCommand(string Email, string Password)`.
2. Create `src/GastroGestion.Application/Auth/Login/LoginResult.cs` — `sealed record LoginResult(string AccessToken, DateTime ExpiresAtUtc, Guid UsuarioId, RolUsuario Rol)`.
3. Create `src/GastroGestion.Application/Auth/Login/LoginHandler.cs`:
   - Constructor-injected: `IUsuarioRepository`, `IPasswordHasher`, `ITokenIssuer`.
   - `Handle(LoginCommand cmd, CancellationToken ct = default)` — async, returns `Task<LoginResult>`.
   - On user null or `Activo == false` → throw `AuthenticationFailedException("Invalid credentials.")`.
   - On `Verify` false → throw same exception (indistinguishable from above).
   - On success → call `_tokens.Issue(usuario)`, return `LoginResult`.
   - NO `IUnitOfWork` injection (read-only use case, ADR-8).
4. Register `LoginHandler` as scoped in `src/GastroGestion.Application/DependencyInjection.cs`.

**Satisfies:** AUTH-03.3, AUTH-03.5 (scenarios A–E)
**Depends on:** AJ-02, AJ-03
**Verifiable:** Unit tests for all 5 handler scenarios pass (mock repo + mock hasher + mock issuer); DI registration present.

---

### [x] AJ-05 · Contracts · PR1
**Auth contracts (4-file pattern)**

Create folder `src/GastroGestion.Contracts/Auth/`:

1. `AuthRequests.cs` — `sealed record LoginRequest(string Email, string Password)`.
2. `AuthResponses.cs` — `sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, Guid UsuarioId, RolUsuario Rol)`.
3. `AuthValidators.cs` — `LoginValidator : AbstractValidator<LoginRequest>` with `NotEmpty().EmailAddress()` on Email and `NotEmpty()` on Password.
4. `AuthMappings.cs` — static extension `ToCommand(this LoginRequest)` returning `LoginCommand`; static extension `ToResponse(this LoginResult)` returning `LoginResponse`.

**Satisfies:** AUTH-03.4, AUTH-05.4
**Depends on:** AJ-04
**Verifiable:** Contracts project builds; validation filter returns 400 on a request missing Email.

---

### [x] AJ-06 · Infrastructure · PR1
**`PasswordHasherAdapter` — PBKDF2 implementation**

Create `src/GastroGestion.Infrastructure/Security/PasswordHasherAdapter.cs`:

- `internal sealed class PasswordHasherAdapter : IPasswordHasher`.
- Wraps `Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>`.
- `Hash`: calls `_inner.HashPassword(usuario, plainPassword)`, returns string.
- `Verify`: calls `_inner.VerifyHashedPassword(usuario, hashedPassword, providedPassword)`, returns bool (true on `Success` or `SuccessRehashNeeded`).
- Confirm `Microsoft.AspNetCore.Identity` dependency stays in Infrastructure only.

**Satisfies:** AUTH-02.2, AUTH-02.4 (scenarios A–B), AUTH-09.2
**Depends on:** AJ-02
**Verifiable:** Unit test: hash then verify returns true; wrong password returns false.

---

### [x] AJ-07 · Infrastructure · PR1
**`JwtTokenIssuer` — JWT signing adapter**

Create `src/GastroGestion.Infrastructure/Security/JwtTokenIssuer.cs`:

- `internal sealed class JwtTokenIssuer : ITokenIssuer`.
- Constructor-injected `IConfiguration`.
- Reads `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey` from config (same keys as `Program.cs:59-62`).
- Uses `JwtSecurityTokenHandler` (NOT `JsonWebTokenHandler`).
- Signs with `SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))` / `HmacSha256`.
- Claims: `sub = usuario.Id.ToString()`, `email = usuario.Email`, `ClaimTypes.Role = usuario.Rol.ToString()`.
- Expiry: `DateTime.UtcNow.AddHours(8)`.
- Returns `AccessToken(token, expiresAtUtc)`.

**Satisfies:** AUTH-04.1, AUTH-04.2, AUTH-04.3, AUTH-04.5 (scenarios A–D)
**Depends on:** AJ-02
**Verifiable:** Unit test: issued token contains `sub`, `email`, `ClaimTypes.Role` claims; `exp` is within 5s of `UtcNow + 8h`; token validates against matching `TokenValidationParameters`.

---

### [x] AJ-08 · Infrastructure · PR1
**`UsuarioRepository` — EF persistence adapter**

Create `src/GastroGestion.Infrastructure/Persistence/Repositories/UsuarioRepository.cs`:

- `internal sealed class UsuarioRepository : IUsuarioRepository`.
- Constructor-injected `GastroGestionDbContext`.
- `GetByEmailAsync` → `FirstOrDefaultAsync(u => u.Email == email, ct)`.
- `AnyAsync` → `_ctx.Usuarios.AnyAsync(ct)`.
- `AddAsync` → `await _ctx.Usuarios.AddAsync(usuario, ct)`.

**Satisfies:** AUTH-03.1
**Depends on:** AJ-03, AJ-09 (DbSet must exist before repository compiles)
**Note:** AJ-08 and AJ-09 should land in the same commit for a clean build.

---

### [x] AJ-09 · Infrastructure · PR1
**`UsuarioConfiguration` + `Usuarios` DbSet**

1. Create `src/GastroGestion.Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs`:
   - `internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>`.
   - `ToTable("Usuarios")`, `HasKey(u => u.Id)`, `ValueGeneratedNever()`.
   - `Email`: `IsRequired`, `HasMaxLength(320)`, unique index `HasIndex(u => u.Email).IsUnique()`.
   - `NombreCompleto`: `IsRequired`, `HasMaxLength(200)`.
   - `Rol`: `HasConversion<int>()`.
   - `PasswordHash`: `IsRequired`.
   - `Activo`: no constraint (nullable bool defaults to false on uninitialised; here always set by factory).
2. Add `public DbSet<Usuario> Usuarios => Set<Usuario>();` to `GastroGestionDbContext`.

**Satisfies:** AUTH-01.1 (persistence contract), AUTH-08 (DbSet prerequisite)
**Depends on:** AJ-01
**Verifiable:** `dotnet build` succeeds; config is picked up by `ApplyConfigurationsFromAssembly`.

---

### [x] AJ-10 · Infrastructure · PR1
**EF migration `AddUsuarios`**

Run:
```bash
dotnet ef migrations add AddUsuarios \
  --project src/GastroGestion.Infrastructure \
  --startup-project src/GastroGestion.Api
```

Verify the generated migration:
- Adds `Usuarios` table with all columns; does NOT alter any existing table.
- Forward-only (no legacy data import).

**Satisfies:** AUTH-08 prerequisite; Phase-4 spec REQ-05 (auto-applied in dev via `MigrateAsync`)
**Depends on:** AJ-09
**Verifiable:** Migration file generated; `dotnet ef database update` succeeds against a clean dev DB.

---

### [x] AJ-11 · Infrastructure · PR1
**Admin seeder — extend `DevDataSeeder`**

In `src/GastroGestion.Infrastructure/Persistence/Seeders/DevDataSeeder.cs` (or equivalent):

1. Inject `IUsuarioRepository` and `IPasswordHasher` (or resolve via DI inside the seeder — match existing pattern).
2. Read `Seed:AdminEmail` / `Seed:AdminPassword` from `IConfiguration`; fall back to documented constants if keys are absent (e.g., `admin@gastrogestion.local` / `Admin1234!`).
3. If `await _usuarioRepository.AnyAsync(ct)` is true → return without inserting.
4. Otherwise: `IPasswordHasher.Hash(...)`, `Usuario.Crear(email, "Admin", RolUsuario.Administrador, hash)`, `AddAsync`, `SaveChangesAsync`.
5. Document seed credentials in `src/GastroGestion.Api/appsettings.Development.json` under `Seed:AdminEmail` / `Seed:AdminPassword`.

**Satisfies:** AUTH-08.1, AUTH-08.2, AUTH-08.3, AUTH-08.4, AUTH-08.5 (scenarios A–D)
**Depends on:** AJ-03, AJ-06, AJ-10
**Verifiable:** Starting app on an empty DB creates exactly one `Usuario` row; restarting does not create a second row.

---

### [x] AJ-12 · Infrastructure · PR1
**DI registrations — Infrastructure**

In `src/GastroGestion.Infrastructure/DependencyInjection.cs`:

- `services.AddScoped<IPasswordHasher, PasswordHasherAdapter>()`.
- `services.AddScoped<ITokenIssuer, JwtTokenIssuer>()`.
- `services.AddScoped<IUsuarioRepository, UsuarioRepository>()`.

**Satisfies:** AUTH-02.3, AUTH-04.4
**Depends on:** AJ-06, AJ-07, AJ-08
**Verifiable:** App starts; `LoginHandler` resolves from DI without exceptions.

---

### [x] AJ-13 · Api · PR1
**`AuthEndpoints` — `POST /auth/login`**

Create `src/GastroGestion.Api/Endpoints/AuthEndpoints.cs`:

- `MapGroup("/auth").WithTags("Auth")` — NO `.RequireAuthorization()` on this group.
- `MapPost("/login", [AllowAnonymous] async (...) => ...)`.
- Inject `LoginHandler` via delegate parameter (consistent with REQ-19).
- On success → `Results.Ok(result.ToResponse())` (HTTP 200).
- Catch `AuthenticationFailedException` (or rely on existing exception handler mapping) → HTTP 401 with generic `ProblemDetails` (`detail: "Invalid credentials."`).
- Apply `.WithValidation<LoginRequest>()` filter.
- Register the exception handler mapping for `AuthenticationFailedException` → 401 in the existing exception-handler middleware (same place `DomainException` → 422 and `NotFoundException` → 404 are mapped).
- Wire `app.MapAuthEndpoints()` in `Program.cs` alongside other `Map*Endpoints()` calls.

**Satisfies:** AUTH-05.1, AUTH-05.2, AUTH-05.3, AUTH-05.4, AUTH-05.5, AUTH-05.6 (scenarios A–E)
**Depends on:** AJ-04, AJ-05, AJ-12
**Verifiable:** `POST /auth/login` with valid seeded credentials returns 200 + JSON body; wrong password returns 401; missing Email returns 400.

---

## PR2 — Lockdown + Test Migration

*PR2 must not be opened until PR1 is merged to main.*

### AJ-14 · Api · PR2
**Group-level `.RequireAuthorization()` — 8 endpoint files**

For each of the 8 business endpoint files (ClienteEndpoints, IngredienteEndpoints, PlatoEndpoints, MenuEndpoints, MesaEndpoints, PedidoEndpoints, FacturaEndpoints, StockEndpoints):

- Add `.RequireAuthorization()` to the `MapGroup(...)` call.
- Remove ALL `[AllowAnonymous]` attributes from individual endpoint lambdas within the group.

Do NOT touch `Program.cs:MapHealthChecks` (stays anonymous) and do NOT touch `AuthEndpoints` (its group has no `.RequireAuthorization()`).

**Satisfies:** AUTH-06.1, AUTH-06.2, AUTH-06.3, AUTH-06.5 (scenarios A–D)
**Verifiable:** After applying, a request to `GET /clientes` without a token returns 401; `GET /health` without a token returns 200.

---

### AJ-15 · Contracts · PR2
**Drop `Rol` from `TransicionarEstadoRequest` (PHASE-5 seam 1)**

In `src/GastroGestion.Contracts/Pedidos/PedidoRequests.cs` (line 26):

- Change `public sealed record TransicionarEstadoRequest(EstadoPedido EstadoNuevo, RolUsuario Rol)` to `public sealed record TransicionarEstadoRequest(EstadoPedido EstadoNuevo)`.
- **`TransicionarEstadoPedidoCommand` MUST NOT change** — it deliberately retains its `Rol` field; only the DTO loses it.

**Satisfies:** AUTH-07.2
**Verifiable:** Build succeeds; the record no longer compiles with a `Rol` argument.

---

### AJ-16 · Contracts · PR2
**Update `PedidoMappings` — role as parameter (PHASE-5 seam 2 prep)**

In `src/GastroGestion.Contracts/Pedidos/PedidoMappings.cs` (line ~29-30):

- Change `ToCommand` for `TransicionarEstadoRequest` to accept `RolUsuario rol` as a second parameter:
  `public static TransicionarEstadoPedidoCommand ToCommand(this TransicionarEstadoRequest request, Guid pedidoId, RolUsuario rol) => new(pedidoId, request.EstadoNuevo, rol)`.

**Satisfies:** AUTH-07.2 (contract side of seam)
**Depends on:** AJ-15
**Verifiable:** Mapping compiles; call-site in PedidoEndpoints (AJ-17) is the only consumer.

---

### AJ-17 · Api · PR2
**Pedido transition: role-from-claim extraction (PHASE-5 seam 2 complete)**

In `src/GastroGestion.Api/Endpoints/PedidoEndpoints.cs` (line ~54–66):

- Add `HttpContext http` parameter to the `POST /{id:guid}/transicion` delegate.
- Extract: `var rolClaim = http.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;`.
- If `rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol)` → `return Results.Problem(title: "Invalid or missing role claim.", statusCode: StatusCodes.Status403Forbidden)`.
- Replace old `request.ToCommand(id, ...)` with `request.ToCommand(id, rol)`.
- `TransicionarEstadoPedidoHandler` and its command are UNCHANGED (PHASE-5 seam 3 — handler stays as-is).

**Satisfies:** AUTH-07.1, AUTH-07.3, AUTH-07.4, AUTH-07.5 (scenarios A–E)
**Depends on:** AJ-15, AJ-16
**Verifiable:** Authenticated request with `ClaimTypes.Role = "Mozo"` in JWT processes transition; missing role claim returns 403; domain still returns 422 for invalid role transitions.

---

### AJ-18 · Tests · PR2
**`ApiFactory` — `GenerateTestToken` + `CreateAuthenticatedClient` helpers**

In `tests/GastroGestion.Api.Tests/ApiFactory.cs`:

- Add `public string GenerateTestToken(RolUsuario role)`:
  - Signs with `TestJwtSigningKey` (existing private const), `HmacSha256`.
  - Issuer: `"GastroGestion"`, Audience: `"GastroGestion"` (matching existing test config).
  - Claims: `sub = Guid.NewGuid().ToString()`, `ClaimTypes.Role = role.ToString()`.
  - Expiry: `DateTime.UtcNow.AddHours(8)`.
  - Returns token string via `JwtSecurityTokenHandler`.
- Add `public HttpClient CreateAuthenticatedClient(RolUsuario role = RolUsuario.Administrador)`:
  - Calls `CreateClient()` then sets `DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateTestToken(role))`.
  - Returns the configured client.

**Satisfies:** AUTH-09.3
**Verifiable:** `GenerateTestToken(RolUsuario.Administrador)` produces a token that validates against the test host's `TokenValidationParameters`.

---

### AJ-19 · Tests · PR2
**Migrate `CatalogueEndpointTests` to authenticated client**

In `tests/GastroGestion.Api.Tests/CatalogueEndpointTests.cs` (line ~35):

- Change `_client = factory.CreateClient();` → `_client = factory.CreateAuthenticatedClient(RolUsuario.Administrador);`.
- No other changes needed in this file — all catalogue tests become authenticated with this single line.

**Satisfies:** AUTH-09.4, AUTH-09.5
**Depends on:** AJ-18
**Verifiable:** All catalogue tests pass after PR2's endpoint lockdown is applied.

---

### AJ-20 · Tests · PR2
**Migrate `TransactionalEndpointTests` to authenticated client + fix Pedido seam tests**

In `tests/GastroGestion.Api.Tests/TransactionalEndpointTests.cs`:

1. Line ~35: `_client = factory.CreateClient();` → `_client = factory.CreateAuthenticatedClient(RolUsuario.Administrador);`.
2. Line ~176 (Pedido valid-role test): change request body from `new TransicionarEstadoRequest(EstadoPedido.Preparandose, RolUsuario.Cajero)` to `new TransicionarEstadoRequest(EstadoPedido.Preparandose)` AND swap client to `factory.CreateAuthenticatedClient(RolUsuario.Cajero)` so the role travels in the JWT.
3. Line ~194 (Pedido wrong-role 422 test): same body fix + swap client to `factory.CreateAuthenticatedClient(RolUsuario.Cocinero)` (or whichever role triggers the 422).

**Satisfies:** AUTH-09.4, AUTH-09.5
**Depends on:** AJ-18, AJ-15
**Verifiable:** All transactional tests pass, including both Pedido seam tests.

---

### AJ-21 · Tests · PR2
**Verify `SmokeTests` remain anonymous (no change — verification step)**

Confirm `tests/GastroGestion.Api.Tests/SmokeTests.cs`:

- Still uses `factory.CreateClient()` (unauthenticated) — no change needed.
- Tests only `/health` (anonymous) and an unknown route — both pass without a token.
- This task is a read-and-confirm, not an edit.

**Satisfies:** AUTH-06.2, AUTH-06.5-C (health stays open)
**Depends on:** AJ-14
**Verifiable:** Smoke tests pass with the unauthenticated client after lockdown is live.

---

### AJ-22 · Tests · PR2
**Full test suite green — `dotnet test` validation**

Run `dotnet test` and confirm:

- Exit code 0.
- 222 pre-existing tests pass.
- Any new tests added in AJ-01 (domain unit), AJ-04 (handler unit), AJ-06 (hasher unit), AJ-07 (issuer unit) also pass.
- No `[AllowAnonymous]` test bypasses remain that would hide lockdown regressions.

**Satisfies:** AUTH-09.5, AUTH-09.6 (scenario D)
**Depends on:** AJ-14 through AJ-21
**Verifiable:** CI green; `dotnet test --no-build` exit 0.

---

## Task Dependency Summary

```
AJ-01 (Domain: Usuario)
  └─► AJ-09 (EF Config + DbSet)
        └─► AJ-10 (Migration)
              └─► AJ-11 (Seeder)

AJ-02 (Ports + Exception)   ←── parallel with AJ-03
AJ-03 (IUsuarioRepository)  ─┐
                              ├─► AJ-04 (LoginHandler) ─► AJ-05 (Contracts) ─► AJ-13 (Endpoint)
AJ-02 ───────────────────────┘
  ├─► AJ-06 (PasswordHasherAdapter)
  └─► AJ-07 (JwtTokenIssuer)

AJ-08 (UsuarioRepository) ─► depends on AJ-03 + AJ-09

AJ-06 + AJ-07 + AJ-08 ─► AJ-12 (DI registrations) ─► AJ-13

AJ-12 + AJ-05 + AJ-04 ─► AJ-13 (POST /auth/login)

--- PR1 merge boundary ---

AJ-14 (RequireAuthorization × 8 groups)
AJ-15 (Drop Rol from TransicionarEstadoRequest)
  └─► AJ-16 (PedidoMappings update)
        └─► AJ-17 (Pedido role-from-claim endpoint)

AJ-18 (ApiFactory helpers)
  ├─► AJ-19 (CatalogueEndpointTests fix)
  └─► AJ-20 (TransactionalEndpointTests fix + seam)

AJ-14 ─► AJ-21 (SmokeTests verify)

AJ-14..AJ-21 ─► AJ-22 (dotnet test full pass)
```

---

## Parallel Execution Notes

**PR1 — can run in parallel:**
- AJ-01 (domain) is independent of AJ-02/AJ-03 (ports).
- AJ-02 and AJ-03 can run in parallel with each other.
- AJ-06 and AJ-07 can run in parallel after AJ-02 lands.
- AJ-09 can start as soon as AJ-01 lands.

**PR1 — must be sequential:**
- AJ-04 requires AJ-02 + AJ-03.
- AJ-08 requires AJ-03 + AJ-09.
- AJ-10 requires AJ-09.
- AJ-11 requires AJ-03 + AJ-06 + AJ-10.
- AJ-12 requires AJ-06 + AJ-07 + AJ-08.
- AJ-13 requires AJ-04 + AJ-05 + AJ-12.

**PR2 — can run in parallel:**
- AJ-14 is independent of AJ-15/AJ-16/AJ-17.
- AJ-18 is independent of AJ-14/AJ-15.

**PR2 — must be sequential:**
- AJ-16 requires AJ-15.
- AJ-17 requires AJ-15 + AJ-16.
- AJ-19 and AJ-20 require AJ-18.
- AJ-20 also requires AJ-15 (body shape change).
- AJ-21 requires AJ-14.
- AJ-22 requires everything in PR2.

---

## Spec Traceability

| Spec Requirement | Tasks |
|---|---|
| AUTH-01 (Usuario aggregate) | AJ-01 |
| AUTH-02 (Password hashing port + impl) | AJ-02, AJ-06, AJ-12 |
| AUTH-03 (Login use case) | AJ-02, AJ-03, AJ-04, AJ-05 |
| AUTH-04 (JWT token issuance) | AJ-07, AJ-12 |
| AUTH-05 (Login endpoint) | AJ-05, AJ-13 |
| AUTH-06 (Endpoint protection) | AJ-14 |
| AUTH-07 (Role from JWT claim — Pedido) | AJ-15, AJ-16, AJ-17 |
| AUTH-08 (Admin seeding) | AJ-09, AJ-10, AJ-11 |
| AUTH-09 (Round-trip + test migration) | AJ-18, AJ-19, AJ-20, AJ-21, AJ-22 |

---

## Review Workload Forecast

### PR1 — Additive Auth Foundation

| File | Estimated lines changed |
|------|------------------------|
| `Domain/Usuarios/Usuario.cs` (new) | ~50 |
| `Application/Abstractions/Security/IPasswordHasher.cs` (new) | ~15 |
| `Application/Abstractions/Security/ITokenIssuer.cs` (new) | ~15 |
| `Application/Common/Exceptions/AuthenticationFailedException.cs` (new) | ~10 |
| `Application/Abstractions/Persistence/IUsuarioRepository.cs` (new) | ~15 |
| `Application/Auth/Login/LoginCommand.cs` (new) | ~5 |
| `Application/Auth/Login/LoginResult.cs` (new) | ~10 |
| `Application/Auth/Login/LoginHandler.cs` (new) | ~35 |
| `Application/DependencyInjection.cs` (add 1 registration) | ~3 |
| `Contracts/Auth/AuthRequests.cs` (new) | ~5 |
| `Contracts/Auth/AuthResponses.cs` (new) | ~8 |
| `Contracts/Auth/AuthValidators.cs` (new) | ~12 |
| `Contracts/Auth/AuthMappings.cs` (new) | ~15 |
| `Infrastructure/Security/PasswordHasherAdapter.cs` (new) | ~30 |
| `Infrastructure/Security/JwtTokenIssuer.cs` (new) | ~50 |
| `Infrastructure/Persistence/Repositories/UsuarioRepository.cs` (new) | ~25 |
| `Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs` (new) | ~30 |
| `Infrastructure/Persistence/Seeders/DevDataSeeder.cs` (extend) | ~30 |
| `Infrastructure/DependencyInjection.cs` (add 3 registrations) | ~6 |
| `Infrastructure/Migrations/AddUsuarios.cs` (generated) | ~60 |
| `Api/Endpoints/AuthEndpoints.cs` (new) | ~30 |
| `Api/Program.cs` (add MapAuthEndpoints + exception handler entry) | ~5 |
| `Api/appsettings.Development.json` (add Seed keys) | ~5 |
| `GastroGestionDbContext.cs` (add 1 DbSet line) | ~2 |
| **Unit tests for AJ-01, AJ-04, AJ-06, AJ-07** | ~120 |
| **PR1 total (est.)** | **~600 lines** |

> 400-line budget risk: **High** (600 lines estimated).
> The EF migration file (~60 lines) and test files (~120 lines) inflate this. Migration diffs are mechanical/generated and typically skipped in review. Excluding migration + tests: ~420 lines of hand-written code — still above 400 but borderline. Reviewer time ~70–80 minutes. Within one focused session; `size:exception` is acceptable here given all files are additive (no existing code modified) and the domain, ports, and infrastructure are strongly cohesive.

### PR2 — Lockdown + Test Migration

| File | Estimated lines changed |
|------|------------------------|
| 8 endpoint files (remove `[AllowAnonymous]` + add `.RequireAuthorization()`) | ~40 |
| `Contracts/Pedidos/PedidoRequests.cs` (drop `Rol` field) | ~3 |
| `Contracts/Pedidos/PedidoMappings.cs` (update `ToCommand` signature) | ~5 |
| `Api/Endpoints/PedidoEndpoints.cs` (role-from-claim, ~10 lines changed) | ~15 |
| `Tests/ApiFactory.cs` (add 2 helpers) | ~30 |
| `Tests/CatalogueEndpointTests.cs` (1 line changed) | ~2 |
| `Tests/TransactionalEndpointTests.cs` (3 lines changed: constructor + 2 seam tests) | ~15 |
| **PR2 total (est.)** | **~110 lines** |

> 400-line budget risk: **Low** (110 lines estimated).

### Overall Summary

| PR | Estimated lines | Chained PRs recommended | 400-line budget risk | Decision needed before apply |
|----|----------------|------------------------|----------------------|------------------------------|
| PR1 | ~600 | Yes | High | No — `size:exception` accepted for additive-only new files; migration + test inflation are the primary drivers |
| PR2 | ~110 | Yes | Low | No |
| **Total** | **~710** | **Yes** | **Medium (split reduces per-PR risk)** | **No** |

**Chained PRs recommended:** Yes (already planned — stacked-to-main, 2 PRs).
**Decision needed before apply:** No — chaining strategy is locked (`stacked-to-main`). PR1 carries a `size:exception` rationale: all changes are additive new files, and the migration + test files inflate the count mechanically without increasing review complexity.
