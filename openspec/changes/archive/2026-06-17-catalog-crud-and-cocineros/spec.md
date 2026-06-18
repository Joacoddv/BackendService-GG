# Specification: catalog-crud-and-cocineros

## Purpose

Adds three capabilities to the .NET 8 backend: cocineros listing (Piece A), full
Cliente CRUD with soft-delete (Piece B), and Ingrediente CRUD with soft-delete and
locked UnidadBase (Piece C). No database migration required.

---

## Piece A — Cocineros Listing

### Requirement: CCC-A01 — List Active Cocineros

The system MUST expose `GET /usuarios/cocineros` returning active users whose role is
`Cocinero`. The response MUST include each user's `id` and display name. Access is
restricted to callers whose role is `Cocinero` or `Administrador`; authenticated callers
with any other role MUST receive `403`. Unauthenticated callers receive `401` from the
ASP.NET Core auth middleware (not 403 — see spec correction note below).

> **Spec correction (applied at archive 2026-06-17):** The original spec text said
> "unauthenticated → 403". The verified implementation correctly returns 401 from
> `.RequireAuthorization()` middleware. 403 is reserved for authenticated callers whose
> role is missing or insufficient. This is the authoritative behavior.

#### Scenario: Admin retrieves cocinero list

- GIVEN at least one active `Usuario` with `Rol == Cocinero` exists
- WHEN `GET /usuarios/cocineros` is called with an `Administrador` token
- THEN response is `200 OK` with an array of `{ id, nombreCompleto }` for each active cocinero

#### Scenario: Cocinero retrieves cocinero list

- GIVEN a valid `Cocinero` token
- WHEN `GET /usuarios/cocineros` is called
- THEN response is `200 OK` with the same payload

#### Scenario: Mozo or Cajero is rejected

- GIVEN a valid token with role `Mozo` or `Cajero`
- WHEN `GET /usuarios/cocineros` is called
- THEN response is `403 Forbidden` with ProblemDetails body

#### Scenario: Unauthenticated caller

- GIVEN no Authorization header
- WHEN `GET /usuarios/cocineros` is called
- THEN response is `401 Unauthorized` (ASP.NET auth middleware)

#### Scenario: Inactive cocineros excluded

- GIVEN a `Usuario` with `Rol == Cocinero` and `Activo == false`
- WHEN `GET /usuarios/cocineros` is called with an `Administrador` token
- THEN that user does NOT appear in the response array

---

## Piece B — Cliente CRUD

### Requirement: CCC-B01 — Edit Cliente

The system MUST allow an `Administrador` to update a cliente's `Nombre`, `Email`,
`Cuit`, and `CondicionIVA` via `PUT /clientes/{id}`. `NumeroCliente` MUST NOT change
regardless of request content. A `Cuit` that conflicts with another cliente MUST
produce `409`. Empty `Nombre` field MUST produce `400` (FluentValidation endpoint filter,
fires before the handler). Domain-rule violations (e.g., `ResponsableInscripto` without
`Cuit`) MUST produce `422`. Non-admin callers MUST receive `403`.

> **Spec correction (applied at archive 2026-06-17):** Original spec said invalid domain
> state → 422 and implied empty Nombre → 422 too. Empty-field validation returns 400
> (FluentValidation filter); 422 is reserved for DomainException (e.g., RI without CUIT).

#### Scenario: Admin edits valid cliente

- GIVEN a cliente with `id=5` exists and the supplied `Cuit` is unique
- WHEN `PUT /clientes/5` is called with an `Administrador` token and valid body
- THEN response is `200 OK` with updated resource; `NumeroCliente` is unchanged

#### Scenario: Cliente not found

- GIVEN no cliente with `id=99` exists
- WHEN `PUT /clientes/99` is called with an `Administrador` token
- THEN response is `404 Not Found`

#### Scenario: Cuit conflict with another cliente

- GIVEN a different cliente already holds the supplied `Cuit`
- WHEN `PUT /clientes/5` is called with that `Cuit`
- THEN response is `409 Conflict`

#### Scenario: Invalid domain state (e.g. ResponsableInscripto without Cuit)

- GIVEN the request sets `CondicionIVA = ResponsableInscripto` with no `Cuit`
- WHEN `PUT /clientes/5` is called
- THEN response is `422 Unprocessable Entity` (DomainException)

#### Scenario: Empty Nombre field

- GIVEN the request body contains an empty string for `nombre`
- WHEN `PUT /clientes/5` is called
- THEN response is `400 Bad Request` (FluentValidation endpoint filter)

#### Scenario: Non-admin caller rejected

- GIVEN a valid token with role `Mozo`, `Cajero`, or `Cocinero`
- WHEN `PUT /clientes/5` is called
- THEN response is `403 Forbidden`

---

### Requirement: CCC-B02 — Soft-Delete Cliente

The system MUST soft-delete a cliente via `DELETE /clientes/{id}`, setting
`Activo = false` and returning `204`. The operation MUST be idempotent (calling it
again on an already-inactive cliente MUST also return `204`). Non-admin callers MUST
receive `403`. A non-existent id MUST return `404`. After deletion the cliente MUST be
hidden from default list results.

#### Scenario: Admin soft-deletes active cliente

- GIVEN a cliente with `id=5` and `Activo == true`
- WHEN `DELETE /clientes/5` is called with an `Administrador` token
- THEN response is `204 No Content` and `Activo` is `false`

#### Scenario: Idempotent — already inactive

- GIVEN the same cliente is already inactive
- WHEN `DELETE /clientes/5` is called again
- THEN response is `204 No Content` (no error)

#### Scenario: Not found

- GIVEN no cliente with `id=99` exists
- WHEN `DELETE /clientes/99` is called with an `Administrador` token
- THEN response is `404 Not Found`

#### Scenario: Non-admin rejected

- GIVEN a token with role `Mozo`
- WHEN `DELETE /clientes/5` is called
- THEN response is `403 Forbidden`

#### Scenario: Inactive cliente hidden from default list

- GIVEN a cliente with `Activo == false`
- WHEN `GET /clientes` is called without `?incluirInactivos=true`
- THEN that cliente does NOT appear in the response

---

### Requirement: CCC-B03 — Search/List Clientes

The system MUST support `GET /clientes?nombre=&incluirInactivos=`. By default only
active clientes are returned. When `?incluirInactivos=true` is supplied, inactive
clientes are also returned. The `nombre` parameter MUST apply a case-insensitive
partial match. The endpoint requires authentication.

#### Scenario: Default list excludes inactive

- GIVEN active and inactive clientes exist
- WHEN `GET /clientes` is called (no query params) with any authenticated token
- THEN only active clientes are returned

#### Scenario: incluirInactivos=true shows all

- GIVEN an `Administrador` token and `?incluirInactivos=true`
- WHEN `GET /clientes?incluirInactivos=true` is called
- THEN both active and inactive clientes are returned

#### Scenario: nombre filter applied

- GIVEN clientes named "García SA" and "López SRL" exist (both active)
- WHEN `GET /clientes?nombre=garc` is called
- THEN only "García SA" is returned

#### Scenario: Unauthenticated request rejected

- GIVEN no Authorization header
- WHEN `GET /clientes` is called
- THEN response is `401 Unauthorized`

---

## Piece C — Ingrediente CRUD

### Requirement: CCC-C01 — Edit Ingrediente (Nombre Only)

The system MUST allow an `Administrador` to update an ingrediente's `Nombre` via
`PUT /ingredientes/{id}`. `UnidadBase` MUST NOT be changed — `EditarIngredienteRequest`
structurally omits `UnidadBase` (immutability by contract, not runtime rejection).
A `Nombre` conflict with another ingrediente MUST produce `409`. Empty `Nombre` MUST
produce `400` (FluentValidation filter). Non-admin callers MUST receive `403`.

> **Spec correction (applied at archive 2026-06-17):** Original spec said empty/blank
> Nombre → 422. The verified behavior is 400 (FluentValidation endpoint filter fires before
> the handler). 422 is reserved for DomainException violations.

#### Scenario: Admin edits Nombre

- GIVEN an ingrediente with `id=3` exists and new `Nombre` is unique
- WHEN `PUT /ingredientes/3` is called with an `Administrador` token and `{ "nombre": "Harina 0000" }`
- THEN response is `200 OK`; `UnidadBase` is unchanged (confirmed via GET after PUT)

#### Scenario: UnidadBase is structurally absent from request

- The `EditarIngredienteRequest` DTO contains only `Nombre`. There is no `UnidadBase`
  field to send; the value is immutable by contract.

#### Scenario: Ingrediente not found

- GIVEN no ingrediente with `id=99` exists
- WHEN `PUT /ingredientes/99` is called with an `Administrador` token
- THEN response is `404 Not Found`

#### Scenario: Nombre conflict

- GIVEN another ingrediente already has the proposed `Nombre`
- WHEN `PUT /ingredientes/3` is called
- THEN response is `409 Conflict`

#### Scenario: Empty Nombre

- GIVEN the request body contains an empty string for `nombre`
- WHEN `PUT /ingredientes/3` is called
- THEN response is `400 Bad Request` (FluentValidation endpoint filter)

#### Scenario: Non-admin rejected

- GIVEN a token with role `Cocinero`
- WHEN `PUT /ingredientes/3` is called
- THEN response is `403 Forbidden`

---

### Requirement: CCC-C02 — Soft-Delete Ingrediente

The system MUST soft-delete an ingrediente via `DELETE /ingredientes/{id}`, returning
`204`. The operation MUST be idempotent. Non-admin callers MUST receive `403`. A
non-existent id MUST return `404`. After deletion the ingrediente MUST be hidden from
default list results.

#### Scenario: Admin soft-deletes active ingrediente

- GIVEN an ingrediente with `id=3` and `Activo == true`
- WHEN `DELETE /ingredientes/3` is called with an `Administrador` token
- THEN response is `204 No Content` and `Activo` is `false`

#### Scenario: Idempotent — already inactive

- GIVEN the same ingrediente is already inactive
- WHEN `DELETE /ingredientes/3` is called again
- THEN response is `204 No Content`

#### Scenario: Not found

- GIVEN no ingrediente with `id=99` exists
- WHEN `DELETE /ingredientes/99` is called
- THEN response is `404 Not Found`

#### Scenario: Non-admin rejected

- GIVEN a token with role `Mozo`
- WHEN `DELETE /ingredientes/3` is called
- THEN response is `403 Forbidden`

#### Scenario: Inactive ingrediente hidden from default list

- GIVEN an ingrediente with `Activo == false`
- WHEN `GET /ingredientes` is called without `?incluirInactivos=true`
- THEN that ingrediente does NOT appear in the response

---

### Requirement: CCC-C03 — Search/List Ingredientes

The system MUST support `GET /ingredientes?nombre=&incluirInactivos=` with the same
behavior as CCC-B03 applied to ingredientes. Authentication is required.

#### Scenario: Default list excludes inactive

- GIVEN active and inactive ingredientes exist
- WHEN `GET /ingredientes` is called without query params
- THEN only active ingredientes are returned

#### Scenario: incluirInactivos=true shows all

- GIVEN `?incluirInactivos=true`
- WHEN `GET /ingredientes?incluirInactivos=true` is called with an `Administrador` token
- THEN active and inactive ingredientes are returned

#### Scenario: nombre filter applied

- GIVEN ingredientes named "Harina 0000" and "Azúcar" exist
- WHEN `GET /ingredientes?nombre=har` is called
- THEN only "Harina 0000" is returned

---

## Verification Approach

| Layer | What to test |
|---|---|
| Domain.Tests | `Cliente.ActualizarDatos` invariants; `Ingrediente.ActualizarNombre` rejects empty + no UnidadBase change; `Desactivar` idempotency on both entities |
| Application.Tests | Each handler: happy path, NotFound, Conflict, Forbidden, Validation; `GetCocinerosByRolHandler` filters by role+active |
| Api.Tests | Role-gate HTTP integration tests for every endpoint (correct status codes per role); `?incluirInactivos` toggle; `?nombre` partial filter |
| Infrastructure.Tests | `GetByRolAsync` returns only active cocineros; `UpdateCliente`/`UpdateIngrediente` persist changes; unique-index conflict surfaced as `ConflictException` |

All tests MUST pass as part of the same PR that introduces each piece. The existing
~302-test suite MUST remain green.

---

## Archive Note

Archived 2026-06-17. All 3 PRs merged to main. 413 tests green. Two spec corrections
applied: (1) unauthenticated → 401 (not 403); (2) empty-field validation → 400 (not 422).
See `archive-report.md` for full detail.
