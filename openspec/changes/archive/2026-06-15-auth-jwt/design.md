# Design: auth-jwt (Phase 5 of 7)

Phase 5 wires the issuing half of an already-validating JWT pipeline. This design locks the `Usuario` aggregate, two Application ports (password hasher + token issuer), one login use case, the Infrastructure adapters, the EF mapping into the existing single `GastroGestionDbContext`, the `POST /auth/login` endpoint, the `[AllowAnonymous]` → `[Authorize]` rollout, the role-from-claim extraction on the Pedido transition path, the admin seeder, and the lowest-churn test migration.

## Architecture Decision Records

| # | Decision | Rationale | Rejected alternative |
|---|----------|-----------|----------------------|
| ADR-1 | Hand-rolled `Usuario : AggregateRoot`, not ASP.NET Core Identity | Mirrors `Cliente` exactly, keeps Domain zero-dependency, stores `RolUsuario` directly in one table | Identity couples Domain to `IdentityUser`, needs ~6 tables, does not map to 4-value enum |
| ADR-2 | `PasswordHash` opaque `string`; hashing/verification in Infrastructure behind a port | Preserves the zero-dependency rule — no `Microsoft.AspNetCore.Identity` namespace in Domain | Hashing inside `Usuario.Crear` would drag ASP.NET Core into Domain |
| ADR-3 | Two Application ports under `Abstractions/Security/`: `IPasswordHasher` and `ITokenIssuer` | Segments by concern; `Abstractions/` already uses this structure | Mixing in `Persistence/` breaks separation |
| ADR-4 | Token issuer uses `JwtSecurityTokenHandler`, reading the SAME `Jwt:Issuer`/`Jwt:Audience`/`Jwt:SigningKey` keys | Matches the validation side (Program.cs:59-62); avoids subtle claim-parsing drift | `JsonWebTokenHandler` parses claims differently |
| ADR-5 | 8-hour token expiry, access-token only | One restaurant shift; no refresh in Phase 5 | Refresh tokens add a table + 2 endpoints for zero benefit |
| ADR-6 | `Usuarios` table in existing `GastroGestionDbContext` (single catalog) | No live legacy users; `GastroGestion_Seguridad` does not exist in .NET 8 appsettings. Second context = second connection string + factory + migrations folder | Second `GastroGestionSeguridadDbContext` is premature |
| ADR-7 | Group-level `.RequireAuthorization()` on each `MapGroup`, NOT per-endpoint `[Authorize]` | 8 calls replace 25 attributes; one call per group is harder to omit on new endpoints | Per-endpoint `[Authorize]` repeats ~25 times |
| ADR-8 | Login does NOT call `IUnitOfWork.SaveChangesAsync` | Login is read + verify + issue; it persists nothing | Dead weight; use case pattern injects `IUnitOfWork` only on writes |
| ADR-9 | Extend `DevDataSeeder` with admin seed, guarded by `Usuarios.AnyAsync` | `DevDataSeeder` already runs post-migrate, idempotent via `Clientes.AnyAsync` | Dedicated seeder duplicates DI boilerplate |
| ADR-10 | Test migration via `CreateAuthenticatedClient(RolUsuario)` factory helper, swapped into 3 test constructors | 3 files call `factory.CreateClient()`; swapping one line per class authenticates every test at once | Editing 222 call sites for same effect |

## Sequence diagram — login → token → authenticated request → role-from-claim

```mermaid
sequenceDiagram
    actor Client
    participant API as AuthEndpoints (POST /auth/login)
    participant LH as LoginHandler (Application)
    participant Repo as IUsuarioRepository
    participant Hasher as IPasswordHasher (Infra)
    participant Issuer as ITokenIssuer (Infra)
    participant Pipe as JwtBearer pipeline (Program.cs)
    participant PE as PedidoEndpoints (transicion)

    Note over Client,Issuer: 1. Login (anonymous)
    Client->>API: POST /auth/login { email, password }
    API->>LH: Handle(LoginCommand, ct)
    LH->>Repo: GetByEmailAsync(email)
    Repo-->>LH: Usuario? (with PasswordHash)
    alt user null OR not Activo
        LH-->>API: AuthenticationFailedException
        API-->>Client: 401 Unauthorized
    else found and active
        LH->>Hasher: Verify(usuario, usuario.PasswordHash, password)
        alt verify fails
            Hasher-->>LH: Failed
            LH-->>API: AuthenticationFailedException
            API-->>Client: 401 Unauthorized
        else verify succeeds
            LH->>Issuer: Issue(usuario)
            Note right of Issuer: claims: sub=Id, email,<br/>ClaimTypes.Role=Rol.ToString(),<br/>iss/aud/exp(+8h)
            Issuer-->>LH: (token, expiresAtUtc)
            LH-->>API: LoginResult
            API-->>Client: 200 { accessToken, expiresAtUtc, usuarioId, rol }
        end
    end

    Note over Client,PE: 2. Authenticated request — role from claim
    Client->>PE: POST /pedidos/{id}/transicion  (Authorization: Bearer token)
    PE->>Pipe: validate token (TokenValidationParameters)
    Pipe-->>PE: ClaimsPrincipal (authenticated)
    Note right of PE: [Authorize] via group.RequireAuthorization()<br/>passes
    PE->>PE: rolClaim = User.FindFirst(ClaimTypes.Role)?.Value
    alt claim missing OR not a RolUsuario
        PE-->>Client: 403 Forbidden
    else parses
        PE->>PE: rol = Enum.Parse<RolUsuario>(rolClaim)
        PE->>LH2 as TransicionarEstadoPedidoHandler: Handle(ToCommand(id, rol), ct)
        Note right of PE: handler + domain method UNCHANGED;<br/>only cmd.Rol source changed
    end
```

## Locked signatures and key implementation notes

- **`Usuario` aggregate**: mirrors `Cliente` exactly — `AggregateRoot` base, private setters, EF private ctor, static `Crear` factory with `DomainException` on validation failure.
- **`IPasswordHasher` port**: in `Application/Abstractions/Security/`; no ASP.NET Core namespace in Domain.
- **`ITokenIssuer` port**: returns `AccessToken` record with token value + expiry.
- **`LoginHandler`**: injects repository + hasher + issuer only (no `IUnitOfWork`); throws `AuthenticationFailedException` on failure.
- **`JwtTokenIssuer`**: uses `JwtSecurityTokenHandler` with test-compatible configuration (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey`); claims: `sub` (user id), `email`, `ClaimTypes.Role` (enum name), `iss`, `aud`, `exp(+8h)`.
- **`UsuarioConfiguration`**: unique email index, `Rol` as int via `HasConversion<int>()`, `PasswordHash` required, single `Usuarios` DbSet on existing `GastroGestionDbContext`.
- **Admin seeder**: extended `DevDataSeeder` with `Usuarios.AnyAsync` guard (idempotent), credentials from config with fallback.
- **`[AllowAnonymous]` → `[Authorize]`**: group-level `.RequireAuthorization()` on each of 8 endpoint groups; `/health` and `/auth/login` stay anonymous.
- **Pedido transition role extraction**: reads `ClaimTypes.Role`, parses to `RolUsuario`, returns 403 on missing/invalid claim; handler + domain method unchanged.
- **`TransicionarEstadoRequest`**: drops `Rol` field; `ToCommand` takes role as parameter.
- **Test helpers**: `GenerateTestToken(RolUsuario)` and `CreateAuthenticatedClient(RolUsuario)` on `ApiFactory`.

## Minimal changes checklist

- [ ] `Usuario` aggregate + `Crear` factory with invariant validation.
- [ ] `IPasswordHasher` + `ITokenIssuer` in `Application/Abstractions/Security/`.
- [ ] `LoginHandler` + `AuthenticationFailedException`.
- [ ] `JwtTokenIssuer` implementation using test-compatible signing.
- [ ] `UsuarioRepository` + `UsuarioConfiguration`.
- [ ] `AddUsuarios` EF migration.
- [ ] Admin seeder in `DevDataSeeder`.
- [ ] Group-level `.RequireAuthorization()` on all 8 endpoint groups.
- [ ] Pedido transition role-from-claim extraction (403 on missing/invalid).
- [ ] `GenerateTestToken` + `CreateAuthenticatedClient` on `ApiFactory`.
- [ ] Test migration: 3 constructor lines + 2 seam-test edits.
