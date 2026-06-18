# Proposal: Catalog CRUD and Cocineros Listing

## Intent

Unblock the frontend's remaining work — the asignar-cocinero picker (Slice C2) and full Cliente/Ingrediente catalog management. Today the backend exposes only Create + List + GetById for Cliente and Ingrediente, and has no way to list available cocineros. The frontend cannot edit/deactivate catalog records nor populate the cocinero picker. This change closes those gaps using existing domain capabilities (`Desactivar()` already modeled; all DB columns exist — **no migration needed**).

## Verified Domain Facts

- `Cliente.Desactivar()` / `Ingrediente.Desactivar()` exist and are idempotent soft-deletes.
- `Cliente`: `Nombre`, `CondicionIVA`, `Cuit?`, `Email?` editable; `NumeroCliente` immutable. Cuit unique index is infra-enforced.
- `Ingrediente`: `Nombre`, `UnidadBase`. Name uniqueness is infra-enforced.
- `Usuario`: `Rol` (RolUsuario), `NombreCompleto`, `Activo`. No separate Cocinero entity.
- Repos expose `GetByIdAsync`/`AddAsync`/`GetAllAsync` (Cliente/Ingrediente); `IUsuarioRepository` has no role query yet.
- Role gating is manual `ClaimTypes.Role` parsing → 403 ProblemDetails (per OrdenTrabajoEndpoints).
- Exceptions are centrally mapped: `NotFound`→404, `Conflict`→409, `Forbidden`→403, `Validation`/`Domain`→422.

## Locked Decisions (do not re-litigate)

- Soft-delete only. Hard delete BLOCKED at API. DELETE → 204 (soft).
- Lists hide inactive by default (`Where(Activo)`); optional `?incluirInactivos=true` for admins.
- `Ingrediente.UnidadBase` NOT editable — edit allows `Nombre` only (protects recipe quantities). To change unit: desactivar + crear nuevo.
- `Cliente.CondicionIVA` editable (affects future facturas only); `Nombre`/`Email`/`Cuit` editable (Cuit edit pre-checks unique conflict); `NumeroCliente` never editable.
- Edit/delete (PUT/DELETE) gated to Administrador only. Create/list stay authenticated. List-cocineros gated to Cocinero+Administrador.
- Cocineros = `Usuario` where `Rol==Cocinero && Activo`. LegajoId == `Usuario.Id`. Picker returns id + display name only.
- Every endpoint/use case ships WITH its tests (Standard Mode, tests-alongside — existing ~302-test suite across Domain/Application/Api/Infrastructure.Tests).

## Scope

### In Scope — three cohesive pieces (one chained PR per entity)

**Piece A — List Cocineros** (no domain change, no migration)
- `IUsuarioRepository.GetByRolAsync(RolUsuario, ...)` + impl; query + handler; contract DTO (id + NombreCompleto).
- `GET /usuarios/cocineros` — Cocinero/Administrador → 200; bad/missing role → 403.

**Piece B — Cliente CRUD**
- Domain: `Cliente.ActualizarDatos(nombre, condicionIVA, cuit, email)` (re-validates RI invariant).
- Use cases: `EditarCliente`, `DesactivarCliente`, `BuscarClientes`; repo `UpdateAsync`/search support.
- `PUT /clientes/{id}` — Admin → 200; not found → 404; Cuit conflict → 409; invalid → 422; non-admin → 403.
- `DELETE /clientes/{id}` — Admin, soft → 204; not found → 404; non-admin → 403.
- `GET /clientes?nombre=&incluirInactivos=` — name filter; hides inactive by default.

**Piece C — Ingrediente CRUD**
- Domain: `Ingrediente.ActualizarNombre(nombre)` (Nombre only — UnidadBase locked).
- Use cases: `EditarIngrediente`, `DesactivarIngrediente`, `BuscarIngredientes`; repo update/search support.
- `PUT /ingredientes/{id}` — Admin, Nombre only → 200; not found → 404; name conflict → 409; invalid → 422; non-admin → 403.
- `DELETE /ingredientes/{id}` — Admin, soft → 204; not found → 404; non-admin → 403.
- `GET /ingredientes?nombre=&incluirInactivos=` — name filter; hides inactive by default.

### Out of Scope (Non-Goals)

- Plato/Menu/Pedido/Mesa CRUD (next wave).
- Pagination / sorting (filter-only `?nombre=`).
- Hard delete (blocked) and `UnidadBase` editing (blocked).
- Frontend work (asignar-cocinero UI + catalog screens — consume these endpoints in the next wave).
- New auth/role infrastructure — reuse existing manual `ClaimTypes.Role` gating.

## Approach

Mirror the existing `CrearCliente` vertical-slice pattern (Command + Handler + repo port + endpoint + Contracts mapping/validator). Expose existing `Desactivar()` via DELETE→204. Add narrow domain mutators (`ActualizarDatos`, `ActualizarNombre`) that re-run creation invariants. Add `Where(Activo)` to GetAll and a `?nombre=` filter at the repo layer. For Cocineros, add `GetByRolAsync` and a thin read DTO. Unique-conflict pre-checks (Cuit, Ingrediente.Nombre) throw `ConflictException`→409 before persistence. Role gating reuses the manual claim-parse→403 pattern from `OrdenTrabajoEndpoints`.

## Capabilities

### New Capabilities
- `cocineros-listing`: query active users with Cocinero role for the assignment picker.

### Modified Capabilities
- `cliente-management`: add edit, soft-delete, name search; default-active listing.
- `ingrediente-management`: add edit (Nombre only), soft-delete, name search; default-active listing.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Domain/Clientes/Cliente.cs` | Modified | Add `ActualizarDatos(...)` |
| `Domain/Ingredientes/Ingrediente.cs` | Modified | Add `ActualizarNombre(...)` |
| `Application/Abstractions/Persistence/I{Cliente,Ingrediente,Usuario}Repository.cs` | Modified | Update/search/`GetByRolAsync` |
| `Application/{Clientes,Ingredientes,Usuarios}/...` | New | Editar/Desactivar/Buscar + cocineros query+handler |
| `Infrastructure/.../Repositories/*` | Modified | Impl update/filter/`Where(Activo)`/role query |
| `Api/Endpoints/{Cliente,Ingrediente}Endpoints.cs`, new `UsuarioEndpoints.cs` | New/Modified | PUT/DELETE/GET + cocineros GET, role gates |
| `Contracts/{Clientes,Ingredientes,Usuarios}/*` | New/Modified | Edit requests, validators, cocinero DTO |
| `tests/*` (Domain/Application/Api/Infrastructure.Tests) | New | Tests alongside every use case/endpoint |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Deleting referenced Cliente/Ingrediente breaks recipes/facturas | Med | Soft-delete only preserves RI; rows remain |
| Cuit / Ingrediente.Nombre unique-index violation on edit | Med | Pre-check conflict → `ConflictException`→409 before save |
| Role-gate drift from established pattern | Low | Reuse manual `ClaimTypes.Role`→403 pattern verbatim |
| Editing CondicionIVA retroactively alters past facturas | Low | Past facturas snapshotted; edit affects future only |

## Rollback Plan

Pure-additive, no migration. Revert per-piece PR. Reverting removes endpoints/use cases/domain mutators; existing Create/List/GetById untouched. No data shape change to roll back.

## Dependencies

- None external. No migration. All columns exist.

## Delivery Plan — Chained PRs (one per entity)

1. **PR A — cocineros** (smallest, fully independent): repo query + handler + `GET /usuarios/cocineros` + DTO + tests.
2. **PR B — cliente CRUD**: domain + use cases + endpoints + contracts + tests.
3. **PR C — ingrediente CRUD**: domain + use cases + endpoints + contracts + tests.

Each PR is independently shippable, ships with its tests, and stays within review budget. Chain strategy to be confirmed at tasks phase.

## Success Criteria

- [x] `GET /usuarios/cocineros` returns active Cocinero users (id + name); 403 for other roles.
- [x] `PUT /clientes/{id}` (Admin) edits Nombre/Email/Cuit/CondicionIVA; Cuit conflict→409; non-admin→403; NumeroCliente unchanged.
- [x] `PUT /ingredientes/{id}` (Admin) edits Nombre only; UnidadBase unchanged; name conflict→409.
- [x] `DELETE /clientes/{id}` and `DELETE /ingredientes/{id}` (Admin) soft-delete → 204; idempotent.
- [x] List endpoints hide inactive by default; `?nombre=` filters; `?incluirInactivos=true` (Admin) includes inactive.
- [x] Every new use case and endpoint ships with passing tests; full suite green (413 tests).
