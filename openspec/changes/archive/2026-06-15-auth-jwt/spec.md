# Delta Specification: auth-jwt (Phase 5 of 7)

**Last updated:** 2026-06-15 (ARCHIVED)
**Phase:** 5 of 7 in the .NET 8 strangler roadmap
**Status:** IMPLEMENTED AND MERGED — all 22 tasks (AJ-01..AJ-22) complete; both PRs merged to main.

---

## Scope Summary

This delta spec defined what MUST be true after the `auth-jwt` change is applied. It covers nine numbered requirement areas: the `Usuario` domain aggregate, password hashing infrastructure, the login use case, JWT token issuance, the login endpoint, endpoint protection, role extraction from JWT claim, initial admin seeding, and cross-cutting round-trip validation.

**Implementation Status**: All requirements from AUTH-01 to AUTH-09 have been implemented and verified. See `openspec/specs/Api/spec.md` (updated 2026-06-15) for the merged, authoritative specification.

---

## Key Requirements (Summary)

### AUTH-01 — `Usuario` Aggregate (Domain Layer)
**Status: IMPLEMENTED**
- Mirrors `Cliente` pattern with private setters, EF private ctor, static `Crear` factory.
- Properties: `Email`, `NombreCompleto`, `Rol` (`RolUsuario`), `PasswordHash` (string), `Activo` (bool).
- Invariant validation: email non-empty with `@`, `nombreCompleto` non-empty, `passwordHash` non-empty.
- Zero framework dependencies in Domain.

### AUTH-02 — Password Hashing Port and Infrastructure (Application + Infrastructure Layers)
**Status: IMPLEMENTED**
- `IPasswordHasher` port in `Application/Abstractions/Security/`.
- `PasswordHasherAdapter` in `Infrastructure`, wrapping `Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>` (PBKDF2-SHA256).
- Single Base64 column in database.

### AUTH-03 — Login Use Case (Application Layer)
**Status: IMPLEMENTED**
- `IUsuarioRepository` with `GetByEmailAsync`, `AnyAsync`, `AddAsync`.
- `LoginHandler` with constructor-injected repository, hasher, token issuer.
- Generic auth-failure result on any credential failure (user not found, inactive, wrong password).

### AUTH-04 — JWT Token Issuance (Infrastructure Layer)
**Status: IMPLEMENTED**
- `JwtTokenIssuer` using `JwtSecurityTokenHandler`.
- Claims: `sub` (user id), `email`, `ClaimTypes.Role` (enum name), `iss`, `aud`, `exp(+8h)`.
- Matches existing `TokenValidationParameters` configuration.

### AUTH-05 — Login Endpoint (Api Layer)
**Status: IMPLEMENTED**
- `POST /auth/login` anonymous endpoint.
- Returns 200 with `LoginResponse` on valid credentials.
- Returns 401 on invalid credentials (generic message).
- Applies `WithValidation<LoginRequest>()` filter.

### AUTH-06 — Endpoint Protection (Api Layer)
**Status: IMPLEMENTED**
- Group-level `.RequireAuthorization()` on all 8 endpoint groups.
- Replaces inline `[AllowAnonymous]` attributes on ~25 endpoints.
- `/health` and `/auth/login` remain anonymous.

### AUTH-07 — Role Extraction from JWT Claim — Pedido State Transition (Api Layer)
**Status: IMPLEMENTED**
- Endpoint reads `ClaimTypes.Role` from `HttpContext.User`.
- Parses to `RolUsuario` via `Enum.TryParse<RolUsuario>`.
- Returns **403 Forbidden** on missing/unparseable claim (adjudicated from initial spec 401).
- `TransicionarEstadoRequest` drops `Rol` field.
- Handler and domain method unchanged.

### AUTH-08 — Initial Admin Seeding (Infrastructure + Api Layers)
**Status: IMPLEMENTED**
- Seeded in `DevDataSeeder` when `Usuarios` table is empty.
- Credentials from `appsettings.Development.json` (`Seed:AdminEmail`, `Seed:AdminPassword`).
- Password hashed via `IPasswordHasher` before storage.
- Idempotent guard via `Usuarios.AnyAsync()`.

### AUTH-09 — Cross-Cutting: Round-Trip Validation (Api Layer / Infrastructure Layer)
**Status: IMPLEMENTED**
- Existing `TokenValidationParameters` unchanged.
- No `Microsoft.AspNetCore.Identity` in Domain or Application projects.
- `ApiFactory.GenerateTestToken(RolUsuario)` and `CreateAuthenticatedClient(RolUsuario)` helpers.
- All 222 Phase-4 integration tests migrated to authenticated clients.
- Total 245 tests passing (160 domain + 6 app + 33 infra + 46 api).

---

## Implementation Notes

### AUTH-07.3 Adjudication (Resolved)

**Original spec**: missing/unparseable role claim → 401 Unauthorized.
**Design recommendation**: missing/unparseable role claim → 403 Forbidden.
**RFC 7231/7235 reasoning**: Token has already passed `[Authorize]` validation (signature, lifetime, issuer, audience all valid) — user IS authenticated. A missing/corrupt role claim in an otherwise-valid token is an authorization failure (3xx), not an authentication failure (4xx).

**Resolution**: Updated to 403. Code already returns 403. Spec now aligns.

### Test Coverage Gaps (Deferred to Phase 6)

No integration tests exist for:
- `POST /auth/login` HTTP round-trip (scenarios AUTH-05.6-A to AUTH-05.6-E).
- AUTH-07-B (missing role claim → 403) and AUTH-07-C (unparseable role → 403) at the HTTP layer.

These gaps are noted for Phase 6; the implementation is correct and verified at the unit level.

---

## For the Archive

This spec is locked as of 2026-06-15. Future changes to authentication/authorization behavior belong in Phase 6 or later changes, documented in the main `openspec/specs/Api/spec.md` (which now includes AUTH-01..AUTH-09 as merged requirements).

Non-goals (explicitly out of Phase 5 scope, recorded for Phase 6):
1. Register / user CRUD endpoints
2. Refresh tokens and token revocation
3. Second catalog / `GastroGestion_Seguridad` DbContext
4. Legacy data migration
5. Granular per-endpoint role policies (beyond Pedido)
