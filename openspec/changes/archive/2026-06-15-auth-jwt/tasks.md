# Tasks: auth-jwt (Phase 5 of 7)

**Last updated:** 2026-06-16 (ARCHIVED)
**Status:** ALL 22 TASKS COMPLETE — PR1 merged @ 9e4835b, PR2 merged @ f7724c8
**Delivery strategy:** Chained PRs — Stacked to main (2 PRs)
**Spec:** openspec/specs/Api/spec.md (AUTH-01..AUTH-09)
**Design:** see openspec/changes/archive/2026-06-15-auth-jwt/design.md

---

## Summary

All 22 tasks (AJ-01..AJ-22) are complete and marked [x]. 245 integration tests pass (160 domain + 6 app + 33 infra + 46 api).

### Execution timeline
- **PR1 (additive foundation)**: merged to main @ commit 9e4835b.
- **PR2 (lockdown + test migration)**: merged to main @ commit f7724c8.

---

## PR Slicing

| Slice | Description | Merge target | Status |
|-------|-------------|--------------|--------|
| PR1   | Additive auth foundation — domain, ports, infra adapters, EF, seeder, login endpoint | main | MERGED |
| PR2   | Lockdown + test migration — RequireAuthorization on groups, role-claim seam, test helpers, test file fixes | main (after PR1 merged) | MERGED |

---

## PR1 — Additive Auth Foundation

### [x] AJ-01 · Domain · PR1 — `Usuario` aggregate
- Extends `AggregateRoot`, private setters on all properties, EF-ctor `private Usuario()`.
- Properties: `Email`, `NombreCompleto`, `Rol` (`RolUsuario`), `PasswordHash`, `Activo`.
- Static factory `Crear(email, nombreCompleto, rol, passwordHash)` with validation.
- **Satisfies:** AUTH-01.1, AUTH-01.2, AUTH-01.3 (scenarios A–E).

### [x] AJ-02 · Application · PR1 — Application security ports + `AuthenticationFailedException`
- `IPasswordHasher` port with `Hash` and `Verify` methods.
- `ITokenIssuer` port with `Issue` method returning `AccessToken` record.
- `AuthenticationFailedException` exception.
- **Satisfies:** AUTH-02.1, AUTH-03.2, AUTH-09.2.

### [x] AJ-03 · Application · PR1 — `IUsuarioRepository` persistence port
- `GetByEmailAsync(string email, CancellationToken ct)`.
- `AnyAsync(CancellationToken ct)`.
- `AddAsync(Usuario usuario, CancellationToken ct)`.
- **Satisfies:** AUTH-03.1, AUTH-08.3.

### [x] AJ-04 · Application · PR1 — Login command, result, and handler
- `LoginCommand` record.
- `LoginResult` record.
- `LoginHandler` with constructor-injected repo, hasher, token issuer.
- On success: call token issuer, return `LoginResult`.
- On failure: throw `AuthenticationFailedException` (not discriminated result, but API handler catches it).
- **Satisfies:** AUTH-03.3, AUTH-03.5 (scenarios A–E).

### [x] AJ-05 · Contracts · PR1 — Auth contracts (4-file pattern)
- `LoginRequest` record.
- `LoginResponse` record.
- `LoginValidator : AbstractValidator<LoginRequest>`.
- `AuthMappings` with `ToCommand` and `ToResponse` extensions.
- **Satisfies:** AUTH-03.4, AUTH-05.4.

### [x] AJ-06 · Infrastructure · PR1 — `PasswordHasherAdapter` — PBKDF2 implementation
- Wraps `Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>`.
- `Hash` and `Verify` methods.
- **Satisfies:** AUTH-02.2, AUTH-02.4 (scenarios A–B), AUTH-09.2.

### [x] AJ-07 · Infrastructure · PR1 — `JwtTokenIssuer` — JWT signing adapter
- Uses `JwtSecurityTokenHandler` (NOT `JsonWebTokenHandler`).
- Reads `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey` from config.
- Signs with `SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))` / `HmacSha256`.
- Claims: `sub = usuario.Id.ToString()`, `email = usuario.Email`, `ClaimTypes.Role = usuario.Rol.ToString()`.
- Expiry: `DateTime.UtcNow.AddHours(8)`.
- **Satisfies:** AUTH-04.1, AUTH-04.2, AUTH-04.3, AUTH-04.5 (scenarios A–D).

### [x] AJ-08 · Infrastructure · PR1 — `UsuarioRepository` — EF persistence adapter
- `GetByEmailAsync` → `FirstOrDefaultAsync(u => u.Email == email, ct)`.
- `AnyAsync` → `_ctx.Usuarios.AnyAsync(ct)`.
- `AddAsync` → `await _ctx.Usuarios.AddAsync(usuario, ct)`.
- **Satisfies:** AUTH-03.1.

### [x] AJ-09 · Infrastructure · PR1 — `UsuarioConfiguration` + `Usuarios` DbSet
- `IEntityTypeConfiguration<Usuario>`, `ToTable("Usuarios")`, `HasKey(u => u.Id)`, `ValueGeneratedNever()`.
- `Email`: `IsRequired`, `HasMaxLength(320)`, unique index.
- `NombreCompleto`: `IsRequired`, `HasMaxLength(200)`.
- `Rol`: `HasConversion<int>()`.
- `PasswordHash`: `IsRequired`.
- `Activo`: no constraint.
- Add `public DbSet<Usuario> Usuarios => Set<Usuario>();` to `GastroGestionDbContext`.
- **Satisfies:** AUTH-01.1 (persistence contract), AUTH-08 (DbSet prerequisite).

### [x] AJ-10 · Infrastructure · PR1 — EF migration `AddUsuarios`
- Adds `Usuarios` table with all columns; does NOT alter any existing table.
- Forward-only.
- **Satisfies:** AUTH-08 prerequisite; Phase-4 spec REQ-05 (auto-applied in dev via `MigrateAsync`).

### [x] AJ-11 · Infrastructure · PR1 — Admin seeder — extend `DevDataSeeder`
- Inject `IUsuarioRepository` and `IPasswordHasher`.
- Read `Seed:AdminEmail` / `Seed:AdminPassword` from `IConfiguration`; fall back to documented constants.
- If `await _usuarioRepository.AnyAsync(ct)` is true → return without inserting.
- Otherwise: hash password, create `Usuario.Crear(email, "Admin", RolUsuario.Administrador, hash)`, `AddAsync`, `SaveChangesAsync`.
- **Satisfies:** AUTH-08.1, AUTH-08.2, AUTH-08.3, AUTH-08.4, AUTH-08.5 (scenarios A–D).

### [x] AJ-12 · Infrastructure · PR1 — DI registrations — Infrastructure
- `services.AddScoped<IPasswordHasher, PasswordHasherAdapter>()`.
- `services.AddScoped<ITokenIssuer, JwtTokenIssuer>()`.
- `services.AddScoped<IUsuarioRepository, UsuarioRepository>()`.
- **Satisfies:** AUTH-02.3, AUTH-04.4.

### [x] AJ-13 · Api · PR1 — `AuthEndpoints` — `POST /auth/login`
- `MapGroup("/auth").WithTags("Auth")` — NO `.RequireAuthorization()` on this group.
- `MapPost("/login", [AllowAnonymous] async (...) => ...)`.
- Inject `LoginHandler` via delegate parameter.
- On success → `Results.Ok(result.ToResponse())` (HTTP 200).
- Catch `AuthenticationFailedException` → HTTP 401 via exception handler.
- Apply `.WithValidation<LoginRequest>()` filter.
- Register exception handler mapping for `AuthenticationFailedException` → 401.
- Wire `app.MapAuthEndpoints();` in `Program.cs`.
- **Satisfies:** AUTH-05.1, AUTH-05.2, AUTH-05.3, AUTH-05.4, AUTH-05.5, AUTH-05.6 (scenarios A–E).

---

## PR2 — Lockdown + Test Migration

*PR2 must not be opened until PR1 is merged to main.*

### [x] AJ-14 · Api · PR2 — Group-level `.RequireAuthorization()` — 8 endpoint files
- Add `.RequireAuthorization()` to the `MapGroup(...)` call on: ClienteEndpoints, IngredienteEndpoints, PlatoEndpoints, MenuEndpoints, MesaEndpoints, PedidoEndpoints, FacturaEndpoints, StockEndpoints.
- Remove ALL `[AllowAnonymous]` attributes from individual endpoint lambdas within the group.
- Do NOT touch `Program.cs:MapHealthChecks` (stays anonymous) and do NOT touch `AuthEndpoints` (its group has no `.RequireAuthorization()`).
- **Satisfies:** AUTH-06.1, AUTH-06.2, AUTH-06.3, AUTH-06.5 (scenarios A–D).

### [x] AJ-15 · Contracts · PR2 — Drop `Rol` from `TransicionarEstadoRequest`
- Change `public sealed record TransicionarEstadoRequest(EstadoPedido EstadoNuevo, RolUsuario Rol)` to `public sealed record TransicionarEstadoRequest(EstadoPedido EstadoNuevo)`.
- `TransicionarEstadoPedidoCommand` MUST NOT change — it deliberately retains its `Rol` field.
- **Satisfies:** AUTH-07.2.

### [x] AJ-16 · Contracts · PR2 — Update `PedidoMappings` — role as parameter
- Change `ToCommand` for `TransicionarEstadoRequest` to accept `RolUsuario rol` as a second parameter:
  `public static TransicionarEstadoPedidoCommand ToCommand(this TransicionarEstadoRequest request, Guid pedidoId, RolUsuario rol) => new(pedidoId, request.EstadoNuevo, rol)`.
- **Satisfies:** AUTH-07.2 (contract side of seam).

### [x] AJ-17 · Api · PR2 — Pedido transition: role-from-claim extraction
- Add `HttpContext http` parameter to the `POST /{id:guid}/transicion` delegate.
- Extract: `var rolClaim = http.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;`.
- If `rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol)` → `return Results.Problem(title: "Invalid or missing role claim.", statusCode: StatusCodes.Status403Forbidden)`.
- Replace old `request.ToCommand(id, ...)` with `request.ToCommand(id, rol)`.
- `TransicionarEstadoPedidoHandler` and its command are UNCHANGED.
- **Satisfies:** AUTH-07.1, AUTH-07.3, AUTH-07.4, AUTH-07.5 (scenarios A–E).

### [x] AJ-18 · Tests · PR2 — `ApiFactory` — `GenerateTestToken` + `CreateAuthenticatedClient` helpers
- Add `public string GenerateTestToken(RolUsuario role)`:
  - Signs with `TestJwtSigningKey`, `HmacSha256`.
  - Issuer: `"GastroGestion"`, Audience: `"GastroGestion"`.
  - Claims: `sub = Guid.NewGuid().ToString()`, `ClaimTypes.Role = role.ToString()`.
  - Expiry: `DateTime.UtcNow.AddHours(8)`.
  - Returns token string via `JwtSecurityTokenHandler`.
- Add `public HttpClient CreateAuthenticatedClient(RolUsuario role = RolUsuario.Administrador)`:
  - Calls `CreateClient()` then sets `DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateTestToken(role))`.
  - Returns the configured client.
- **Satisfies:** AUTH-09.3.

### [x] AJ-19 · Tests · PR2 — Migrate `CatalogueEndpointTests` to authenticated client
- Change `_client = factory.CreateClient();` → `_client = factory.CreateAuthenticatedClient(RolUsuario.Administrador);`.
- No other changes needed in this file.
- **Satisfies:** AUTH-09.4, AUTH-09.5.

### [x] AJ-20 · Tests · PR2 — Migrate `TransactionalEndpointTests` to authenticated client + fix Pedido seam tests
- Line ~35: `_client = factory.CreateClient();` → `_client = factory.CreateAuthenticatedClient(RolUsuario.Administrador);`.
- Line ~176 (Pedido valid-role test): change body from `new TransicionarEstadoRequest(EstadoPedido.Preparandose, RolUsuario.Cajero)` to `new TransicionarEstadoRequest(EstadoPedido.Preparandose)` AND swap client to `factory.CreateAuthenticatedClient(RolUsuario.Cajero)`.
- Line ~194 (Pedido wrong-role 422 test): same body fix + swap client to `factory.CreateAuthenticatedClient(RolUsuario.Cocinero)`.
- **Satisfies:** AUTH-09.4, AUTH-09.5.

### [x] AJ-21 · Tests · PR2 — Verify `SmokeTests` remain anonymous (no change — verification step)
- Confirm `SmokeTests.cs` still uses `factory.CreateClient()` (unauthenticated).
- Tests only `/health` and an unknown route.
- This task is a read-and-confirm, not an edit.
- **Satisfies:** AUTH-06.2, AUTH-06.5-C (health stays open).

### [x] AJ-22 · Tests · PR2 — Full test suite green — `dotnet test` validation
- Run `dotnet test` and confirm:
  - Exit code 0.
  - 160 domain tests pass.
  - 6 application tests pass.
  - 33 infrastructure tests pass.
  - 46 API tests pass (up from 22 Phase-4 tests; new auth tests added).
  - Total: 245 passing, 0 failing.
- No `[AllowAnonymous]` test bypasses remain that would hide lockdown regressions.
- **Satisfies:** AUTH-09.5, AUTH-09.6 (scenario D).

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

## Archived for history

This task file is locked as of 2026-06-16. All tasks are complete. The spec and design documents contain the rationale and architecture; the implementation lives in the merged main branch.

**Final PR lineage:**
- PR #12 (commit 9e4835b): additive auth foundation (AJ-01..AJ-13).
- PR #13 (commit f7724c8): lockdown + test migration (AJ-14..AJ-22).
