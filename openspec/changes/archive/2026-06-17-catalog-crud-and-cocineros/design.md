# Technical Design: catalog-crud-and-cocineros

> **Archive note:** Archived 2026-06-17. See `archive-report.md` for full closure summary.
> Original design observations: proposal (obs #147), spec (obs #149).

Project: `backendservice-gg`. Backend root: `C:\Users\Joaquin\OneDrive\Desktop\Desktop\GastroGestion`.
Reads: proposal (obs #147), spec (obs #149). Requirements: CCC-A01, CCC-B01..B03, CCC-C01..C03.

## 1. Architecture Approach

No new architectural pattern. This change extends the existing **Clean Architecture vertical-slice** convention
already used by `CrearCliente` / `CrearIngrediente`: `Command (record) + Handler (class)` two files per use case,
a thin repository port in `Application/Abstractions/Persistence`, an EF implementation in
`Infrastructure/Persistence/Repositories`, request/response/mapping/validator records in `Contracts`, and a
Minimal-API endpoint group in `Api/Endpoints`. Exceptions flow up to the centralized
`GastroGestionExceptionHandler` (RFC 7807). Role gating is the manual `ClaimTypes.Role`-parse → `Results.Problem(403)`
pattern from `OrdenTrabajoEndpoints.cs`.

Layering and boundaries are unchanged:
- Domain owns invariants (new mutators on `Cliente` / `Ingrediente`). Zero new package refs.
- Application orchestrates (load → mutate / pre-check conflict → `SaveChangesAsync`). Throws app/domain exceptions.
- Infrastructure does EF queries only. Activo filtering and uniqueness pre-check live here (repo methods).
- Api maps HTTP, parses role claim, returns status. No business logic.
- Contracts holds DTOs + extension-method mappings. No EF, no domain leakage beyond entity → response.

No migration. All columns (`Activo`, `Rol`, `NombreCompleto`, `Cuit`, `Email`) already exist.

## 2. Component & Data-Flow Map

### Piece A — Cocineros listing (read-only, no domain change)

```
GET /usuarios/cocineros
  → UsuarioEndpoints (parse ClaimTypes.Role; gate Cocinero|Administrador else 403)
  → GetCocinerosHandler.Handle(GetCocinerosQuery)
  → IUsuarioRepository.GetByRolAsync(RolUsuario.Cocinero)   // EF: Where(Rol==X && Activo)
  → List<Usuario> → .Select(u => u.ToCocineroResponse())   // { id, nombreCompleto }
  → Results.Ok(list)
```

### Piece B / C — Edit (write)

```
PUT /clientes/{id}   (gate Administrador else 403)
  → EditarClienteHandler.Handle(EditarClienteCommand)
       repo.GetByIdAsync(id) ?? throw NotFoundException            → 404
       if Cuit changed: repo.CuitExistsForOtherAsync(cuit,id) → throw ConflictException → 409
       cliente.ActualizarDatos(...)   // DomainException on RI-without-CUIT → 422
       uow.SaveChangesAsync()
  → reload-free: map the same tracked entity → ClienteResponse
  → Results.Ok(response)
```

`PUT /ingredientes/{id}` is identical with `EditarIngredienteHandler` →
`repo.NombreExistsForOtherAsync(nombre,id)` → `ingrediente.ActualizarNombre(nombre)`.

### Soft-delete (write, idempotent)

```
DELETE /clientes/{id} | /ingredientes/{id}   (gate Administrador else 403)
  → DesactivarXHandler.Handle(DesactivarXCommand)
       entity = repo.GetByIdAsync(id) ?? throw NotFoundException   → 404
       entity.Desactivar()        // idempotent; no-op if already inactive
       uow.SaveChangesAsync()
  → Results.NoContent()           → 204
```

### Search/List (read)

```
GET /clientes?nombre=&incluirInactivos=   (authenticated)
  → BuscarClientesHandler.Handle(BuscarClientesQuery(nombre, incluirInactivos))
  → IClienteRepository.SearchAsync(nombre, incluirInactivos)
       EF: query = Clientes; if !incluirInactivos → Where(Activo);
           if nombre != null → Where(EF.Functions.Like(Nombre, $"%{nombre}%"))
  → list.Select(c => c.ToResponse())
  → Results.Ok(list)
```

`GET /ingredientes?...` mirrors with `BuscarIngredientesHandler` + `IIngredienteRepository.SearchAsync`.

## 3. Locked Decisions

### 3.1 Domain mutators

`Cliente.ActualizarDatos(string nombre, CondicionIVA condicionIVA, Cuit? cuit, Email? email) : void`
- Throws `DomainException` on blank nombre and RI-without-CUIT.
- Leaves `NumeroCliente` and `Activo` unchanged.

`Ingrediente.ActualizarNombre(string nombre) : void`
- Throws `DomainException` on blank nombre.
- `UnidadBase` never touched; `EditarIngredienteRequest` structurally omits it.

`Desactivar()` already exists on both (idempotent, sets `Activo = false`) — **reused, not re-added**.

**CCC-C01 UnidadBase decision — LOCKED: IGNORE server-side.** `EditarIngredienteRequest` carries `Nombre` only.

### 3.2 Repository ports + EF implementations

`IClienteRepository` additions:
```csharp
Task<IReadOnlyList<Cliente>> SearchAsync(string? nombre, bool incluirInactivos, CancellationToken ct = default);
Task<bool> CuitExistsForOtherAsync(string cuit, Guid excludeId, CancellationToken ct = default);
```

`IIngredienteRepository` additions:
```csharp
Task<IReadOnlyList<Ingrediente>> SearchAsync(string? nombre, bool incluirInactivos, CancellationToken ct = default);
Task<bool> NombreExistsForOtherAsync(string nombre, Guid excludeId, CancellationToken ct = default);
```

`IUsuarioRepository` addition:
```csharp
Task<IReadOnlyList<Usuario>> GetByRolAsync(RolUsuario rol, CancellationToken ct = default);
```

**`GetAllAsync` is left untouched** (ADR-CCC-3). New GET endpoints call `SearchAsync`.

UoW pattern: handlers depend on `IUnitOfWork _uow` and call `await _uow.SaveChangesAsync(ct)` after mutating the
EF-tracked aggregate returned by `GetByIdAsync`. No explicit `UpdateAsync` needed.

### 3.3 Application use cases

| Use case | Command/Query | Handler returns | Exceptions thrown |
|---|---|---|---|
| EditarCliente | `EditarClienteCommand(Guid Id, string Nombre, CondicionIVA, string? Cuit, string? Email)` | `Cliente` | `NotFoundException`→404, `ConflictException`→409, `DomainException`→422 |
| DesactivarCliente | `DesactivarClienteCommand(Guid Id)` | `Task` | `NotFoundException`→404 |
| BuscarClientes | `BuscarClientesQuery(string? Nombre, bool IncluirInactivos)` | `IReadOnlyList<Cliente>` | — |
| EditarIngrediente | `EditarIngredienteCommand(Guid Id, string Nombre)` | `Ingrediente` | `NotFoundException`→404, `ConflictException`→409, `DomainException`→422 |
| DesactivarIngrediente | `DesactivarIngredienteCommand(Guid Id)` | `Task` | `NotFoundException`→404 |
| BuscarIngredientes | `BuscarIngredientesQuery(string? Nombre, bool IncluirInactivos)` | `IReadOnlyList<Ingrediente>` | — |
| GetCocineros | `GetCocinerosQuery()` | `IReadOnlyList<Usuario>` | — |

### 3.4 Contracts

New records:
- `EditarClienteRequest(string Nombre, CondicionIVA CondicionIVA, string? Cuit, string? Email)`
- `EditarIngredienteRequest(string Nombre)` — UnidadBase intentionally absent
- `CocineroResponse(Guid Id, string NombreCompleto)`

Validators (auto-discovered by `AddValidatorsFromAssemblyContaining<>` scan):
- `EditarClienteValidator` → `RuleFor(x => x.Nombre).NotEmpty()` → 400
- `EditarIngredienteValidator` → `RuleFor(x => x.Nombre).NotEmpty()` → 400

### 3.5 Endpoints

- `ClienteEndpoints.cs`: `MapPut("/{id:guid}", ...)`, `MapDelete("/{id:guid}", ...)`, rewired `MapGet("/", ...)` to `BuscarClientesHandler`.
- `IngredienteEndpoints.cs`: mirror.
- `UsuarioEndpoints.cs` (new): `MapGet("/cocineros", ...)` with `Cocinero|Administrador` gate.
- `Program.cs`: `app.MapUsuarioEndpoints()` added.

## 4. Test Plan — See tasks.md (CCC-T10..T12, T27..T32, T47..T52)

## 5. Chained-PR Split (confirmed)

1. PR A — cocineros (CCC-T01..T12) → merged #19
2. PR B — cliente CRUD (CCC-T13..T32) → merged #20
3. PR C — ingrediente CRUD (CCC-T33..T52) → merged #21

## 6. ADRs

### ADR-CCC-1 — Uniqueness conflict: pre-check → 409
Pre-check `CuitExistsForOtherAsync` / `NombreExistsForOtherAsync` in the handler before `SaveChangesAsync`.
Rejected: catching `DbUpdateException` (couples Application to EF, harder to unit-test).
Accepted tradeoff: TOCTOU race → 500 (acceptable for admin-only single-operator catalog edits).

### ADR-CCC-2 — Soft-delete idempotency via existing `Desactivar()`
DELETE always returns 204 when entity exists. Non-existent id → 404. No "already deleted" 409/410.

### ADR-CCC-3 — `GetAllAsync` left intact; new `SearchAsync` carries default-active behavior
Do NOT add `Where(Activo)` to existing `GetAllAsync`. New list endpoints call `SearchAsync`.
API contract impact: `GET /clientes` and `GET /ingredientes` now hide inactive rows by default.
Since soft-delete is brand new, no inactive rows exist in current data, so existing tests keep passing.

### ADR-CCC-4 — Role gate at the endpoint edge via manual claim parse
Enforce Administrador-only (PUT/DELETE) and Cocinero|Administrador (cocineros GET) by parsing
`ClaimTypes.Role` in the endpoint. NOT via `[Authorize(Roles=...)]`. NOT in the handler.
Verbatim reuse of the `OrdenTrabajoEndpoints` pattern.

## 7. Locked Public Shapes

```
// Domain
Cliente.ActualizarDatos(string nombre, CondicionIVA condicionIVA, Cuit? cuit, Email? email) : void
Ingrediente.ActualizarNombre(string nombre) : void

// HTTP
PUT    /clientes/{id:guid}      Admin → 200 ClienteResponse | 400 | 403 | 404 | 409 | 422
DELETE /clientes/{id:guid}      Admin → 204 | 403 | 404
GET    /clientes?nombre=&incluirInactivos=   auth → 200 [ClienteResponse] | 401
PUT    /ingredientes/{id:guid}  Admin → 200 IngredienteResponse | 400 | 403 | 404 | 409 | 422
DELETE /ingredientes/{id:guid}  Admin → 204 | 403 | 404
GET    /ingredientes?nombre=&incluirInactivos=  auth → 200 [IngredienteResponse] | 401
GET    /usuarios/cocineros      Cocinero|Admin → 200 [CocineroResponse] | 401 | 403
```

Note: 400 from FluentValidation filter (empty Nombre), 422 from DomainException (RI without CUIT),
401 from RequireAuthorization middleware (unauthenticated), 403 from role gate (authenticated, wrong role).
