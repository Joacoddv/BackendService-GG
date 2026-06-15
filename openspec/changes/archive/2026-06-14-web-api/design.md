# Technical Design — Web API Layer + Application Use Cases + Dev Seeder (Phase 4 of 7)

This design locks the **HOW at architecture level** for exposing the persisted Phase-3 domain over HTTP: the Minimal-API host layout, the full list of Application use cases to author (each pinned to the **real** domain factory signature), the DTO ↔ command/query ↔ response mapping, the W-01 async refactor with every call site, the `IExceptionHandler` mapping table, the `ValidationFilter<T>`, the `DevDataSeeder`, the `GetAllAsync` port additions, the `Api.Tests` harness, and the 3-slice chained breakdown.

> Scope guard: this document does NOT write spec scenarios (that is `sdd-spec`) and does NOT enumerate implementation steps (that is `sdd-tasks`). It decides shapes, boundaries, and rationale. The LOCKED decisions from the proposal/user are designed AROUND, not re-opened.

> **The CLR types in `src/GastroGestion.Domain/` and `src/GastroGestion.Application/` are authoritative.** Phase 3 had spec/design name drift; this design was written by reading the actual shipped code. Every factory name, parameter, and property reference below is verified against source. Where the proposal used indicative names (e.g. `ResolverPrecioEfectivo`, `Exento`, `fechaVigencia`), this design records the **real** name.

---

## Quick path (what gets built, in slice order)

1. **PR 1 — API foundation.** W-01 async fix (interface + impl + call sites); `GastroGestionExceptionHandler : IExceptionHandler` + `AddProblemDetails()`; `ValidationFilter<T>` + FluentValidation registration; JWT auth pipeline (`[AllowAnonymous]` everywhere); remove `Microsoft.AspNetCore.OpenApi`; `DevDataSeeder` (idempotent, dev-only, tomorrow-date); `Program.cs` composition rewrite; `GastroGestion.Api.Tests` project + smoke test. Must precede every endpoint.
2. **PR 2 — Catalogue.** `CrearCliente`/`GetClienteById`/`GetAllClientes`, `CrearIngrediente`/Get(All), `CrearPlato`/Get(All), `CrearMenu`/Get(All), `CrearMesa`/Get(All) handlers; catalogue DTOs + validators + mapping; `GetAllAsync` on the five catalogue ports + impls; `/clientes` `/ingredientes` `/platos` `/menus` `/mesas` endpoint groups. Depends on PR 1.
3. **PR 3 — Transactional + fiscal + stock.** `CrearPedido`/`AgregarLinea`/`ConfirmarPrecioLinea` (exercises W-01)/`TransicionarEstadoPedido` (role from body)/`GetPedidoById`; `CrearFactura` endpoint (handler exists) + `RegistrarPago`/`GetFacturaById`; `RegistrarMovimientoStock`/`GetBalanceStock`; their DTOs + validators; `/pedidos` `/facturas` `/stock` endpoint groups. Depends on PR 1–2.

Each slice ends green against `(localdb)\mssqllocaldb` and is independently revertible.

---

## 1. Project layout & file placement

The Phase-1 reference graph is unchanged (`Domain ◄ Application ◄ Infrastructure ◄ Api`, plus `Contracts`). This phase fills the empty `Contracts` project and adds endpoints + handlers.

### 1a. Contracts reference decision (NEW — required)

`GastroGestion.Contracts.csproj` today has **zero ProjectReferences**. DTOs alone need none, but the **manual mapping extensions** must construct Application commands and read domain enums/VOs. Decision:

```
GastroGestion.Contracts  ──ProjectReference──►  GastroGestion.Application  (─► Domain transitively)
```

- Request/response DTOs are plain `record`s (primitives + enums only — no VO, no aggregate).
- Mapping extension methods (`ToCommand()`, `ToResponse()`) live in Contracts and may reference Application commands + Domain enums/VOs.
- `Api` already references `Contracts` and `Application`; no Api reference change beyond removing the redundant OpenAPI package.
- **Rationale:** keeps mapping co-located with the DTO it maps, compile-time safe, no AutoMapper. Contracts depending on Application is acceptable — Contracts is a leaf consumed only by Api, never by Domain/Application/Infrastructure, so no cycle.

### 1b. Directory layout

```text
src/
├─ GastroGestion.Domain/
│  └─ Services/IEfectivoPrecioService.cs        (MODIFIED — async, §4)
│
├─ GastroGestion.Application/
│  ├─ Abstractions/Persistence/                 (MODIFIED — GetAllAsync added, §8)
│  │  ├─ IClienteRepository.cs ... IMesaRepository.cs
│  ├─ Services/EfectivoPrecioService.cs         (MODIFIED — genuinely async, §4)
│  ├─ Clientes/                                 (NEW use cases)
│  │  ├─ CrearCliente/{CrearClienteCommand,CrearClienteHandler}.cs
│  │  ├─ GetClienteById/{GetClienteByIdQuery,GetClienteByIdHandler}.cs
│  │  └─ GetAllClientes/{GetAllClientesQuery,GetAllClientesHandler}.cs
│  ├─ Ingredientes/  (CrearIngrediente, GetIngredienteById, GetAllIngredientes)
│  ├─ Platos/        (CrearPlato, GetPlatoById, GetAllPlatos)
│  ├─ Menus/         (CrearMenu, GetMenuById, GetAllMenus)
│  ├─ Mesas/         (CrearMesa, GetMesaById, GetAllMesas)
│  ├─ Pedidos/       (CrearPedido, AgregarLinea, ConfirmarPrecioLinea,
│  │                  TransicionarEstadoPedido, GetPedidoById)
│  ├─ Facturacion/
│  │  ├─ CrearFactura/   (EXISTS — unchanged)
│  │  ├─ RegistrarPago/{RegistrarPagoCommand,RegistrarPagoHandler}.cs
│  │  └─ GetFacturaById/{GetFacturaByIdQuery,GetFacturaByIdHandler}.cs
│  ├─ Stock/
│  │  ├─ RegistrarMovimientoStock/{...Command,...Handler}.cs
│  │  └─ GetBalanceStock/{GetBalanceStockQuery,GetBalanceStockHandler}.cs
│  └─ DependencyInjection.cs                     (MODIFIED — register all handlers + validators)
│
├─ GastroGestion.Contracts/                      (NEW content — was empty)
│  ├─ GastroGestion.Contracts.csproj             (MODIFIED — add ProjectReference to Application)
│  ├─ Clientes/{ClienteRequests.cs, ClienteResponses.cs, ClienteMappings.cs, ClienteValidators.cs}
│  ├─ Ingredientes/ ... Mesas/                   (same 4-file pattern per aggregate)
│  ├─ Pedidos/ , Facturacion/ , Stock/
│  └─ (validators MAY live in Contracts alongside DTOs — see §6)
│
├─ GastroGestion.Infrastructure/
│  ├─ Persistence/Repositories/                  (MODIFIED — GetAllAsync impls, §8)
│  └─ Persistence/Seed/DevDataSeeder.cs          (NEW, §7)
│
└─ GastroGestion.Api/
   ├─ Program.cs                                 (REWRITTEN composition — §1c order)
   ├─ GastroGestion.Api.csproj                   (MODIFIED — +FluentValidation +JwtBearer, −OpenApi)
   ├─ Endpoints/
   │  ├─ ClienteEndpoints.cs ... MesaEndpoints.cs
   │  ├─ PedidoEndpoints.cs , FacturaEndpoints.cs , StockEndpoints.cs
   ├─ Filters/ValidationFilter.cs                (generic endpoint filter, §6)
   └─ ErrorHandling/GastroGestionExceptionHandler.cs   (IExceptionHandler, §5)

tests/
└─ GastroGestion.Api.Tests/                      (NEW — WebApplicationFactory, §9)
   ├─ GastroGestion.Api.Tests.csproj
   ├─ ApiFactory.cs                              (WebApplicationFactory<Program>)
   └─ SmokeTests.cs , CatalogueEndpointTests.cs , TransactionalEndpointTests.cs
```

> `Program.cs` must expose the implicit `Program` class to the test project: add `public partial class Program { }` at the bottom (the standard Minimal-API `WebApplicationFactory<Program>` enabler).

### 1c. `Program.cs` composition order (LOCKED)

Service registration (`builder.Services`):
1. Serilog (existing).
2. `AddApplication()` + `AddInfrastructure(config)` (existing).
3. `AddHealthChecks()` (existing).
4. **`AddProblemDetails()`** + `AddExceptionHandler<GastroGestionExceptionHandler>()`.
5. **`AddValidatorsFromAssemblyContaining<...>()`** (FluentValidation — scan the Contracts assembly).
6. **`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`** + `AddAuthorization()`.
7. `AddEndpointsApiExplorer()` + `AddSwaggerGen()` (existing, with a JWT bearer security definition for later).
8. JWT signing-key startup guard (existing — keep verbatim).

Middleware/app pipeline (`app`):
1. **`app.UseExceptionHandler()`** (must be first so it wraps everything).
2. Dev-only: auto-migrate (existing) → **`await DevDataSeeder.SeedAsync(scope.ServiceProvider)`** (after migrate, same dev scope).
3. Dev-only: `UseSwagger()` + `UseSwaggerUI()` (existing).
4. `UseSerilogRequestLogging()` (existing).
5. **`UseAuthentication()`** → **`UseAuthorization()`**.
6. `MapHealthChecks("/health")` (existing).
7. **`app.MapClienteEndpoints(); app.MapIngredienteEndpoints(); ... app.MapStockEndpoints();`**
8. `app.Run()`.

---

## 2. Use-case catalogue — LOCKED command/query + handler signatures

> Pattern is the **`CrearFacturaHandler` precedent**: a `public sealed class XHandler` with constructor-injected ports, a single `public async Task<T> Handle(XCommand cmd, CancellationToken ct = default)` method, throwing `DomainException`/`ConflictException`, calling `_uow.SaveChangesAsync(ct)` on writes. Commands/queries are `sealed record`s. Every name/param below is anchored on the real domain factory.

> **Not-found contract (no mediator):** query handlers return `T?` (nullable) — the endpoint maps `null` → `TypedResults.NotFound()`. Command handlers that operate on an existing aggregate (AgregarLinea, ConfirmarPrecio, Transicionar, RegistrarPago) throw `ConflictException` when the aggregate is missing (→ 404 is signalled by the handler returning a sentinel OR by a dedicated `NotFoundException`; see §5 decision). See §5 for the exact not-found mechanism.

### 2a. Catalogue (PR 2)

| Use case | Command/Query record | Handler signature | Domain call (REAL) |
|----------|---------------------|-------------------|--------------------|
| CrearCliente | `CrearClienteCommand(string Nombre, CondicionIVA CondicionIVA, string? Cuit, string? Email)` | `Task<Guid> Handle(CrearClienteCommand, CT)` | `Cliente.Crear(nombre, condicionIVA, cuit is null ? null : new Cuit(cuit), email is null ? null : new Email(email))` → `IClienteRepository.AddAsync` → `uow.SaveChangesAsync` → `cliente.Id` |
| GetClienteById | `GetClienteByIdQuery(Guid Id)` | `Task<Cliente?> Handle(...)` | `IClienteRepository.GetByIdAsync(Id, ct)` |
| GetAllClientes | `GetAllClientesQuery()` | `Task<IReadOnlyList<Cliente>> Handle(...)` | `IClienteRepository.GetAllAsync(ct)` (new port, §8) |
| CrearIngrediente | `CrearIngredienteCommand(string Nombre, UnidadDeMedida UnidadBase)` | `Task<Guid> Handle(...)` | `Ingrediente.Crear(nombre, unidadBase)` → `AddAsync` → save |
| GetIngredienteById | `GetIngredienteByIdQuery(Guid Id)` | `Task<Ingrediente?> Handle(...)` | `IIngredienteRepository.GetByIdAsync` |
| GetAllIngredientes | `GetAllIngredientesQuery()` | `Task<IReadOnlyList<Ingrediente>> Handle(...)` | `IIngredienteRepository.GetAllAsync` (§8) |
| CrearPlato | `CrearPlatoCommand(string Nombre, decimal PrecioBase, AlicuotaIVA AlicuotaIVA, IReadOnlyList<RecetaLineaInput> Lineas)` where `RecetaLineaInput(Guid IngredienteId, decimal Cantidad, UnidadDeMedida Unidad)` | `Task<Guid> Handle(...)` | `Plato.Crear(nombre, new Dinero(precioBase), alicuotaIVA)`; then `foreach line: plato.AgregarLineaReceta(line.IngredienteId, new Cantidad(line.Cantidad, line.Unidad))` → `AddAsync` → save |
| GetPlatoById | `GetPlatoByIdQuery(Guid Id)` | `Task<Plato?> Handle(...)` | `IPlatoRepository.GetByIdAsync` |
| GetAllPlatos | `GetAllPlatosQuery()` | `Task<IReadOnlyList<Plato>> Handle(...)` | `IPlatoRepository.GetAllAsync` (§8) |
| CrearMenu | `CrearMenuCommand(string Nombre, DateOnly FechaVigencia, IReadOnlyList<MenuItemInput> Items)` where `MenuItemInput(Guid PlatoId, decimal? PrecioOverride)` | `Task<Guid> Handle(...)` | `Menu.Crear(nombre, fechaVigencia)`; `foreach item: menu.AgregarItem(item.PlatoId, item.PrecioOverride is null ? null : new Dinero(item.PrecioOverride.Value))` → `AddAsync` → save. **NOTE: real param is `FechaVigencia` (not `fechaMenu`); guard requires future date.** |
| GetMenuById | `GetMenuByIdQuery(Guid Id)` | `Task<Menu?> Handle(...)` | `IMenuRepository.GetByIdAsync` |
| GetAllMenus | `GetAllMenusQuery()` | `Task<IReadOnlyList<Menu>> Handle(...)` | `IMenuRepository.GetAllAsync` (§8) |
| CrearMesa | `CrearMesaCommand(int Numero, int Capacidad)` | `Task<Guid> Handle(...)` | `Mesa.Crear(numero, capacidad)` → `AddAsync` → save |
| GetMesaById | `GetMesaByIdQuery(Guid Id)` | `Task<Mesa?> Handle(...)` | `IMesaRepository.GetByIdAsync` |
| GetAllMesas | `GetAllMesasQuery()` | `Task<IReadOnlyList<Mesa>> Handle(...)` | `IMesaRepository.GetAllAsync` (§8) |

### 2b. Transactional (PR 3)

| Use case | Command/Query record | Handler signature | Domain call (REAL) |
|----------|---------------------|-------------------|--------------------|
| CrearPedido | `CrearPedidoCommand(TipoPedido Tipo, Guid? MesaId, Guid? ClienteId, DireccionEntregaInput? DireccionEntrega)` where `DireccionEntregaInput(string Calle, string Numero, string Ciudad, string Provincia, string CodigoPostal, string? Piso, string? Departamento)` | `Task<Guid> Handle(...)` | `Pedido.Crear(Tipo, MesaId, ClienteId, dir, DateTime.UtcNow)`; **if `Tipo == Salon`**: load `IMesaRepository.GetByIdAsync(MesaId)` (→ ConflictException if missing), `mesa.AsignarPedido(pedido.Id)`. Persist pedido (+ mesa) → save → `pedido.Id`. App-layer injects `creadoEnUtc = DateTime.UtcNow` (matches existing testability seam). |
| AgregarLinea | `AgregarLineaCommand(Guid PedidoId, Guid PlatoId, int Cantidad, string? Observaciones)` | `Task<Guid> Handle(...)` returns new `LineaPedido.Id` | load `IPedidoRepository.GetByIdAsync` (ConflictException if missing); `var linea = pedido.AgregarLinea(PlatoId, Cantidad, Observaciones)` → save → `linea.Id` |
| ConfirmarPrecioLinea | `ConfirmarPrecioLineaCommand(Guid PedidoId, Guid LineaId)` | `Task Handle(...)` | load pedido (ConflictException if missing); find `linea` by `LineaId` in `pedido.Lineas` (ConflictException if missing); resolve price for the line's `PlatoId` via the **now-async** service: `var (precio, iva) = await _precios.ResolverPrecioEfectivoAsync(linea.PlatoId, DateOnly.FromDateTime(pedido.CreadoEnUtc), ct);` then `linea.ConfirmarPrecio(precio, iva)` → save. **This is the live W-01 path (§4).** |
| TransicionarEstadoPedido | `TransicionarEstadoPedidoCommand(Guid PedidoId, EstadoPedido EstadoNuevo, RolUsuario Rol)` | `Task Handle(...)` | load pedido (ConflictException if missing); `pedido.TransicionarEstado(EstadoNuevo, Rol)` → save. **`Rol` arrives in the request body — temporary security seam (§10, Phase-5 closes it).** |
| GetPedidoById | `GetPedidoByIdQuery(Guid Id)` | `Task<Pedido?> Handle(...)` | `IPedidoRepository.GetByIdAsync` |

### 2c. Fiscal / Stock (PR 3)

| Use case | Command/Query record | Handler signature | Domain call (REAL) |
|----------|---------------------|-------------------|--------------------|
| CrearFactura | `CrearFacturaCommand(Guid ClienteId, IReadOnlyList<Guid> PedidoIds, TipoComprobanteSolicitado Tipo)` **(EXISTS — unchanged)** | `Task<Guid> Handle(...)` **(EXISTS)** | wire endpoint only; no handler change |
| RegistrarPago | `RegistrarPagoCommand(Guid FacturaId, decimal Monto, MetodoPago MetodoPago)` | `Task Handle(...)` | load `IFacturaRepository.GetByIdAsync` (ConflictException if missing); `factura.RegistrarPago(new Dinero(Monto), MetodoPago, DateTime.UtcNow)` → save |
| GetFacturaById | `GetFacturaByIdQuery(Guid Id)` | `Task<Factura?> Handle(...)` | `IFacturaRepository.GetByIdAsync` |
| RegistrarMovimientoStock | `RegistrarMovimientoStockCommand(Guid IngredienteId, TipoMovimientoStock Tipo, decimal Cantidad, Guid? OrdenTrabajoId, Guid? LineaPedidoId)` | `Task<Guid> Handle(...)` | `MovimientoStock.RegistrarMovimiento(IngredienteId, Tipo, Cantidad, OrdenTrabajoId, LineaPedidoId)` → `IMovimientoStockRepository.AddAsync` → save → `mov.Id`. (Caller passes absolute Cantidad; factory applies sign per Tipo.) |
| GetBalanceStock | `GetBalanceStockQuery(Guid IngredienteId)` | `Task<decimal> Handle(...)` | `IMovimientoStockRepository.CalcularBalanceAsync(IngredienteId, ct)` |

> `RegistrarMovimientoStock` exposes the general factory `RegistrarMovimiento` (not `RegistrarCompra`) because the endpoint takes a `Tipo`; lot/expiry traceability (`RegistrarCompra`) is a later enrichment, out of scope here.

---

## 3. DTO ↔ command/query ↔ response mapping (per endpoint)

**Rule (LOCKED): aggregates NEVER cross the wire.** Request DTOs are flat records → mapped to commands. Response DTOs are flat read models built by hand from the aggregate inside the mapping extension. VOs are flattened to primitives (`Dinero`→`decimal Monto` + `string Moneda`; `PorcentajeIVA`→`decimal Tasa` + `AlicuotaIVA`; `Cuit`→`string`; `Email`→`string`).

### Endpoint surface (HTTP verb → DTO → response)

| Group | Verb + route | Request DTO | Response DTO (flat) | TypedResult |
|-------|--------------|-------------|---------------------|-------------|
| `/clientes` | POST | `CrearClienteRequest(Nombre, CondicionIVA, Cuit?, Email?)` | — | `Created<Guid>` (location `/clientes/{id}`) |
| | GET `/{id}` | — | `ClienteResponse(Id, Nombre, CondicionIVA, Cuit?, Email?, Activo)` | `Ok<ClienteResponse>` / `NotFound` |
| | GET | — | `IReadOnlyList<ClienteResponse>` | `Ok<...>` |
| `/ingredientes` | POST / GET{id} / GET | `CrearIngredienteRequest(Nombre, UnidadBase)` | `IngredienteResponse(Id, Nombre, UnidadBase, Activo)` | `Created<Guid>` / `Ok` / `NotFound` |
| `/platos` | POST / GET{id} / GET | `CrearPlatoRequest(Nombre, PrecioBase, AlicuotaIVA, RecetaLineaRequest[])`; `RecetaLineaRequest(IngredienteId, Cantidad, Unidad)` | `PlatoResponse(Id, Nombre, PrecioBase, Moneda, AlicuotaIVA, Activo, RecetaLineaResponse[])`; `RecetaLineaResponse(Id, IngredienteId, Cantidad, Unidad)` | `Created<Guid>` / `Ok` / `NotFound` |
| `/menus` | POST / GET{id} / GET | `CrearMenuRequest(Nombre, FechaVigencia, MenuItemRequest[])`; `MenuItemRequest(PlatoId, PrecioOverride?)` | `MenuResponse(Id, Nombre, FechaVigencia, Activo, MenuItemResponse[])`; `MenuItemResponse(Id, PlatoId, PrecioOverride?)` | `Created<Guid>` / `Ok` / `NotFound` |
| `/mesas` | POST / GET{id} / GET | `CrearMesaRequest(Numero, Capacidad)` | `MesaResponse(Id, Numero, Capacidad, Estado, Activa, PedidoActivoId?)` | `Created<Guid>` / `Ok` / `NotFound` |
| `/pedidos` | POST | `CrearPedidoRequest(Tipo, MesaId?, ClienteId?, DireccionEntregaRequest?)` | — | `Created<Guid>` |
| | POST `/{id}/lineas` | `AgregarLineaRequest(PlatoId, Cantidad, Observaciones?)` | — | `Created<Guid>` (line id) |
| | POST `/{id}/lineas/{lineaId}/confirmar-precio` | — | — | `NoContent` / `NotFound` |
| | POST `/{id}/transiciones` | `TransicionarEstadoRequest(EstadoNuevo, Rol)` | — | `NoContent` |
| | GET `/{id}` | — | `PedidoResponse(Id, Tipo, Estado, MesaId?, ClienteId?, DireccionEntrega?, CreadoEnUtc, LineaPedidoResponse[])`; `LineaPedidoResponse(Id, PlatoId, Cantidad, Observaciones?, PrecioUnitario?, Moneda?, IvaTasa?, SubtotalLinea?, TotalLinea?)` | `Ok` / `NotFound` |
| `/facturas` | POST | `CrearFacturaRequest(ClienteId, PedidoIds, Tipo)` | — | `Created<Guid>` |
| | POST `/{id}/pagos` | `RegistrarPagoRequest(Monto, MetodoPago)` | — | `NoContent` / `NotFound` |
| | GET `/{id}` | — | `FacturaResponse(Id, TipoComprobante, Estado, ClienteId, FechaAlta, SubTotal, TotalIVA, Total, TotalPagado, EstaPagada, CAE?, VencimientoCAE?, FacturaLineaResponse[], PagoResponse[])` | `Ok` / `NotFound` |
| `/stock` | POST `/movimientos` | `RegistrarMovimientoStockRequest(IngredienteId, Tipo, Cantidad, OrdenTrabajoId?, LineaPedidoId?)` | — | `Created<Guid>` |
| | GET `/balance/{ingredienteId}` | — | `BalanceStockResponse(IngredienteId, Balance)` | `Ok` |

> Request DTO and Command are intentionally near-duplicates but kept separate: the DTO is the HTTP contract (validated, stable for clients), the command is the application contract (constructs VOs). `ToCommand()` in Contracts bridges them. This is the documented two-record pattern; do NOT collapse them into one type.

---

## 4. W-01 — async `IEfectivoPrecioService` (LOCKED — Option A, PR 1)

### 4a. New interface signature (`src/GastroGestion.Domain/Services/IEfectivoPrecioService.cs`)

```csharp
public interface IEfectivoPrecioService
{
    Task<(Dinero Precio, PorcentajeIVA IVA)> ResolverPrecioEfectivoAsync(
        Guid platoId, DateOnly fecha, CancellationToken ct = default);
}
```

- **Real current name is `ResolverPrecioEfectivo` (sync).** The async version is `ResolverPrecioEfectivoAsync` with a trailing `CancellationToken`.
- `Task` is BCL → Domain keeps **zero** `PackageReference`/`ProjectReference`. **Gate before merge:** review `GastroGestion.Domain.csproj` shows no refs.

### 4b. Implementation rewrite (`src/GastroGestion.Application/Services/EfectivoPrecioService.cs`)

```csharp
public async Task<(Dinero Precio, PorcentajeIVA IVA)> ResolverPrecioEfectivoAsync(
    Guid platoId, DateOnly fecha, CancellationToken ct = default)
{
    var plato = await _platos.GetByIdAsync(platoId, ct)
        ?? throw new InvalidOperationException($"Plato {platoId} not found.");
    var iva = new PorcentajeIVA(plato.AlicuotaIVA);

    var menus = await _menus.GetActivosByFechaAsync(fecha, ct);
    var overridePrice = menus
        .SelectMany(m => m.Items)
        .Where(it => it.PlatoId == platoId && it.PrecioOverride is not null)
        .Select(it => it.PrecioOverride)
        .FirstOrDefault();

    var precio = overridePrice ?? plato.PrecioBase;
    return (precio, iva);
}
```

- **No `.GetAwaiter().GetResult()`** anywhere. Both repo calls `await`ed directly. The deadlock is structurally gone.
- Remove the stale XML comment that said "the domain interface is synchronous … blocking calls".

### 4c. Every call site that changes

| Call site | Before | After |
|-----------|--------|-------|
| `EfectivoPrecioService` (impl itself) | sync method, blocking | async method, awaits (§4b) |
| `IEfectivoPrecioService` (interface) | sync signature | async signature (§4a) |
| **`ConfirmarPrecioLineaHandler`** (NEW, PR 3) | n/a — created against async | `var (p, iva) = await _precios.ResolverPrecioEfectivoAsync(linea.PlatoId, DateOnly.FromDateTime(pedido.CreadoEnUtc), ct); linea.ConfirmarPrecio(p, iva);` |
| Application unit tests (existing, if any reference the sync method) | sync mock setup | async mock setup (`ReturnsAsync`) |

> **Search gate (PR 1):** `rg "ResolverPrecioEfectivo"` across `src/` + `tests/` must show only the async name after the change. The interface is currently injected nowhere live (only registered in DI), so the blast radius is the impl + the new handler + tests — confirming the proposal's "harmless today" claim.

> The `fecha` passed in `ConfirmarPrecioLinea` is `DateOnly.FromDateTime(pedido.CreadoEnUtc)` — the order's creation date drives which active menu applies, consistent with `GetActivosByFechaAsync(fecha)` returning menus with `FechaVigencia >= fecha`.

---

## 5. Error handling — `GastroGestionExceptionHandler : IExceptionHandler` (LOCKED, PR 1)

`Api/ErrorHandling/GastroGestionExceptionHandler.cs`, registered via `AddExceptionHandler<>()` + `AddProblemDetails()`; `app.UseExceptionHandler()` first in the pipeline.

### 5a. Mapping table

| Exception | HTTP status | ProblemDetails.Title | Notes |
|-----------|-------------|----------------------|-------|
| `ConflictException` (Application) | **409** Conflict | "Business rule conflict" | thrown by handlers for cross-aggregate/orchestration conflicts |
| `NotFoundException` (NEW, Application) | **404** Not Found | "Resource not found" | see §5b — explicit not-found for write paths |
| `DomainException` (Domain) | **422** Unprocessable Entity | "Domain rule violation" | aggregate/VO invariant broken |
| anything else | **500** Internal Server Error | "An unexpected error occurred" | log full detail via Serilog; do NOT leak `ex.Message` to the body |

`detail` = `ex.Message` for the three mapped types (these messages are domain-authored and safe). For 500, `detail` is a generic string; the real exception is logged.

### 5b. Not-found mechanism decision (NEW — resolves a §2 open point)

Two not-found shapes exist; we standardize:

- **Query (GET) paths:** the handler returns `T?`; the **endpoint** maps `null → TypedResults.NotFound()`. No exception. (e.g. `GetClienteById`, `GetPedidoById`.) ProblemDetails 404 is produced by `TypedResults.NotFound()` + `AddProblemDetails()`.
- **Write paths operating on an existing aggregate** (AgregarLinea, ConfirmarPrecio, Transicionar, RegistrarPago, CrearPedido-with-Mesa): the handler throws a new **`NotFoundException : Exception`** in `Application/Common/Exceptions/` when the target aggregate/line is missing. The exception handler maps it to 404.

> **Decision:** add `NotFoundException` (sibling to `ConflictException`) rather than overloading `ConflictException` for missing aggregates. Reason: a missing Pedido on `AgregarLinea` is semantically 404, not 409. `ConflictException` stays reserved for genuine business conflicts (e.g. REQ-13-G mismatched clients, empty PedidoIds). This is a small additive type, lands in PR 1.

### 5c. Why `IExceptionHandler`, not middleware or Result

Native .NET 8, no package, RFC 7807 out of the box via `AddProblemDetails()`, endpoints stay free of try/catch. Result<T> rewrite is an explicit non-goal (would change every handler signature).

---

## 6. Validation — `ValidationFilter<T>` (LOCKED, PR 1)

### 6a. The generic endpoint filter (`Api/Filters/ValidationFilter.cs`)

```csharp
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;
    public ValidationFilter(IValidator<T> validator) => _validator = validator;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var arg = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (arg is null) return await next(ctx);

        var result = await _validator.ValidateAsync(arg, ctx.HttpContext.RequestAborted);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());
        return await next(ctx);
    }
}
```

- Applied per endpoint via `.AddEndpointFilter<ValidationFilter<CrearClienteRequest>>()`.
- `result.ToDictionary()` is FluentValidation's `ValidationResult` → `IDictionary<string,string[]>`, which `TypedResults.ValidationProblem(...)` turns into a 400 RFC 7807 `ValidationProblemDetails`. The filter runs **before** the handler — invalid requests never reach Application.
- A `WithValidation<T>()` extension wraps `.AddEndpointFilter<ValidationFilter<T>>()` for readable registration.

### 6b. Validator placement & registration (LOCKED)

- Validators (`AbstractValidator<CrearClienteRequest>` etc.) live **in `Contracts`**, next to their DTO (e.g. `Contracts/Clientes/ClienteValidators.cs`). Reason: a validator is part of the request contract; co-location keeps DTO + rules + mapping together; the filter is generic and lives in Api.
- Registered once in `Program.cs`: `builder.Services.AddValidatorsFromAssemblyContaining<CrearClienteRequest>()` (scans the whole Contracts assembly).
- FluentValidation packages (`FluentValidation` + `FluentValidation.DependencyInjectionExtensions`) added to **Contracts** (validators) and the DI extension; Api references Contracts transitively.

### 6c. What validators check vs what the domain checks

Validators handle **shape/format** at the boundary (required strings, positive numbers, non-empty collections, CUIT-required-when-RI at request level for a friendly 400). Domain factories remain the **invariant authority** (422 if bypassed). Validators are a UX layer, never the security/consistency layer — duplicate-but-friendlier checks are acceptable.

---

## 7. DevDataSeeder (LOCKED, PR 1)

`src/GastroGestion.Infrastructure/Persistence/Seed/DevDataSeeder.cs`. Static `Task SeedAsync(IServiceProvider sp)`; resolves the repos + `IUnitOfWork` + `GastroGestionDbContext` from the passed dev scope.

### 7a. Structure

```
1. Resolve GastroGestionDbContext (idempotency probe) + repos + IUnitOfWork from sp.
2. if (await db.Clientes.AnyAsync()) return;             // idempotency guard
3. Build aggregates via domain factories (invariants hold, events fire).
4. AddAsync each through its repository.
5. await uow.SaveChangesAsync();  (one commit; post-commit dispatcher fires events)
```

Called from `Program.cs` dev block **after** `MigrateAsync`, inside the same `app.Services.CreateScope()` scope (so the scoped DbContext/repos are the same instance graph).

### 7b. Sample data (built through factories — exact)

| Aggregate | Count | Construction detail |
|-----------|-------|---------------------|
| Cliente | 3 | `Crear("Consumidor Demo", CondicionIVA.ConsumidorFinal, null, null)`; `Crear("RI Demo", CondicionIVA.ResponsableInscripto, new Cuit("<valid-CUIT>"), new Email("ri@demo.test"))`; `Crear("Exento Demo", CondicionIVA.ExentoIVA, null, null)`. **NOTE: real enum value is `ExentoIVA`, not `Exento`.** Use a checksum-valid CUIT (e.g. `30-71659554-9` style — must pass `Cuit` check digit). |
| Ingrediente | 5 | varied `UnidadDeMedida` (Gramo, Kilogramo, Litro, Unidad, Mililitro). |
| Plato | 3 | `Plato.Crear(nombre, new Dinero(precio), AlicuotaIVA.General)` + `AgregarLineaReceta(ingredienteId, new Cantidad(qty, unidad))` referencing seeded ingredients. |
| Menu | 1 | `Menu.Crear("Menú del Día", tomorrow)` where `tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)`; `AgregarItem(plato1.Id, null)` + `AgregarItem(plato2.Id, new Dinero(<override>))` (one override). |
| Mesa | 4 | `Mesa.Crear(1,2)`, `(2,4)`, `(3,4)`, `(4,6)`. |
| Pedido (Salon) | 1 | `Pedido.Crear(TipoPedido.Salon, mesa1.Id, cliente1.Id, null, DateTime.UtcNow)`; `mesa1.AsignarPedido(pedido.Id)`; `AgregarLinea(plato.Id, 2)`; confirm each line's price via `EfectivoPrecioService` (now async) then `linea.ConfirmarPrecio(...)`. |
| Pedido (TakeAway) | 1 | `Pedido.Crear(TipoPedido.TakeAway, null, cliente1.Id, null, DateTime.UtcNow)` + line + confirmed price. |
| Factura | 1 | `Factura.CrearTicket(cliente1.Id, [takeAwayPedido.Id], lineas)` where `lineas` built from the TakeAway pedido's confirmed lines (mirror `CrearFacturaHandler.BuildLineasFromPedidos`). |

> The seeder must `await` the async `EfectivoPrecioService` (W-01 already landed in PR 1, so the seeder uses the async method natively — no sync-over-async re-introduction).

### 7c. Future-date idempotency edge (documented mitigation)

`Menu.Crear` requires a future `FechaVigencia`. The seeder computes `tomorrow` dynamically at run time, so the first seed always succeeds. But the idempotency guard (`Clientes.AnyAsync`) means a DB seeded on day N keeps a Menu with `FechaVigencia = N+1`; on day N+2 that menu is stale and `GetActivosByFechaAsync(today)` returns nothing. **Mitigation:** document a reseed path — drop the dev DB (`dotnet ef database drop` or delete the LocalDB `.mdf`) to refresh. `Api.Tests` seed fresh per run via a dedicated test DB, so tests are unaffected. (No `--reseed` switch in this phase — documented manual reset only.)

### 7d. Gating

Seeder runs **only** inside `if (app.Environment.IsDevelopment())`. Never in CI/prod. The idempotency guard is a second safety net.

---

## 8. `GetAllAsync` port + impl additions (LOCKED, PR 2)

Ports that gain `GetAllAsync` (the five **catalogue** aggregates exposed via GET-all; transactional/fiscal GET-all is out of scope this phase — only GET-by-id):

| Port | New method | Impl |
|------|-----------|------|
| `IClienteRepository` | `Task<IReadOnlyList<Cliente>> GetAllAsync(CT ct = default)` | `(await _ctx.Clientes.ToListAsync(ct)).AsReadOnly()` |
| `IIngredienteRepository` | same shape | `_ctx.Ingredientes.ToListAsync` |
| `IPlatoRepository` | same shape | `_ctx.Platos.ToListAsync` — owned `LineasReceta` auto-load (configured `OwnsMany` + field access) |
| `IMenuRepository` | same shape | `_ctx.Menus.ToListAsync` — owned `Items` auto-load |
| `IMesaRepository` | same shape | `_ctx.Mesas.ToListAsync` |

- **Loading contract:** owned-entity collections are loaded automatically by EF for owned types (confirmed in `PlatoConfiguration` — `OwnsMany` + `Navigation(...).UsePropertyAccessMode(Field)`), so `.ToListAsync()` returns full graphs with no explicit `.Include()`. This matches the existing `GetByIdAsync` contract.
- No pagination/filtering/sorting (non-goal). Full set returned — dev-scale only.
- N+1 risk is low/med; mirror the existing eager owned-load strategy. If `AsSplitQuery` is needed for owned collections at scale, that is a later concern.

---

## 9. `Api.Tests` — WebApplicationFactory harness (LOCKED)

`tests/GastroGestion.Api.Tests/` — xUnit + FluentAssertions (matches existing test stack) + `Microsoft.AspNetCore.Mvc.Testing`.

### 9a. Factory

- `ApiFactory : WebApplicationFactory<Program>` overriding `ConfigureWebHost` to set `Environment = "Development"` (so seeder + auto-migrate run) and point `ConnectionStrings:GastroGestion` + `Jwt:SigningKey` at test config (a dedicated LocalDB test database + an inline dev signing key) so the startup guard passes.
- All tests `[Trait("Category","Integration")]` so CI can `--filter Category!=Integration` when LocalDB is absent.
- `public partial class Program { }` added to `Program.cs`.

### 9b. Smoke tests (PR 1)

- `GET /health` → 200.
- One write+read round-trip is deferred to PR 2 (no catalogue endpoint exists in PR 1); PR 1 smoke = health + app boots with seeder + ProblemDetails wired (a deliberately-bad request to `/health`-adjacent or a forced unhandled path returns a ProblemDetails shape). Minimal but real (boots the full pipeline incl. migrate + seed).

### 9c. Endpoint tests (PR 2–3)

- PR 2: `POST /clientes` → 201 + `GET /clientes/{id}` → 200 matching shape; `GET /clientes` → contains seeded clients; invalid `POST` → 400 ValidationProblem; RI without CUIT → 422 (domain) or 400 (validator).
- PR 3: `POST /pedidos` (Salon) → 201; add line → confirm price (exercises W-01 on the live HTTP stack — the deadlock regression test) → factura ticket; `RegistrarPago` → 204; `GetBalanceStock` after a movimiento.

---

## 10. Security posture (time-boxed, Phase-5 closes)

| Hole | Status | Phase-5 remediation |
|------|--------|---------------------|
| All endpoints `[AllowAnonymous]` | deliberate, pipeline wired but unprotected | remove `[AllowAnonymous]` once login + `Usuario` exist |
| `RolUsuario` from request body on `TransicionarEstadoPedido` | deliberate authorization bypass | replace `cmd.Rol` source with `User.FindFirst(ClaimTypes.Role)` extracted from the JWT |

**Seam marker:** in `TransicionarEstadoPedidoHandler` and `PedidoEndpoints` add a `// PHASE-5: replace body-supplied Rol with JWT claim` comment at the exact line where `Rol` enters from the body, so the security debt is greppable.

JWT pipeline IS wired now (`AddAuthentication(JwtBearer)` + `UseAuthentication` + `UseAuthorization`) using the existing `Jwt:Issuer/Audience/SigningKey` config + startup guard — Phase 5 only adds the login endpoint and removes `[AllowAnonymous]`.

---

## 11. Package changes (LOCKED, PR 1)

| Project | Add | Remove |
|---------|-----|--------|
| `GastroGestion.Api` | `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.x — pin to match the SDK, not implicitly in `Microsoft.NET.Sdk.Web`) | **`Microsoft.AspNetCore.OpenApi` 8.0.27** (redundant with Swashbuckle; the .NET-9-native package on a net8 project) |
| `GastroGestion.Contracts` | `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`; ProjectReference → Application | — |
| `GastroGestion.Api.Tests` (new) | `Microsoft.AspNetCore.Mvc.Testing`, `xunit`, `FluentAssertions`, `Microsoft.NET.Test.Sdk` | — |

> Swashbuckle 6.6.2 stays, Swagger dev-only (existing). Confirm `JwtBearer` is added explicitly — `Microsoft.NET.Sdk.Web` does NOT include the JwtBearer authentication handler by default on net8.

---

## 12. Delivery — 3 chained slices (stacked-to-main)

```
PR1 (foundation) ──► PR2 (catalogue) ──► PR3 (transactional/fiscal/stock)
   W-01, errors,        handlers+DTOs+        Pedido lifecycle,
   validation,          validators+GetAll      Factura/Pago, Stock
   seeder, auth,        (5 catalogue groups)   (3 groups)
   Api.Tests smoke
```

- **PR 1** is foundational — every endpoint depends on it. W-01 MUST be here (before any price path). ~150–250 lines.
- **PR 2** depends on PR 1; may exceed 400 lines (accepted under chained plan).
- **PR 3** depends on PR 1–2; may exceed 400 lines; highest product risk (price/fiscal/stock), isolated last.
- Each slice green on LocalDB; each independently revertible (revert PR 3 doesn't touch PR 1–2).

---

## 13. Risks & mitigations

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| **W-01 deadlock** if a price path ships before the async fix | High (if unfixed) | async fix in **PR 1**; `rg "ResolverPrecioEfectivo"` gate confirms no sync name remains; PR 3 integration test hits ConfirmarPrecio on the real HTTP stack |
| **Menu seeder stale future-date** breaks `GetActivosByFecha` on later days | Med | compute `tomorrow` dynamically at seed time; document drop-DB reseed; tests seed fresh per run |
| **`RolUsuario` from body** = authorization bypass | High (by design, time-boxed) | greppable `// PHASE-5` seam marker; Phase 5 swaps to JWT claim; only the `[AllowAnonymous]` dev window is exposed |
| **Redundant OpenAPI packages** cause subtle behaviour diff | Low | remove `Microsoft.AspNetCore.OpenApi` in PR 1; keep Swashbuckle only |
| **LocalDB required for `Api.Tests`** complicates CI | Low | `[Trait("Category","Integration")]` skip filter; document LocalDB prereq; Testcontainers is the future upgrade |
| **Domain gains a framework dependency** via W-01 | Low | `Task` is BCL; review `Domain.csproj` for zero refs before merge |
| **Aggregate leaks over the wire** | Low | DTO-only contract; response DTOs hand-built; integration tests assert flat shapes; review gate on endpoint return types |
| **`GetAllAsync` over-fetch / N+1** on owned graphs | Low/Med | mirror existing owned-entity eager load; dev-scale data; pagination is a non-goal |
| **`CondicionIVA.Exento` does not exist** (real value is `ExentoIVA`) | Med (drift trap) | locked in §2a/§7b; seeder + validators use `ExentoIVA`; `Menu` param is `FechaVigencia` not `fechaMenu` |
| **Contracts→Application reference introduces a cycle** | Low | Contracts is a leaf consumed only by Api; no project references back into Contracts — verified, no cycle |

---

## 14. ADR-style decisions (rationale + rejected alternatives)

| # | Decision | Rationale | Rejected |
|---|----------|-----------|----------|
| D1 | Minimal APIs, `RouteGroupBuilder` per aggregate, `TypedResults`, plain handler injection (no mediator) | net8-idiomatic; mirrors `CrearFacturaHandler`; explicit OpenAPI signatures; zero indirection | MVC controllers (heavier); MediatR (package + magic, non-goal) |
| D2 | `IExceptionHandler` + `AddProblemDetails()` for RFC 7807 | native net8, no package, endpoints stay try/catch-free | exception middleware (boilerplate); Result<T> (rewrites every handler, non-goal) |
| D3 | `NotFoundException` added (sibling to `ConflictException`); GET handlers return `T?` mapped to 404 | a missing aggregate on a write is 404 not 409; keeps `ConflictException` for true conflicts | overloading `ConflictException` for not-found (wrong status semantics) |
| D4 | W-01 Option A — async interface | eliminates deadlock cleanly; `Task` is BCL so Domain stays zero-dep; lowest effort | Option B pre-load-in-handler (more indirection; deferred as future refactor) |
| D5 | DTOs + validators + mapping in `Contracts`; Contracts → Application reference | co-locates contract concerns; compile-time-safe manual mapping; no AutoMapper | DTOs in Application (can't reference Api shapes); AutoMapper (runtime errors, non-goal) |
| D6 | `ValidationFilter<T>` generic endpoint filter → `ValidationProblem` (400) | reusable, runs before handler, native ProblemDetails | DataAnnotations (pollutes DTO); manual in-handler validation (repetitive) |
| D7 | Runtime `DevDataSeeder` via factories+repos, idempotent, dev-only | invariants hold, events fire, trivial idempotency | EF `HasData` (fixed Guids, bypasses invariants, can't satisfy Menu future-date) |
| D8 | `GetAllAsync` on the 5 catalogue ports only | seeded catalogue must be browsable; transactional/fiscal GET-all not needed this phase | GetAll on all 8 (scope creep); no GetAll (seeded data not browsable) |
| D9 | Keep Swashbuckle; remove `Microsoft.AspNetCore.OpenApi`; Swagger dev-only | already wired, richer on net8; remove redundancy | keep both (conflict); switch to native AddOpenApi (partial on net8) |
| D10 | JWT pipeline wired, all endpoints `[AllowAnonymous]`; `RolUsuario` from body (seam) | establishes pipeline so Phase 5 only removes attribute + adds login; no `Usuario` aggregate needed now | full login+Usuario (out of scope); no pipeline at all (Phase 5 does double work) |

---

## 15. Checklist (reviewer can confirm)

- [ ] `IEfectivoPrecioService.ResolverPrecioEfectivoAsync` is async; impl has no `.GetAwaiter().GetResult()`; `Domain.csproj` has zero refs.
- [ ] `GastroGestionExceptionHandler` maps Conflict→409, NotFound→404, Domain→422, else→500 (RFC 7807).
- [ ] `ValidationFilter<T>` returns 400 `ValidationProblem`; validators registered from Contracts assembly.
- [ ] All DTOs flat (no VO/aggregate); response read-models hand-built; `ToCommand`/`ToResponse` in Contracts.
- [ ] Use-case handlers match the §2 signatures and call the REAL domain factories.
- [ ] `DevDataSeeder` dev-only, idempotent on `Clientes.AnyAsync`, Menu uses `tomorrow`, uses `ExentoIVA`.
- [ ] `GetAllAsync` added to the 5 catalogue ports + impls; owned graphs load.
- [ ] `Microsoft.AspNetCore.OpenApi` removed; `JwtBearer` + FluentValidation added; Swagger dev-only.
- [ ] JWT pipeline wired; all endpoints `[AllowAnonymous]`; `// PHASE-5` seam markers present.
- [ ] `Api.Tests` boots `WebApplicationFactory<Program>` against LocalDB; `[Trait("Category","Integration")]`.

## Next step

Proceed to `sdd-tasks` (after spec is also ready). Tasks decompose each §2 use case + §3 endpoint + §4/§5/§6/§7 infrastructure into ordered, checkable implementation steps per slice.
