# Web API Specification — GastroGestion

**Last updated:** 2026-06-14  
**Phase:** 4 of 7 in the .NET 8 strangler roadmap  
**Scope:** GastroGestion Web API layer — .NET 8 Minimal API endpoints, request/response contracts, middleware, authentication shape.

---

## Overview

The Web API layer exposes the persisted Phase-3 domain and completed application use cases over HTTP via ASP.NET Core Minimal APIs. The contract is REST + RFC 7807 problem details; all endpoints operate on DTOs (never domain aggregates); authentication is wired (all Phase-4 endpoints are `[AllowAnonymous]` pending Phase-5 login). Error handling is centralized via `IExceptionHandler` + `AddProblemDetails()`.

**Status:** Phase 4 complete — 3 slices (Foundation, Catalogue, Transactional+Fiscal+Stock) delivered via PRs #9, #10, #11 to main. All 30 tasks (WA-01..WA-30) complete. Test suite: 222 tests green. Verification: 0 CRITICAL, 3 WARNINGS, 2 SUGGESTIONS (all documented as acceptable deviations). Phase-5 follow-ups captured below.

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

## Known Open Items / Phase-5 Follow-ups (CARRY FORWARD — DO NOT DROP)

### 1. RolUsuario from request body (TEMPORARY SECURITY SEAM)

**Issue:** Pedido state-transition endpoints accept `RolUsuario` from the request body instead of a JWT claim. There are 3 `// PHASE-5` markers in code:
- `PedidoEndpoints.cs:61` (endpoint delegate)
- `TransicionarEstadoPedidoHandler.cs:22` (handler)
- `PedidoRequests.cs:26` (request DTO field)

All endpoints are currently `[AllowAnonymous]`.

**Phase 5 remediation (mandatory):** Add a Usuario aggregate + login/token endpoint, remove `[AllowAnonymous]` from all endpoints, and replace body `RolUsuario` with JWT claim extraction. **Security risk**: accepting role from the body is an authorization bypass until Phase 5 resolves this.

---

### 2. ConfirmarPrecioLinea returns 204 (decision)

**Issue:** Spec scenario 15-C originally said "200 with price body." Implementation returns 204 No Content (command pattern).

**Canonical decision: 204 is correct.** Price is readable via separate GET. Phase 5 may revisit if UI needs the price inline.

---

### 3. Validator-vs-domain status codes

**Issue:** Some invalid inputs (negative price, past menu date, zero capacity, Salon without MesaId) return 400 via the FluentValidation layer rather than 422 from the domain.

**Intentional per design §6c (friendlier boundary checks).** Canonical note: "400 (validator) or 422 (domain if validator bypassed)."

---

### 4. IngredienteValidator whitespace gap (W-2 from PR2 verify)

**Issue:** `NotEmpty()` allows whitespace-only names through to the domain (→ 422). 

**Minor tightening:** `.Must(s => !string.IsNullOrWhiteSpace(s))` gives deterministic 400. Non-blocking; address in a future maintenance slice.

---

### 5. EF MultipleCollectionInclude on PedidoRepository.GetByIdAsync (S-01 from PR3)

**Issue:** EF Core logs a warning at runtime: "Compiling a query which loads related collections for more than one collection navigation... no QuerySplittingBehavior has been configured."

**Recommendation:** Consider `.AsSplitQuery()` when traffic warrants, to avoid the Cartesian explosion warning on multi-collection includes.

---

### 6. CA1848 logger pattern (informational)

**Issue:** `GastroGestionExceptionHandler` uses non-cached logger message.

**No functional impact.** Address in a future maintenance pass.

---

## Endpoint Signatures (TypedResults summary)

| Route | Verb | Request DTO | Response Type | Status codes |
|-------|------|-------------|---------------|--------------|
| `/clientes` | POST | `CrearClienteRequest` | Created`<Guid>` | 201, 400, 409, 422 |
| `/clientes/{id:guid}` | GET | — | `Ok<ClienteResponse>` | 200, 404 |
| `/clientes` | GET | — | `Ok<List<ClienteResponse>>` | 200 |
| `/ingredientes` | POST | `CrearIngredienteRequest` | `Created<Guid>` | 201, 400, 422 |
| `/ingredientes/{id:guid}` | GET | — | `Ok<IngredienteResponse>` | 200, 404 |
| `/ingredientes` | GET | — | `Ok<List<IngredienteResponse>>` | 200 |
| `/platos` | POST | `CrearPlatoRequest` | `Created<Guid>` | 201, 400, 422 |
| `/platos/{id:guid}` | GET | — | `Ok<PlatoResponse>` | 200, 404 |
| `/platos` | GET | — | `Ok<List<PlatoResponse>>` | 200 |
| `/menus` | POST | `CrearMenuRequest` | `Created<Guid>` | 201, 400, 422 |
| `/menus/{id:guid}` | GET | — | `Ok<MenuResponse>` | 200, 404 |
| `/menus` | GET | — | `Ok<List<MenuResponse>>` | 200 |
| `/mesas` | POST | `CrearMesaRequest` | `Created<Guid>` | 201, 400, 422 |
| `/mesas/{id:guid}` | GET | — | `Ok<MesaResponse>` | 200, 404 |
| `/mesas` | GET | — | `Ok<List<MesaResponse>>` | 200 |
| `/pedidos` | POST | `CrearPedidoRequest` | `Created<Guid>` | 201, 400, 404, 422 |
| `/pedidos/{id:guid}/lineas` | POST | `AgregarLineaRequest` | `Created<Guid>` | 201, 400, 404, 422 |
| `/pedidos/{id:guid}/lineas/{lineaId:guid}/confirmar-precio` | POST | — | `NoContent` | 204, 404, 422 |
| `/pedidos/{id:guid}/transicion` | POST | `TransicionarEstadoRequest` | `Ok<PedidoResponse>` | 200, 404, 422 |
| `/pedidos/{id:guid}` | GET | — | `Ok<PedidoResponse>` | 200, 404 |
| `/facturas` | POST | `CrearFacturaRequest` | `Created<Guid>` | 201, 409 |
| `/facturas/{id:guid}/pagos` | POST | `RegistrarPagoRequest` | `Ok<FacturaResponse>` | 200, 404, 422 |
| `/facturas/{id:guid}` | GET | — | `Ok<FacturaResponse>` | 200, 404 |
| `/stock/movimientos` | POST | `RegistrarMovimientoStockRequest` | `Created<Guid>` | 201, 400, 422 |
| `/stock/balance/{ingredienteId:guid}` | GET | — | `Ok<BalanceStockResponse>` | 200 |
| `/health` | GET | — | 200 OK | 200 |

---

## Development vs Production

- **Development:** Seeder runs; Swagger UI enabled; health endpoint returns 200.
- **Production:** Seeder does not run; Swagger UI disabled; all `[AllowAnonymous]` endpoints become protected once Phase 5 adds login and removes the attribute.

---

## Delivery Status

**Phase 4 complete:** WA-01 through WA-30 all [x] marked complete.
- **PR #9** (Slice 1 — Foundation): merged to main.
- **PR #10** (Slice 2 — Catalogue): merged to main.
- **PR #11** (Slice 3 — Transactional+Fiscal+Stock): merged to main.
- **Test suite:** 222 integration tests passing (0 failures).
- **Verification:** PASS WITH WARNINGS — 0 CRITICAL, 3 documented deviations (204 response, validator vs domain status codes, enum integers), 2 suggestions (JsonStringEnumConverter, MultipleCollectionInclude).

---

## Next Phase

Phase 5 will add the Usuario aggregate, login endpoint, remove `[AllowAnonymous]` from protected endpoints, and replace request-body `RolUsuario` with JWT claim extraction.
