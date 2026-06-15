# Web API — Delta Spec

**Change:** `web-api`
**Phase:** 4 of 7 — Application use cases + Minimal API + Dev seeder
**Scope:** `GastroGestion.Domain/Services` (W-01 async fix), `GastroGestion.Application` (all missing handlers + async ports + `GetAllAsync`), `GastroGestion.Contracts` (DTOs + validators), `GastroGestion.Api` (host wiring, error handling, endpoint groups), `GastroGestion.Infrastructure` (DevDataSeeder + `GetAllAsync` impls), `tests/GastroGestion.Api.Tests` (new).
**Artifact store:** hybrid (openspec + engram).
**Delivery:** 3 chained PRs stacked-to-main (PR 1 = foundation, PR 2 = catalogue, PR 3 = transactional + fiscal + stock).

This spec describes what MUST be true after Phase 4 is applied. It does NOT describe implementation mechanics.

**Locked decisions (not open questions):**
- Minimal APIs; `RouteGroupBuilder` per aggregate; `TypedResults`; no MVC controllers; no mediator.
- `GastroGestionExceptionHandler : IExceptionHandler` + `AddProblemDetails()` — RFC 7807 mapping: `ConflictException`→409, `DomainException`→422, not-found→404, unhandled→500.
- FluentValidation via a reusable endpoint filter → `ValidationProblem` (400).
- DTOs in `GastroGestion.Contracts`; manual static mapping; no AutoMapper; domain aggregates never serialised over the wire.
- Keep Swashbuckle 6.6.2; remove `Microsoft.AspNetCore.OpenApi`; Swagger dev-only.
- W-01 = async interface (Option A): `IEfectivoPrecioService` becomes async; `Task` is BCL → Domain stays zero-dependency; MUST land in PR 1.
- `GetAllAsync` added to all repository ports + EF implementations; GET-all endpoints exposed.
- JWT pipeline wired (`AddAuthentication(JwtBearerDefaults)` + `UseAuthentication` + `UseAuthorization`); ALL Phase-4 endpoints `[AllowAnonymous]`.
- `RolUsuario` supplied via request body for Pedido state-transition endpoints (no login yet — Phase 5 closes via JWT claim).
- `DevDataSeeder`: runtime, Development-only, idempotent on `Clientes.AnyAsync()`, via domain factories + repos, `Menu.FechaMenu` = `DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)`.
- Integration tests: `tests/GastroGestion.Api.Tests/` via `WebApplicationFactory<Program>` + LocalDB, tagged `[Trait("Category","Integration")]`.

---

## Slice 1 — API Foundation (PR 1)

---

### REQ-01 — W-01: `IEfectivoPrecioService` is async; Domain stays zero-dependency

`IEfectivoPrecioService` (in `GastroGestion.Domain.Services`) MUST declare an async method signature returning `Task<(Dinero Precio, PorcentajeIVA IVA)>`. The application-layer implementation (`EfectivoPrecioService`) MUST await repository calls with no `.GetAwaiter().GetResult()` or `.Result` access. `GastroGestion.Domain.csproj` MUST retain zero `<PackageReference>` and zero `<ProjectReference>` elements after this change (`Task` is BCL and requires no new reference).

#### Scenario 01-A — Async method signature on domain interface

```
GIVEN  IEfectivoPrecioService is inspected in GastroGestion.Domain.Services
WHEN   its members are listed
THEN   it declares exactly one method with a return type of Task<(Dinero Precio, PorcentajeIVA IVA)>
AND    no synchronous overload exists
```

#### Scenario 01-B — No blocking call in EfectivoPrecioService

```
GIVEN  EfectivoPrecioService.cs is inspected
WHEN   all calls to repository methods are scanned
THEN   no occurrence of .GetAwaiter().GetResult() or .Result is present
AND    the method body uses await for both _platos and _menus repository calls
```

#### Scenario 01-C — Domain project compiles with zero new references after W-01

```
GIVEN  GastroGestion.Domain.csproj is inspected after PR 1 is merged
WHEN   all <PackageReference> and <ProjectReference> elements are counted
THEN   the combined count is 0
```

---

### REQ-02 — RFC 7807 ProblemDetails error mapping

A single `GastroGestionExceptionHandler : IExceptionHandler` registered via `AddExceptionHandler<GastroGestionExceptionHandler>()` + `AddProblemDetails()` MUST translate domain and application exceptions to RFC 7807 responses. No endpoint MUST contain a try/catch for these exception types. The mapping table is fixed:

| Exception type | HTTP status | ProblemDetails title |
|---|---|---|
| `ConflictException` | 409 Conflict | "Conflict" |
| `DomainException` | 422 Unprocessable Entity | "Domain Rule Violation" |
| Null repository return / explicit `KeyNotFoundException` | 404 Not Found | "Not Found" |
| Any other unhandled exception | 500 Internal Server Error | "Internal Server Error" |

#### Scenario 02-A — ConflictException maps to 409

```
GIVEN  an endpoint whose handler throws ConflictException("test conflict")
WHEN   the endpoint is called via HTTP
THEN   the response status is 409
AND    Content-Type is application/problem+json
AND    the body contains "status": 409
AND    the body contains "title": "Conflict"
```

#### Scenario 02-B — DomainException maps to 422

```
GIVEN  an endpoint whose handler throws DomainException("rule violated")
WHEN   the endpoint is called via HTTP
THEN   the response status is 422
AND    Content-Type is application/problem+json
AND    the body contains "status": 422
AND    the body contains "title": "Domain Rule Violation"
```

#### Scenario 02-C — Not-found maps to 404

```
GIVEN  a GET endpoint that receives a request for a non-existent resource Id
WHEN   the endpoint is called via HTTP
THEN   the response status is 404
AND    Content-Type is application/problem+json
AND    the body contains "status": 404
```

#### Scenario 02-D — Unhandled exception maps to 500

```
GIVEN  an endpoint whose handler throws an unrecognised exception type
WHEN   the endpoint is called via HTTP
THEN   the response status is 500
AND    Content-Type is application/problem+json
AND    the response body does NOT expose raw stack trace text
```

---

### REQ-03 — FluentValidation endpoint filter returns 400 ValidationProblem

A reusable `WithValidation<TRequest>()` endpoint filter MUST be applied to all mutating endpoints (POST/PUT/PATCH). When `AbstractValidator<TRequest>` finds failures, the filter MUST short-circuit before the handler executes and return a `ValidationProblem` (400) response with field-level error details. Validators MUST be registered via `AddValidatorsFromAssemblyContaining<T>()` at startup.

#### Scenario 03-A — Invalid request is rejected before handler execution

```
GIVEN  a POST /clientes endpoint with a validator that requires non-empty Nombre
WHEN   a request with Nombre = "" is submitted
THEN   the response status is 400
AND    Content-Type is application/problem+json
AND    the response body contains an "errors" object with a key matching the Nombre field
AND    the handler (CrearClienteHandler) is NOT invoked
```

#### Scenario 03-B — Valid request passes through to handler

```
GIVEN  a POST /clientes endpoint with the FluentValidation filter
WHEN   a request with all required fields populated correctly is submitted
THEN   the filter does not short-circuit
AND    the handler is invoked
AND    the response status is 201
```

---

### REQ-04 — JWT authentication pipeline wired; all Phase-4 endpoints anonymous

`Program.cs` MUST call `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`, `UseAuthentication()`, and `UseAuthorization()`. All Phase-4 endpoints MUST carry `[AllowAnonymous]`. No endpoint MUST require a bearer token to respond in Phase 4.

#### Scenario 04-A — Unauthenticated request reaches any Phase-4 endpoint

```
GIVEN  the API is running with no Authorization header
WHEN   any Phase-4 endpoint is called (e.g., GET /health, POST /clientes)
THEN   the response status is NOT 401 and NOT 403
AND    the endpoint processes the request normally
```

#### Scenario 04-B — JWT pipeline is present in the middleware stack

```
GIVEN  Program.cs is inspected
WHEN   the middleware pipeline configuration is scanned
THEN   AddAuthentication with JwtBearerDefaults is called
AND    UseAuthentication() is called before UseAuthorization()
AND    UseAuthorization() is called
```

---

### REQ-05 — DevDataSeeder seeds a realistic dataset in Development only; idempotent

`DevDataSeeder` in `GastroGestion.Infrastructure` MUST be called from `Program.cs` after auto-migrate, ONLY when `app.Environment.IsDevelopment()` is true. On first run against an empty database, it MUST create: 3 Clientes (one `ConsumidorFinal`, one `ResponsableInscripto` with a valid CUIT, one `Exento`), 5 Ingredientes with varied `UnidadDeMedida`, 3 Platos with `LineaReceta` entries referencing the seeded Ingredientes, 1 Menu with `FechaMenu` = `DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)` (computed at runtime) and at least one `MenuItem` with a `PrecioOverride`, 4 Mesas with varied capacities, 1 Salon-type Pedido and 1 Mostrador-type Pedido (each with at least one LineaPedido with a confirmed price), and 1 Factura of type `TicketInterno` created from the Mostrador Pedido. All entities MUST be created via domain factory methods (no raw SQL, no direct property sets bypassing factories). On second run against an already-seeded database (i.e., `Clientes.AnyAsync()` returns true), the seeder MUST return without inserting any records.

#### Scenario 05-A — Empty database is seeded on Development startup

```
GIVEN  the API starts in Development with an empty LocalDB
WHEN   Program.cs completes startup (including seeder)
THEN   the database contains at least 3 Clientes
AND    the database contains at least 5 Ingredientes
AND    the database contains at least 3 Platos
AND    the database contains at least 1 Menu with FechaMenu >= today
AND    the database contains at least 4 Mesas
AND    the database contains at least 2 Pedidos
AND    the database contains at least 1 Factura
```

#### Scenario 05-B — Second startup is idempotent (no duplication)

```
GIVEN  the API has already seeded the database in a previous startup
WHEN   the API restarts in Development
THEN   the Cliente count remains the same as after the first startup
AND    no duplicate entities are created
```

#### Scenario 05-C — Seeder does not run outside Development

```
GIVEN  the API starts with ASPNETCORE_ENVIRONMENT != "Development"
WHEN   Program.cs completes startup
THEN   the DevDataSeeder is not invoked
AND    the database remains unchanged by the seeder
```

#### Scenario 05-D — Seeded Menu has a future FechaMenu

```
GIVEN  the DevDataSeeder runs on a fresh database
WHEN   the seeded Menu is loaded from the database
THEN   Menu.FechaMenu >= DateOnly.FromDateTime(DateTime.UtcNow)
AND    the Menu entity was created without throwing a DomainException
      (proving the future-date domain guard was satisfied at seed time)
```

#### Scenario 05-E — Domain invariants hold on all seeded entities

```
GIVEN  the DevDataSeeder completes successfully
WHEN   any seeded entity is loaded and its invariant-enforcing methods are called
THEN   no DomainException is thrown due to invalid seeded state
      (e.g., confirmed price on LineaPedido rejects a second ConfirmarPrecio call)
```

---

### REQ-06 — Health endpoint + Swagger dev-only

A health endpoint MUST return 200 OK. Swagger UI MUST be served only when the environment is Development. `Microsoft.AspNetCore.OpenApi` MUST NOT be present in `GastroGestion.Api.csproj`.

#### Scenario 06-A — Health endpoint returns 200

```
GIVEN  the API is running
WHEN   GET /health is called
THEN   the response status is 200
```

#### Scenario 06-B — Swagger is accessible in Development

```
GIVEN  the API is running with ASPNETCORE_ENVIRONMENT = "Development"
WHEN   GET /swagger/index.html is requested
THEN   the response status is 200
```

#### Scenario 06-C — Microsoft.AspNetCore.OpenApi is removed

```
GIVEN  GastroGestion.Api.csproj is inspected
WHEN   all <PackageReference> elements are listed
THEN   no entry for Microsoft.AspNetCore.OpenApi is present
AND    Swashbuckle.AspNetCore is present
```

---

### REQ-07 — Api.Tests project exists with a smoke test via WebApplicationFactory

`tests/GastroGestion.Api.Tests/` MUST be a valid xUnit project that references `WebApplicationFactory<Program>` and LocalDB. It MUST contain at least one smoke-test that exercises the health endpoint. All tests MUST be tagged `[Trait("Category","Integration")]`.

#### Scenario 07-A — Smoke test passes against LocalDB

```
GIVEN  SQL Server LocalDB is available
WHEN   dotnet test tests/GastroGestion.Api.Tests/ --filter "Category=Integration" is executed
THEN   the command exits with code 0
AND    at minimum the health-check test is reported as passed
```

---

## Slice 2 — Catalogue Endpoints + Use Cases (PR 2)

---

### REQ-08 — All repository ports gain GetAllAsync; implementations load the full owned graph

Every repository port in `GastroGestion.Application.Abstractions.Persistence` MUST add `Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)` for the catalogue aggregates (`IClienteRepository`, `IIngredienteRepository`, `IPlatoRepository`, `IMenuRepository`, `IMesaRepository`) and the transactional/fiscal aggregates needed for list queries. Each EF Core implementation MUST use the same eager-include strategy as the existing `GetByIdAsync` (full owned-entity graph loaded without lazy-loading).

#### Scenario 08-A — GetAllAsync returns all seeded Clientes

```
GIVEN  the database has been seeded with 3 Clientes via DevDataSeeder
WHEN   IClienteRepository.GetAllAsync() is called
THEN   the result contains exactly 3 Cliente entities
AND    each entity has its Direcciones collection loaded (not null)
```

#### Scenario 08-B — GetAllAsync returns all seeded Platos with recipe lines

```
GIVEN  the database has been seeded with 3 Platos each having LineaReceta entries
WHEN   IPlatoRepository.GetAllAsync() is called
THEN   the result contains 3 Plato entities
AND    each Plato.Receta collection is non-empty and fully populated
```

---

### REQ-09 — Cliente endpoints: create + get-by-id + get-all

A `RouteGroupBuilder` mounted at `/clientes` MUST expose:
- `POST /clientes` — validates request DTO, calls `CrearClienteHandler`, returns `201 Created` with a `ClienteResponse` DTO and a `Location` header pointing to the new resource. Invalid CUIT for `ResponsableInscripto` MUST produce a 422 (DomainException from `Cliente.Crear`).
- `GET /clientes/{id}` — calls `GetClienteByIdHandler`; returns `200 OK` with `ClienteResponse` or `404`.
- `GET /clientes` — calls `GetAllClientesHandler`; returns `200 OK` with `IReadOnlyList<ClienteResponse>`.

No domain aggregate type (`Cliente`) MUST appear in any request or response contract; only `ClienteResponse` and `CrearClienteRequest` DTOs from `GastroGestion.Contracts` cross the HTTP boundary.

#### Scenario 09-A — Create Cliente returns 201 with Location

```
GIVEN  a valid POST /clientes request with Nombre, Apellido, and CondicionIVA = ConsumidorFinal
WHEN   the request is processed
THEN   the response status is 201
AND    the Location header is set to /clientes/{newId}
AND    the response body contains a ClienteResponse with a non-empty Id
AND    the body does NOT contain any property that exposes the raw Cliente aggregate
```

#### Scenario 09-B — Create ResponsableInscripto without CUIT returns 422

```
GIVEN  a POST /clientes request with CondicionIVA = ResponsableInscripto and Cuit = null
WHEN   the request is processed
THEN   the response status is 422
AND    the body contains "title": "Domain Rule Violation"
```

#### Scenario 09-C — Get non-existent Cliente returns 404

```
GIVEN  a GET /clientes/{id} request where the Id does not exist in the database
WHEN   the request is processed
THEN   the response status is 404
AND    Content-Type is application/problem+json
```

#### Scenario 09-D — Get-all returns the seeded Clientes

```
GIVEN  the database has been seeded with 3 Clientes
WHEN   GET /clientes is called
THEN   the response status is 200
AND    the body is a JSON array containing 3 elements
```

---

### REQ-10 — Ingrediente endpoints: create + get-by-id + get-all

A `RouteGroupBuilder` mounted at `/ingredientes` MUST expose `POST /ingredientes` (returns 201), `GET /ingredientes/{id}` (200 or 404), and `GET /ingredientes` (200 array). Blank `NombreIngrediente` MUST produce a 422 (DomainException). DTOs in `GastroGestion.Contracts` only.

#### Scenario 10-A — Create Ingrediente with blank name returns 422

```
GIVEN  a POST /ingredientes request with NombreIngrediente = "   " (whitespace)
WHEN   the request is processed
THEN   the response status is 422
AND    the body indicates a domain rule violation
```

#### Scenario 10-B — Create Ingrediente returns 201 with valid data

```
GIVEN  a POST /ingredientes request with a non-blank NombreIngrediente and a valid UnidadDeMedida
WHEN   the request is processed
THEN   the response status is 201
AND    the response body contains an IngredienteResponse with a non-empty Id
```

---

### REQ-11 — Plato endpoints: create + get-by-id + get-all

A `RouteGroupBuilder` at `/platos` MUST expose `POST /platos` (returns 201), `GET /platos/{id}` (200 or 404), and `GET /platos` (200 array). Negative `PrecioBase` or blank `NombrePlato` MUST produce a 422. DTOs in `GastroGestion.Contracts` only; the `PlatoResponse` MUST include the `Receta` lines if present.

#### Scenario 11-A — Create Plato returns 201 with recipe lines

```
GIVEN  a POST /platos request with a valid name, PrecioBase, AlicuotaIVA, and one LineaReceta
WHEN   the request is processed
THEN   the response status is 201
AND    the body contains a PlatoResponse with Receta containing 1 line
```

#### Scenario 11-B — Negative PrecioBase returns 422

```
GIVEN  a POST /platos request with PrecioBase.Amount = -1
WHEN   the request is processed
THEN   the response status is 422
```

---

### REQ-12 — Menu endpoints: create + get-by-id + get-all

A `RouteGroupBuilder` at `/menus` MUST expose `POST /menus` (returns 201), `GET /menus/{id}` (200 or 404), and `GET /menus` (200 array). A past `FechaMenu` MUST produce a 422 (DomainException from `Menu.Crear`). DTOs in `GastroGestion.Contracts` only.

#### Scenario 12-A — Past FechaMenu returns 422

```
GIVEN  a POST /menus request with FechaMenu = yesterday's date
WHEN   the request is processed
THEN   the response status is 422
AND    the body contains "title": "Domain Rule Violation"
```

#### Scenario 12-B — Create Menu with future date returns 201

```
GIVEN  a POST /menus request with FechaMenu = tomorrow's date and at least one MenuItem
WHEN   the request is processed
THEN   the response status is 201
AND    the body contains a MenuResponse with Items containing the submitted item
```

---

### REQ-13 — Mesa endpoints: create + get-by-id + get-all

A `RouteGroupBuilder` at `/mesas` MUST expose `POST /mesas` (returns 201), `GET /mesas/{id}` (200 or 404), and `GET /mesas` (200 array). Zero or negative `Capacidad` MUST produce a 422. DTOs in `GastroGestion.Contracts` only.

#### Scenario 13-A — Create Mesa with zero Capacidad returns 422

```
GIVEN  a POST /mesas request with Capacidad = 0
WHEN   the request is processed
THEN   the response status is 422
```

#### Scenario 13-B — Create Mesa returns 201

```
GIVEN  a POST /mesas request with Numero = 5 and Capacidad = 4
WHEN   the request is processed
THEN   the response status is 201
AND    the body contains a MesaResponse with Capacidad = 4
```

---

### REQ-14 — Catalogue GET-all endpoints return seeded data together

After the DevDataSeeder has run in Development, GET-all calls to each catalogue endpoint MUST return at least the seeded entities for their respective aggregate types, confirming the full pipeline (seeder → repository → handler → endpoint → DTO) is wired correctly.

#### Scenario 14-A — GET /menus returns seeded Menu

```
GIVEN  the database has been seeded by DevDataSeeder
WHEN   GET /menus is called
THEN   the response contains at least 1 menu
AND    the returned menu's FechaMenu is a future date relative to today
```

---

## Slice 3 — Transactional + Fiscal + Stock Endpoints (PR 3)

---

### REQ-15 — Pedido lifecycle endpoints

A `RouteGroupBuilder` at `/pedidos` MUST expose:
- `POST /pedidos` — creates a Pedido via `CrearPedidoHandler`. For `TipoPedido.Salon`, a null `MesaId` MUST return 422. Returns 201.
- `POST /pedidos/{id}/lineas` — adds a line via `AgregarLineaHandler`. Returns 201. Pedido not found → 404.
- `POST /pedidos/{id}/lineas/{lineaId}/confirmar-precio` — resolves price via the now-async `IEfectivoPrecioService` through `ConfirmarPrecioLineaHandler`. Returns 200 with the updated line snapshot. Second confirmation attempt MUST return 422 (DomainException from set-once invariant).
- `POST /pedidos/{id}/transicion` — transitions state via `TransicionarEstadoPedidoHandler`. The request body MUST include `RolUsuario`. An invalid transition or unauthorized role MUST return 422. Returns 200 with updated `PedidoResponse`.
- `GET /pedidos/{id}` — returns 200 with `PedidoResponse` or 404.

DTOs in `GastroGestion.Contracts` only; `Pedido` aggregate is never serialised directly.

#### Scenario 15-A — Create Salon Pedido without MesaId returns 422

```
GIVEN  a POST /pedidos request with TipoPedido = Salon and MesaId = null
WHEN   the request is processed
THEN   the response status is 422
AND    the body indicates a domain rule violation
```

#### Scenario 15-B — Create Pedido returns 201

```
GIVEN  a POST /pedidos request with TipoPedido = Mostrador and a valid ClienteId
WHEN   the request is processed
THEN   the response status is 201
AND    the body contains a PedidoResponse with Estado = Creado
```

#### Scenario 15-C — ConfirmarPrecio exercises async IEfectivoPrecioService (W-01 fix live)

```
GIVEN  a Pedido with an unconfirmed LineaPedido referencing an existing PlatoId
WHEN   POST /pedidos/{id}/lineas/{lineaId}/confirmar-precio is called
THEN   the response status is 200
AND    the response body contains PrecioUnitario != null and IVA != null
AND    no deadlock occurs (the call completes within a reasonable timeout on the ASP.NET Core thread pool)
```

#### Scenario 15-D — Second price confirmation returns 422

```
GIVEN  a LineaPedido that already has a confirmed price
WHEN   POST /pedidos/{id}/lineas/{lineaId}/confirmar-precio is called again
THEN   the response status is 422
AND    the body contains "title": "Domain Rule Violation"
```

#### Scenario 15-E — TransicionarEstado with wrong role returns 422

```
GIVEN  a Mostrador Pedido in state Creado
AND    a POST /pedidos/{id}/transicion request with RolUsuario = Repartidor
      where Repartidor is not in the allowed roles for (Mostrador, Creado→Preparandose)
WHEN   the request is processed
THEN   the response status is 422
AND    the body indicates a domain rule violation (unauthorized role)
```

#### Scenario 15-F — TransicionarEstado with valid role returns 200

```
GIVEN  a Mostrador Pedido in state Creado
AND    a POST /pedidos/{id}/transicion request with EstadoDestino = Preparandose and a valid RolUsuario
WHEN   the request is processed
THEN   the response status is 200
AND    the returned PedidoResponse.Estado is Preparandose
```

#### Scenario 15-G — Get non-existent Pedido returns 404

```
GIVEN  a GET /pedidos/{id} request with a Guid that does not exist
WHEN   the request is processed
THEN   the response status is 404
```

---

### REQ-16 — Factura endpoints: create + register payment + get-by-id

A `RouteGroupBuilder` at `/facturas` MUST expose:
- `POST /facturas` — wires the existing `CrearFacturaHandler`; multi-client Pedidos MUST return 409 (ConflictException — closes Infrastructure REQ-11 at the HTTP layer); Pedido with no confirmed lines MUST return 409; returns 201 with `FacturaResponse` and Location header.
- `POST /facturas/{id}/pagos` — registers a payment via `RegistrarPagoHandler`; cancelling or paying a Pagada/Cancelada Factura MUST return 422; returns 200 with updated `FacturaResponse`.
- `GET /facturas/{id}` — returns 200 with `FacturaResponse` or 404.

DTOs in `GastroGestion.Contracts` only.

#### Scenario 16-A — CrearFactura with mixed-client Pedidos returns 409

```
GIVEN  PedidoA belongs to ClienteId = A and PedidoB belongs to ClienteId = B
WHEN   POST /facturas is called referencing both PedidoIds with ClienteId = A
THEN   the response status is 409
AND    Content-Type is application/problem+json
AND    the body contains "status": 409
```

#### Scenario 16-B — CrearFactura with Pedido having no confirmed lines returns 409

```
GIVEN  a Pedido whose LineaPedido entries have no confirmed price (PrecioUnitario = null)
WHEN   POST /facturas is called referencing that Pedido
THEN   the response status is 409
AND    the body indicates no billable lines exist
```

#### Scenario 16-C — CrearFactura returns 201 with Location

```
GIVEN  one or more Pedidos belonging to the same ClienteId, all with at least one confirmed line
WHEN   POST /facturas is called with a valid TipoComprobanteSolicitado
THEN   the response status is 201
AND    the Location header is set to /facturas/{newId}
AND    the body contains a FacturaResponse with Estado = Creada
```

#### Scenario 16-D — RegistrarPago transitions Factura to Pagada when total is covered

```
GIVEN  a Factura in state Creada with Total = T
WHEN   POST /facturas/{id}/pagos is called with Monto = T
THEN   the response status is 200
AND    the returned FacturaResponse.Estado is Pagada
```

#### Scenario 16-E — RegistrarPago on Cancelada Factura returns 422

```
GIVEN  a Factura in state Cancelada
WHEN   POST /facturas/{id}/pagos is called
THEN   the response status is 422
AND    the body contains "title": "Domain Rule Violation"
```

#### Scenario 16-F — Get non-existent Factura returns 404

```
GIVEN  a GET /facturas/{id} request with a Guid that does not exist
WHEN   the request is processed
THEN   the response status is 404
```

---

### REQ-17 — Stock endpoints: register movement + get balance

A `RouteGroupBuilder` at `/stock` MUST expose:
- `POST /stock/movimientos` — creates a `MovimientoStock` via `RegistrarMovimientoStockHandler` using the domain factory; returns 201. The append-only constraint MUST be preserved (no update/delete path exists).
- `GET /stock/{ingredienteId}/balance` — returns the current net balance via `IMovimientoStockRepository.CalcularBalanceAsync`; returns 200 with a numeric balance value. `IngredienteId` not found (zero movements) MUST return 0 (not 404), consistent with a zero-balance interpretation.

DTOs in `GastroGestion.Contracts` only.

#### Scenario 17-A — Register Compra movement returns 201

```
GIVEN  a POST /stock/movimientos request with TipoMovimiento = Compra, IngredienteId, and Cantidad > 0
WHEN   the request is processed
THEN   the response status is 201
AND    the body contains a MovimientoStockResponse with TipoMovimiento = Compra
AND    Cantidad is positive (sign convention enforced by domain factory)
```

#### Scenario 17-B — Register Consumo movement returns 201 with negative Cantidad

```
GIVEN  a POST /stock/movimientos request with TipoMovimiento = Consumo and Cantidad = 3m
WHEN   the request is processed
THEN   the response status is 201
AND    the stored MovimientoStock.Cantidad is -3m (domain factory applies sign convention)
```

#### Scenario 17-C — GetBalanceStock returns correct net balance

```
GIVEN  the following movements for IngredienteId = X have been registered:
  Compra: +20m
  Reserva: -5m
  LiberacionReserva: +5m
WHEN   GET /stock/{X}/balance is called
THEN   the response status is 200
AND    the body contains balance = 20.0
```

#### Scenario 17-D — Balance for IngredienteId with no movements returns 0

```
GIVEN  no MovimientoStock entries exist for IngredienteId = Y
WHEN   GET /stock/{Y}/balance is called
THEN   the response status is 200
AND    the body contains balance = 0
```

---

## Cross-cutting requirements

---

### REQ-18 — Domain aggregates are never serialised over the wire

No endpoint response MUST directly serialise a domain aggregate type (e.g., `Cliente`, `Pedido`, `Factura`, `Mesa`, `Plato`, `Menu`, `Ingrediente`, `MovimientoStock`). All request and response contracts MUST use types from `GastroGestion.Contracts`. Mapping MUST be performed via hand-written static extension methods; no AutoMapper dependency MUST exist.

#### Scenario 18-A — POST /clientes response body contains only Contracts types

```
GIVEN  a successful POST /clientes call
WHEN   the response body is inspected for type information
THEN   no property corresponds to a domain aggregate internal member
      (e.g., no DomainEvents collection, no ConcurrencyToken byte[] array)
AND    the response is a flat ClienteResponse DTO from GastroGestion.Contracts
```

---

### REQ-19 — No mediator in the application layer

Handler classes MUST be injected directly into endpoint delegates via DI. No `IMediator`, `ISender`, or equivalent mediator interface MUST be present in any handler call path. The `CrearFacturaHandler` precedent (registered as `AddScoped<CrearFacturaHandler>()`) MUST be replicated for all new handlers.

#### Scenario 19-A — Handler injected directly into endpoint delegate

```
GIVEN  any Phase-4 endpoint delegate
WHEN   its parameter list is inspected
THEN   it accepts a concrete handler type (e.g., CrearClienteHandler) directly
AND    no IMediator or ISender parameter is present
```

---

### REQ-20 — Integration test suite covers all three slices

`tests/GastroGestion.Api.Tests/` MUST contain test classes covering:
- Slice 1: health check (REQ-06), exception mapping (REQ-02), validation filter (REQ-03).
- Slice 2: at minimum one happy-path and one error-path test per catalogue endpoint group.
- Slice 3: at minimum one happy-path and one error-path test for Pedido lifecycle, Factura creation, and stock balance.

All tests MUST be tagged `[Trait("Category","Integration")]` and MUST pass with `dotnet test` against LocalDB.

#### Scenario 20-A — Full integration test suite passes

```
GIVEN  SQL Server LocalDB is available
WHEN   dotnet test tests/GastroGestion.Api.Tests/ --filter "Category=Integration" is executed
THEN   the command exits with code 0
AND    tests spanning all three slices are reported as passed
```

---

## Requirement summary

| REQ | Slice | Area | Closes / relates to |
|-----|-------|------|---------------------|
| REQ-01 | 1 | W-01 async fix | Infra spec W-01 open item |
| REQ-02 | 1 | RFC 7807 error mapping | — |
| REQ-03 | 1 | FluentValidation filter | — |
| REQ-04 | 1 | JWT pipeline + AllowAnonymous | — |
| REQ-05 | 1 | DevDataSeeder | — |
| REQ-06 | 1 | Health + Swagger + package cleanup | — |
| REQ-07 | 1 | Api.Tests smoke test | — |
| REQ-08 | 2 | GetAllAsync on all ports | — |
| REQ-09 | 2 | Cliente endpoints | Domain REQ-03 |
| REQ-10 | 2 | Ingrediente endpoints | Domain REQ-04 |
| REQ-11 | 2 | Plato endpoints | Domain REQ-05 |
| REQ-12 | 2 | Menu endpoints | Domain REQ-06 |
| REQ-13 | 2 | Mesa endpoints | Domain REQ-14 |
| REQ-14 | 2 | Catalogue GET-all smoke | REQ-05 + REQ-08 |
| REQ-15 | 3 | Pedido lifecycle endpoints | Domain REQ-07/08/09 |
| REQ-16 | 3 | Factura endpoints | Domain REQ-13 + Infra REQ-11 |
| REQ-17 | 3 | Stock endpoints | Domain REQ-12 |
| REQ-18 | cross | No domain types on wire | — |
| REQ-19 | cross | No mediator | — |
| REQ-20 | cross | Integration test coverage | REQ-07 |

---

## Known assumptions and deferred items

### A-01 — `RolUsuario` body-supplied is a documented security hole

Phase 4 accepts `RolUsuario` in the request body for `POST /pedidos/{id}/transicion`. Any caller can claim any role. This is a deliberate, time-boxed bypass documented in the proposal. **Phase 5 MUST replace this with JWT-claim extraction.** The spec does not model this as a security requirement because the security story is out of Phase 4 scope.

### A-02 — GET-all endpoints return unbounded sets

No pagination, filtering, or sorting is specced for GET-all endpoints. The domain enum `Rol` (used in domain transitions) and the enum used in the HTTP request body for `RolUsuario` are assumed to be the same type; the design phase MUST confirm the exact CLR name (`Rol` vs `RolUsuario`) used in `Pedido.TransicionarEstado` and reconcile the request DTO accordingly.

### A-03 — Menu seeder stale-date risk

The seeded Menu's `FechaMenu` is tomorrow at seed time. If the developer does not re-seed, `IMenuRepository.GetActivosByFechaAsync(today)` will eventually return no matching menu, and `ConfirmarPrecio` will fall back to `Plato.PrecioBase`. This is acceptable; the mitigation is to drop and recreate the dev database. No spec requirement is added for automatic re-seeding.

### A-04 — LocalDB required for Api.Tests in CI

Integration tests tagged `[Trait("Category","Integration")]` require LocalDB. CI pipelines that cannot provision LocalDB MUST skip this trait. Testcontainers is the documented future upgrade path.
