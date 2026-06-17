# Design: OrdenTrabajo (Kitchen) Workflow over the API

Expose the already-modeled `OrdenTrabajo` (kitchen "comanda") lifecycle through the API using
**Approach C (hybrid)**: aggregate-safe nested mutations + a flat top-level kitchen board read,
plus an additive SignalR realtime layer in PR2. No new migration; `GET /pedidos/{id}` is unchanged.

This design locks every signature against the real CLR types (file + line references inline) so the
tasks/apply phases have zero ambiguity. The only Domain edit is hardening `AsignarCocinero`.

## Quick path (what gets built)

1. **Domain hardening** — make `OrdenTrabajo.AsignarCocinero` `internal`; add
   `Pedido.AsignarCocineroAOT(Guid otId, LegajoId cocinero, RolUsuario rol)` so cook assignment always
   routes through the aggregate root.
2. **4 use cases** — `GenerarOrdenesTrabajo`, `AsignarCocinero`, `MarcarOrdenTrabajoLista` (commands +
   handlers, role-gated at Application layer mirroring `TransicionarEstadoPedidoHandler`), and
   `GetOrdenesByEstado` (query + handler, kitchen board).
3. **Repository** — add `GetAllOrdenesTrabajoAsync(EstadoOT? estado, CancellationToken)` to
   `IPedidoRepository`, implemented as a **flat projection** off the owned `PedidoOrdenesTrabajo` set
   (no full aggregate load).
4. **Contracts** — `OrdenTrabajo{Requests,Responses,Validators,Mappings}.cs` (new `OrdenTrabajoResponse`,
   flat `OrdenTrabajoBoardItem`). `PedidoResponse` is NOT touched.
5. **API (PR1)** — `OrdenTrabajoEndpoints.cs`: nested mutations under
   `/pedidos/{pedidoId}/ordenes-trabalho/...` + top-level board `GET /ordenes-trabalho?estado=`.
6. **Realtime (PR2)** — Application port `IKitchenNotifier`, an Infrastructure/API `KitchenHub` SignalR
   hub pushing the flat board item to the `"kitchen"` group, invoked by the three mutation handlers
   after a successful commit.

---

## Architecture position

- **Pattern**: Clean Architecture, command/handler-per-use-case (no MediatR — handlers are concrete
  classes resolved by DI, mirroring every existing Pedido use case).
- **Aggregate boundary**: `OrdenTrabajo` is an owned `Entity` of the `Pedido` aggregate root
  (`Pedido.cs:30,60`). Mutations MUST load and save through `Pedido`. The read board is the only place
  we bypass the root — and it does so read-only, by projection.
- **Layering for realtime**: SignalR is an outbound notification concern. The Application layer depends
  only on a port (`IKitchenNotifier`); the SignalR hub + adapter live in the API/Infrastructure layer.
  Application stays framework-free.
- **Role enforcement**: lives at the Application layer (the command carries `RolUsuario` extracted from
  the JWT `ClaimTypes.Role` claim at the endpoint), exactly like the existing
  `transicion` endpoint (`PedidoEndpoints.cs:63-69`). The Domain has no role check for OT mutations.

---

## Use-case flows

### UC-1 `GenerarOrdenesTrabajo` (explicit mozo action)

```
Endpoint POST /pedidos/{pedidoId}/ordenes-trabalho
  → extract Rol from ClaimTypes.Role (Mozo + Administrador allowed)   [role gate at Application]
  → GenerarOrdenesTrabajoHandler.Handle(cmd)
      1. pedido = _pedidos.GetByIdAsync(pedidoId)            ?? NotFoundException
      2. role gate: cmd.Rol in {Mozo, Administrador}         else ForbiddenException
      3. distinctPlatoIds = pedido.Lineas.Select(l => l.PlatoId).Distinct()
      4. platos = _platos.GetByIdsAsync(distinctPlatoIds)    -- NEW repo method (see LOCKED)
      5. EARLY-FAILURE: for each PlatoId, build snapshot from plato.LineasReceta.
         If a Plato is missing OR has empty LineasReceta
            → ValidationException("Plato {id} has no recipe lines; cannot generate OTs.")
         (fail BEFORE calling the domain so the invariant never throws opaquely)
      6. map: IReadOnlyDictionary<Guid /*PlatoId*/, IReadOnlyList<LineaRecetaSnapshot>>
            where snapshot line = new LineaRecetaSnapshot(lr.IngredienteId, lr.Cantidad)
      7. pedido.GenerarOrdenesTrabajo(snapshotsByPlato)      [domain: all-or-nothing, raises events]
      8. _uow.SaveChangesAsync()                              [commits + dispatches OrdenTrabajoCreada]
```

Notes locked against real code:
- The domain method keys recipes by `PlatoId` and validates price snapshots + duplicate OTs per
  `LineaPedidoId` (`Pedido.cs:227-262`). The handler only needs to provide one snapshot list per
  distinct `PlatoId`.
- `LineaReceta` exposes `IngredienteId` and `Cantidad` (`LineaReceta.cs:18,21`); `Plato.LineasReceta`
  is read-only (`Plato.cs:24`). `LineaRecetaSnapshot` is `record(Guid IngredienteId, Cantidad Cantidad)`
  (`LineaRecetaSnapshot.cs:14-16`).
- Existing `OrdenTrabajoCreada` events flow through `SaveChangesAsync` dispatch (DbContext.cs:43-50) —
  the stock-move side effect is unchanged.

### UC-2 `AsignarCocinero` (Cocinero + Administrador)

```
Endpoint POST /pedidos/{pedidoId}/ordenes-trabalho/{otId}/asignar-cocinero
  body: { cocineroLegajoId: Guid }
  → extract Rol (Cocinero + Administrador allowed)
  → AsignarCocineroHandler.Handle(cmd)
      1. pedido = _pedidos.GetByIdAsync(cmd.PedidoId)        ?? NotFoundException
      2. role gate: cmd.Rol in {Cocinero, Administrador}     else ForbiddenException
      3. pedido.AsignarCocineroAOT(cmd.OtId, new LegajoId(cmd.CocineroLegajoId), cmd.Rol)  [via root]
      4. _uow.SaveChangesAsync()
      5. (PR2) _kitchenNotifier.NotifyOtChangedAsync(boardItem(pedido, otId))
```

`AsignarCocineroAOT` finds the OT via the existing `GetOrdenTrabajoOrThrow` private helper
(`Pedido.cs:299-303`) and calls the now-`internal` `ot.AsignarCocinero(cocinero)`
(`OrdenTrabajo.cs:85-95`). No new domain event, no state regression — see ADR-001.

### UC-3 `MarcarOrdenTrabajoLista` (Cocinero + Administrador)

```
Endpoint POST /pedidos/{pedidoId}/ordenes-trabalho/{otId}/marcar-lista
  → extract Rol (Cocinero + Administrador allowed)
  → MarcarOrdenTrabajoListaHandler.Handle(cmd)
      1. pedido = _pedidos.GetByIdAsync(cmd.PedidoId)        ?? NotFoundException
      2. role gate: cmd.Rol in {Cocinero, Administrador}     else ForbiddenException
      3. pedido.MarcarOrdenTrabajoLista(cmd.OtId, cmd.Rol)   [domain auto-advances Pedido if all Lista]
      4. _uow.SaveChangesAsync()
      5. (PR2) _kitchenNotifier.NotifyOtChangedAsync(boardItem(pedido, otId))
```

The existing domain method already exists with the exact signature
`MarcarOrdenTrabajoLista(Guid ordenTrabajoId, RolUsuario rolCocinero)` (`Pedido.cs:269`) and
auto-advances non-Salon Pedidos via the role-gated transition path (`Pedido.cs:274-281`). The
Application `Rol` is forwarded straight into that call.

### UC-4 `GetOrdenesByEstado` (kitchen board — read-only)

```
Endpoint GET /ordenes-trabalho?estado=Creada|Preparandose|Lista|Cancelada   (estado optional)
  → GetOrdenesByEstadoHandler.Handle(query)
      1. items = _pedidos.GetAllOrdenesTrabajoAsync(query.Estado, ct)   -- flat projection
      2. return items                                                    -- already DTOs
```

No role gate beyond `RequireAuthorization()` (any authenticated user can view the board). The query
projects directly off `PedidoOrdenesTrabajo` (see ADR-002) — it never materializes a `Pedido`.

---

## Architecture Decision Records

### ADR-001 — `AsignarCocinero` internal + `Pedido.AsignarCocineroAOT` (ADOPT)

**Decision**: Make `OrdenTrabajo.AsignarCocinero(LegajoId)` `internal` and add a root method
`Pedido.AsignarCocineroAOT(Guid otId, LegajoId cocinero, RolUsuario rol)`.

**Context**: Today `AsignarCocinero` is `public` (`OrdenTrabajo.cs:85`), so a handler could mutate a
detached OT outside the aggregate. Every other OT write (`MarcarLista`, `Cancelar`) is already
`internal` and routed through `Pedido`. `Crear` is `internal` too (`OrdenTrabajo.cs:66`).

**Verification (no regression)**: Searched the solution — `AsignarCocinero` has **zero existing
callers** (no Application use case exists yet). Making it `internal` breaks nothing. The domain
transition logic (Creada → Preparandose, null-guard) stays byte-for-byte; only visibility changes. No
event was ever raised by this method, so there is no event regression. The new root method is a thin
delegation:

```csharp
// Pedido.cs — new method, mirrors MarcarOrdenTrabajoLista shape
public void AsignarCocineroAOT(Guid otId, LegajoId cocinero, RolUsuario rol)
{
    var ot = GetOrdenTrabajoOrThrow(otId);   // existing private helper, Pedido.cs:299
    ot.AsignarCocinero(cocinero);            // now internal — only Pedido can call
}
```

`rol` is accepted for signature symmetry with `MarcarOrdenTrabajoLista` and to keep the door open for a
future domain-side gate; in v1 it is not consulted inside the domain (the gate is at Application). This
is intentional and documented to avoid drift.

**Rejected alternative**: keep `AsignarCocinero` public and document a "handlers must load through the
root" convention. Rejected because the visibility is a free, compiler-enforced guarantee and the change
has no callers to break — a convention is weaker than a compiler boundary, and the rest of the entity
already follows the internal pattern.

### ADR-002 — Kitchen board via flat projection off the owned set (ADOPT)

**Decision**: Add `Task<IReadOnlyList<OrdenTrabajoBoardItem>> GetAllOrdenesTrabajoAsync(EstadoOT? estado,
CancellationToken)` to `IPedidoRepository`, implemented with a projecting LINQ query that selects
directly from the owned `PedidoOrdenesTrabajo` collection without loading `Pedido` aggregates.

**Context**: OTs are an owned collection (`Pedido.cs:60`, mapped to `PedidoOrdenesTrabajo`). There is no
`DbSet<OrdenTrabajo>` (DbContext.cs:19-20 comment). A cross-Pedido board read that loaded full `Pedido`
aggregates would pull lines, addresses, and every OT per order — wasteful and N+1-prone.

**Implementation** (verified against EF Core owned-collection querying):

```csharp
public async Task<IReadOnlyList<OrdenTrabajoBoardItem>> GetAllOrdenesTrabajoAsync(
    EstadoOT? estado, CancellationToken ct = default)
{
    var query = _ctx.Pedidos
        .AsNoTracking()
        .SelectMany(p => p.OrdenesTrabajo, (p, ot) => new { p, ot });

    if (estado is not null)
        query = query.Where(x => x.ot.Estado == estado);

    return await query
        .Select(x => new OrdenTrabajoBoardItem(
            x.ot.Id,
            x.p.Id,                       // PedidoId — needed for the nested mutation route
            x.p.Tipo,
            x.ot.PlatoId,
            x.ot.LineaPedidoId,
            x.ot.Estado,
            x.ot.CocineroAsignado != null ? x.ot.CocineroAsignado.Valor : (Guid?)null))
        .ToListAsync(ct);
}
```

`SelectMany` over an owned collection translates to a JOIN on `PedidoOrdenesTrabajo`; the projection
materializes only the columns the board needs. `RecetaSnapshot` (a JSON column) is intentionally NOT
projected — the board does not need it.

**Rejected alternatives**: (A) load full `Pedido` aggregates then filter in memory — rejected,
read-side waste; (B) expose a standalone `DbSet<OrdenTrabajo>` / new aggregate — rejected, breaks the
owned-entity model and the no-migration constraint.

### ADR-003 — SignalR realtime via an Application port (ADOPT, PR2)

**Decision**: Define `IKitchenNotifier` in the Application layer. Implement it in the API layer as a
`SignalRKitchenNotifier` adapter wrapping `IHubContext<KitchenHub>`. The hub pushes the flat board item
to the `"kitchen"` group. Mutation handlers call the port **after** `SaveChangesAsync`.

**Context**: Realtime is additive and must not couple Application to ASP.NET SignalR. A port keeps the
dependency arrow pointing inward; PR1 ships with no SignalR at all.

**Layering**:
- `IKitchenNotifier` (Application/Abstractions/Realtime) — pure abstraction, payload is the Contracts
  board item.
- `KitchenHub : Hub` (Api/Hubs) — clients join the `"kitchen"` group on connect.
- `SignalRKitchenNotifier : IKitchenNotifier` (Api/Realtime) — adapter using `IHubContext`.
- Handlers receive `IKitchenNotifier` by constructor injection (PR2 only) and invoke it post-commit.

**Why post-commit, not via domain events**: the existing domain-event dispatch
(`DbContext.SaveChangesAsync`) is reserved for in-process side effects (stock). Pushing UI deltas is an
outbound concern best triggered explicitly by the handler after a successful commit, so a failed push
never rolls back a committed mutation, and the REST board remains the source of truth for reconnects.

**Rejected alternative**: raise a domain event consumed by a SignalR dispatcher. Rejected — couples the
notification lifecycle to the transaction and bloats the domain-event channel with presentation concerns.

### ADR-004 — Role-gate placement at the Application layer (ADOPT)

**Decision**: Extract `RolUsuario` from `ClaimTypes.Role` at the endpoint, pass it into the command, and
enforce the allowed-roles set inside each mutation handler. The Domain performs no role check for OT
mutations.

**Context**: This mirrors the established Phase-5 pattern: `PedidoEndpoints.cs:63-69` parses the role
claim (403 if missing/unparseable) and `TransicionarEstadoPedidoHandler` forwards `Rol` into the domain.
Centralizing the gate at Application keeps endpoints thin and testable via `ApiFactory`.

**Allowed roles**:
- `GenerarOrdenesTrabajo`: `Mozo`, `Administrador`.
- `AsignarCocinero`, `MarcarOrdenTrabajoLista`: `Cocinero`, `Administrador`.
- `GetOrdenesByEstado`: any authenticated role.

**Rejected alternative**: ASP.NET `[Authorize(Roles=...)]` / authorization policies at the endpoint.
Rejected for consistency — the codebase already gates via claim-parse + handler logic, and the existing
integration tests assert the 403 path through that seam.

### ADR-005 — Enum serialization as string (KEEP existing global config)

**Decision**: Reuse the global `JsonStringEnumConverter` already registered in `Program.cs:36-37`.
`EstadoOT`, `TipoPedido`, and `RolUsuario` serialize as strings in responses and parse from strings in
the `?estado=` query and request bodies.

**Context**: W-03 already standardized string enums API-wide. The `?estado=` query binds via
`Enum.TryParse<EstadoOT>` at the endpoint (case-insensitive), returning 400 on an invalid value.

**Rejected alternative**: integer enum on the wire — rejected, inconsistent with the rest of the API and
worse Swagger DX.

---

## LOCKED signatures

### Domain (the only Domain edits)

```csharp
// OrdenTrabajo.cs:85 — visibility change ONLY (body unchanged)
internal void AsignarCocinero(LegajoId cocinero)

// Pedido.cs — NEW method (place after MarcarOrdenTrabajoLista, ~line 282)
public void AsignarCocineroAOT(Guid otId, LegajoId cocinero, RolUsuario rol)
```

Unchanged domain methods used as-is:
- `Pedido.GenerarOrdenesTrabajo(IReadOnlyDictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>)` — `Pedido.cs:227`
- `Pedido.MarcarOrdenTrabajoLista(Guid ordenTrabajoId, RolUsuario rolCocinero)` — `Pedido.cs:269`
- `LineaRecetaSnapshot(Guid IngredienteId, Cantidad Cantidad)` — `LineaRecetaSnapshot.cs:14`
- `EstadoOT { Creada=0, Preparandose=1, Lista=2, Cancelada=3 }` — `EstadoOT.cs`
- `LegajoId(Guid valor)` — `LegajoId.cs:13`

### Application — commands & handlers (new files under `Application/Pedidos/`)

```csharp
// GenerarOrdenesTrabajo/
public sealed record GenerarOrdenesTrabajoCommand(Guid PedidoId, RolUsuario Rol);
public sealed class GenerarOrdenesTrabajoHandler   // ctor: IPedidoRepository, IPlatoRepository, IUnitOfWork
{ public Task Handle(GenerarOrdenesTrabajoCommand cmd, CancellationToken ct = default); }

// AsignarCocinero/
public sealed record AsignarCocineroCommand(Guid PedidoId, Guid OtId, Guid CocineroLegajoId, RolUsuario Rol);
public sealed class AsignarCocineroHandler          // ctor: IPedidoRepository, IUnitOfWork, (PR2) IKitchenNotifier
{ public Task Handle(AsignarCocineroCommand cmd, CancellationToken ct = default); }

// MarcarOrdenTrabajoLista/
public sealed record MarcarOrdenTrabajoListaCommand(Guid PedidoId, Guid OtId, RolUsuario Rol);
public sealed class MarcarOrdenTrabajoListaHandler  // ctor: IPedidoRepository, IUnitOfWork, (PR2) IKitchenNotifier
{ public Task Handle(MarcarOrdenTrabajoListaCommand cmd, CancellationToken ct = default); }

// GetOrdenesByEstado/
public sealed record GetOrdenesByEstadoQuery(EstadoOT? Estado);
public sealed class GetOrdenesByEstadoHandler       // ctor: IPedidoRepository
{ public Task<IReadOnlyList<OrdenTrabajoBoardItem>> Handle(GetOrdenesByEstadoQuery query, CancellationToken ct = default); }
```

Role gate uses the existing `ForbiddenException`/`NotFoundException`/`ValidationException` from
`Application/Common/Exceptions` (same as `TransicionarEstadoPedidoHandler` uses `NotFoundException`).

### Application — Persistence abstractions (extend existing interfaces)

```csharp
// IPedidoRepository.cs — ADD
Task<IReadOnlyList<OrdenTrabajoBoardItem>> GetAllOrdenesTrabajoAsync(
    EstadoOT? estado, CancellationToken ct = default);

// IPlatoRepository.cs — ADD (batch load to resolve recipes without N+1)
Task<IReadOnlyList<Plato>> GetByIdsAsync(
    IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
```

`OrdenTrabajoBoardItem` is a flat read-model record. It lives in the Application layer (read-model
returned by the repository) and is mapped to the API response by Contracts:

```csharp
// Application/Pedidos/GetOrdenesByEstado/OrdenTrabajoBoardItem.cs
public sealed record OrdenTrabajoBoardItem(
    Guid   OtId,
    Guid   PedidoId,
    TipoPedido PedidoTipo,
    Guid   PlatoId,
    Guid   LineaPedidoId,
    EstadoOT Estado,
    Guid?  CocineroAsignadoLegajoId);
```

### Contracts (new `Contracts/Pedidos/OrdenTrabajo*.cs`, 4-file pattern)

```csharp
// OrdenTrabajoRequests.cs
public sealed record AsignarCocineroRequest(Guid CocineroLegajoId);
// (GenerarOrdenesTrabajo and MarcarLista take no body — pedidoId/otId come from the route)

// OrdenTrabajoResponses.cs
public sealed record OrdenTrabajoResponse(           // single-OT mutation response
    Guid Id, Guid PedidoId, Guid PlatoId, Guid LineaPedidoId,
    EstadoOT Estado, Guid? CocineroAsignadoLegajoId);
public sealed record OrdenTrabajoBoardResponse(      // board item (flat)
    Guid OtId, Guid PedidoId, TipoPedido PedidoTipo, Guid PlatoId,
    Guid LineaPedidoId, EstadoOT Estado, Guid? CocineroAsignadoLegajoId);

// OrdenTrabajoValidators.cs
public sealed class AsignarCocineroRequestValidator : AbstractValidator<AsignarCocineroRequest>
{ /* RuleFor(x => x.CocineroLegajoId).NotEmpty() */ }

// OrdenTrabajoMappings.cs
public static OrdenTrabajoBoardResponse ToResponse(this OrdenTrabajoBoardItem item);
public static AsignarCocineroCommand ToCommand(this AsignarCocineroRequest r, Guid pedidoId, Guid otId, RolUsuario rol);
```

`PedidoResponse` is NOT extended (locked decision 5 — no breaking change).

### API routes (new `Api/Endpoints/OrdenTrabajoEndpoints.cs`)

`MapOrdenTrabajoEndpoints()` registered in `Program.cs` after `MapPedidoEndpoints()`. Two groups:
`/pedidos` (nested mutations, `.RequireAuthorization()`) and `/ordenes-trabalho` (board,
`.RequireAuthorization()`). Each mutation endpoint parses `ClaimTypes.Role` exactly like
`PedidoEndpoints.cs:63-69`.

### SignalR (PR2)

```csharp
// Application/Abstractions/Realtime/IKitchenNotifier.cs
public interface IKitchenNotifier
{ Task NotifyOtChangedAsync(OrdenTrabajoBoardItem item, CancellationToken ct = default); }

// Api/Hubs/KitchenHub.cs
public sealed class KitchenHub : Hub
{ public override Task OnConnectedAsync(); }   // adds connection to group "kitchen"

// Api/Realtime/SignalRKitchenNotifier.cs : IKitchenNotifier
// uses IHubContext<KitchenHub>; sends method "OtChanged" with the mapped board response to group "kitchen"
```

- Hub route: `app.MapHub<KitchenHub>("/hubs/kitchen")` in `Program.cs`.
- Group name: `"kitchen"`.
- Client method pushed: `"OtChanged"`.
- Payload: `OrdenTrabajoBoardResponse` (the flat board item).
- DI: `services.AddSignalR()` in `Program.cs`; `services.AddScoped<IKitchenNotifier, SignalRKitchenNotifier>()`.

### DI registration (`Application/DependencyInjection.cs`)

```csharp
// add to AddApplication() — Slice: Kitchen (Phase 6)
services.AddScoped<GenerarOrdenesTrabajoHandler>();
services.AddScoped<AsignarCocineroHandler>();
services.AddScoped<MarcarOrdenTrabajoListaHandler>();
services.AddScoped<GetOrdenesByEstadoHandler>();
```

`IKitchenNotifier` is registered in the API composition root (PR2), since the adapter lives there.

---

## Routes table

### PR1 — core workflow + REST board

| Method | Route | Auth / Roles | Handler | Result |
|--------|-------|--------------|---------|--------|
| POST | `/pedidos/{pedidoId:guid}/ordenes-trabalho` | Mozo, Administrador | `GenerarOrdenesTrabajoHandler` | `204 NoContent` (or `201` w/ count) |
| POST | `/pedidos/{pedidoId:guid}/ordenes-trabalho/{otId:guid}/asignar-cocinero` | Cocinero, Administrador | `AsignarCocineroHandler` | `200 Ok` (`OrdenTrabajoResponse`) |
| POST | `/pedidos/{pedidoId:guid}/ordenes-trabalho/{otId:guid}/marcar-lista` | Cocinero, Administrador | `MarcarOrdenTrabajoListaHandler` | `200 Ok` (`OrdenTrabajoResponse`) |
| GET | `/ordenes-trabalho?estado={EstadoOT?}` | any authenticated | `GetOrdenesByEstadoHandler` | `200 Ok` (`IReadOnlyList<OrdenTrabajoBoardResponse>`) |

Error contracts: 401 unauthenticated; 403 missing/unparseable role claim or disallowed role; 404 Pedido
or OT not found; 400 invalid `?estado=` or `GenerarOrdenesTrabajo` precondition failures (empty recipe,
unconfirmed price) surfaced as ProblemDetails by `GastroGestionExceptionHandler`.

### PR2 — realtime layer (additive)

| Transport | Route / target | Direction | Payload |
|-----------|----------------|-----------|---------|
| SignalR | Hub `/hubs/kitchen`, group `"kitchen"`, method `"OtChanged"` | server → client | `OrdenTrabajoBoardResponse` |

PR2 adds `IKitchenNotifier` injection to `AsignarCocineroHandler` and `MarcarOrdenTrabajoListaHandler`
(post-commit push). PR1 contracts are unchanged.

---

## Test strategy

Integration tests via `ApiFactory.CreateAuthenticatedClient(RolUsuario)` (`ApiFactory.cs:127`), against
the LocalDB test database with the dev seeder. Mirror the existing transactional endpoint test suite.

PR1 cases:
- **Generate OTs (happy path)**: seed a Pedido with priced lines whose Platos have recipes → mozo client
  POSTs generate → 204; `GET /ordenes-trabalho?estado=Creada` returns one item per line with correct
  `PedidoId`, `PlatoId`, `LineaPedidoId`.
- **Generate OTs early-failure**: a line's Plato has empty `LineasReceta` → 400 ProblemDetails, and NO
  OT is created (all-or-nothing; assert board still empty).
- **Generate OTs unconfirmed price**: line without `ConfirmarPrecio` → 400.
- **Assign cook role gate**: `CreateAuthenticatedClient(Cocinero)` → 200 and OT becomes `Preparandose`
  with `CocineroAsignadoLegajoId` set; `CreateAuthenticatedClient(Administrador)` → 200;
  `CreateAuthenticatedClient(Mozo)` → 403; `CreateAuthenticatedClientWithoutRole()` → 403;
  `CreateAuthenticatedClientWithBogusRole()` → 403.
- **Assign cook invalid state**: assign on an already-`Preparandose` OT → 400 (domain throws).
- **Mark lista role gate + auto-advance**: Cocinero marks the last OT of a non-Salon Pedido lista → OT
  `Lista` and Pedido auto-advances to `ListoParaEntregar` (assert via `GET /pedidos/{id}`).
- **Board filter**: `?estado=Lista` returns only Lista OTs; no `estado` returns all; bogus `?estado=` → 400.
- **Not found**: assign/mark on a non-existent pedidoId or otId → 404.
- **`GET /pedidos/{id}` unchanged**: snapshot the response shape before/after generating OTs to prove no
  contract drift (locked decision 5).

PR2 (SignalR) — boundary:
- Integration test with a real `HubConnection` against the `TestServer` (`ApiFactory.Server`): connect,
  join the `"kitchen"` group on connect, perform a `marcar-lista` mutation via the authenticated HTTP
  client, and await an `"OtChanged"` message carrying the expected `OtId`/`Estado`. SignalR over
  `TestServer` requires `HttpMessageHandlerFactory` from the factory's `Server.CreateHandler()`.
- If the SignalR-over-TestServer harness proves flaky, fall back to a **unit test of the handler with a
  mock `IKitchenNotifier`** asserting `NotifyOtChangedAsync` is invoked once after a successful commit,
  and document that the wire-level push is verified manually. The Application-layer port makes this
  fallback clean and is the explicit testability reason for ADR-003.

No new test database or migration is required (locked decision 5).

---

## PR slice boundary (chained, stacked-to-main)

- **PR1 — Core workflow + REST** (autonomous, shippable): Domain hardening (ADR-001), 4 use cases,
  `IPedidoRepository.GetAllOrdenesTrabajoAsync` + `IPlatoRepository.GetByIdsAsync` and their EF impls,
  Contracts (4 files), `OrdenTrabajoEndpoints.cs`, DI registration, `Program.cs` route map, full
  integration tests. Fully usable via REST polling. No SignalR dependency.
- **PR2 — Realtime layer** (additive, stacks on PR1): `IKitchenNotifier` port, `KitchenHub`,
  `SignalRKitchenNotifier` adapter, `AddSignalR()` + hub map + DI, inject the port into the two mutation
  handlers (post-commit push), SignalR integration/handler test. Reverts independently; REST board is
  unaffected and remains the reconnection-recovery source of truth.

Both PRs target main in order (stacked-to-main). PR1 introduces no API surface that PR2 changes, so PR2
is purely additive.

## Checklist (design acceptance)

- [ ] Every signature above matches a verified CLR type or is a clearly-marked new addition.
- [ ] No `PedidoResponse` change; no new migration.
- [ ] Mutations route through `Pedido`; `AsignarCocinero` is `internal`.
- [ ] Board query projects off `PedidoOrdenesTrabajo` without loading aggregates.
- [ ] SignalR is isolated behind `IKitchenNotifier` and confined to PR2.
- [ ] Role gate placement mirrors `TransicionarEstadoPedidoHandler` + `PedidoEndpoints` claim parse.

## Next step

Proceed to `sdd-tasks` once the spec is also ready; tasks will decompose PR1 then PR2 against these
locked signatures.
