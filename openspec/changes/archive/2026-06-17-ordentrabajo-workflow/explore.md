# Exploration: OrdenTrabajo Workflow (Phase 6 of 7)

Change: `ordentrabajo-workflow`. Goal: expose the kitchen-order (OrdenTrabajo / "comanda") workflow over the API. The `OrdenTrabajo` entity is already ported to the new Domain but has NO Application use cases, NO Contracts, and NO endpoint. Stock (the other half of the original "Phase 6 Stock/Orden_Trabajo") is already fully done.

## 1. Current State

### Domain Layer — fully modeled, nothing missing

**`src/GastroGestion.Domain/Pedidos/OrdenTrabajo.cs`** — an `Entity` (not AggregateRoot), owned by `Pedido`.
- Fields: `PlatoId`, `LineaPedidoId`, `Estado` (EstadoOT), `CocineroAsignado` (LegajoId?), `RecetaSnapshot` (JSON-serialized recipe snapshot list).
- Factory: `internal static OrdenTrabajo Crear(...)` — only `Pedido` can call it.
- Only public write method: `AsignarCocinero(LegajoId)` — transitions `Creada → Preparandose`. Callable from Application directly on the entity (a risk — see §6).
- Internal write methods (parent Pedido calls them): `MarcarLista()` and `Cancelar()`.

**`src/GastroGestion.Domain/Pedidos/EstadoOT.cs`** — `Creada=0`, `Preparandose=1`, `Lista=2`, `Cancelada=3`.

**`src/GastroGestion.Domain/Pedidos/Pedido.cs`** — OT lifecycle managed here:
- `GenerarOrdenesTrabajo(IReadOnlyDictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>)` (line 227): creates one OT per LineaPedido, all-or-nothing. Pre-condition: every line must have a confirmed price snapshot. OTs are NOT auto-created when adding a line — this method must be explicitly called.
- `MarcarOrdenTrabajoLista(Guid ordenTrabajoId, RolUsuario rolCocinero)` (line 269): calls `ot.MarcarLista()` and auto-advances non-Salon Pedido to `ListoParaEntregar` if all OTs are Lista.
- Owned collection: `IReadOnlyList<OrdenTrabajo> OrdenesTrabajo`.

**`src/GastroGestion.Domain/Pedidos/Events/OrdenTrabajoCreada.cs`** — raised per OT inside `GenerarOrdenesTrabajo`. Infra layer already consumes this for stock moves.

### Persistence — OTs are owned, NOT a separate aggregate

- **`PedidoConfiguration.cs`** (lines 81–111): `OwnsMany(...).ToTable("PedidoOrdenesTrabajo")`. Columns: `Id`, `PlatoId`, `LineaPedidoId`, `Estado` (int), `CocineroAsignado` (nullable Guid), `RecetaSnapshot` (nvarchar(max) JSON), `PedidoId` (FK).
- **`20260614181827_InitialCatalogue.cs`** (lines 268–289): `PedidoOrdenesTrabajo` table already created. **No new migration needed.**
- **`GastroGestionDbContext.cs`** (line 24 comment): no DbSet exposed for owned types.
- **No `IOrdenTrabajoRepository` exists and none is needed for mutations.** For the cross-pedido kitchen board query, a new method on `IPedidoRepository` suffices.

### Application Layer — ZERO use cases for OrdenTrabajo
No handlers for `GenerarOrdenesTrabajo`, `AsignarCocinero`, `MarcarOrdenTrabajoLista`, or any OT query. `IPedidoRepository` only has `GetByIdAsync`, `GetByIdsAsync`, `AddAsync`.

### Contracts Layer — ZERO contracts for OrdenTrabajo
`PedidoResponses.cs` has `LineaPedidoResponse` but no `OrdenTrabajoResponse`. OTs are invisible to API consumers.

### API Layer — NO endpoint for OrdenTrabajo
`PedidoEndpoints.cs` has five routes; zero OT routes.

## 2. The State Machine

```
Creada ──[AsignarCocinero]──► Preparandose ──[MarcarLista via Pedido]──► Lista
  │                                │
  └──────────[Cancelar* cascade]───┴──────────────────────────────────► Cancelada
```

`*` Cancelar is triggered exclusively via `Pedido.TransicionarEstado(Cancelado)` cascade. No standalone OT cancellation exists in the domain.

| Transition | Domain Method | Likely Role (to confirm) |
|---|---|---|
| Creada → Preparandose | `ot.AsignarCocinero` (public) | Cocinero |
| Preparandose → Lista | `pedido.MarcarOrdenTrabajoLista` (internal) | Cocinero |
| Any → Cancelada | `pedido.Cancelar` cascade | Mozo or Cajero |

No role gate exists inside the domain; role enforcement must live at the Application layer (same pattern as `TransicionarEstadoPedidoHandler`).

## 3. Gap Analysis

**Application (new use cases):**
1. `GenerarOrdenesTrabajo` command+handler — loads Pedido, resolves recipe snapshots per `PlatoId` via `IPlatoRepository`, calls `pedido.GenerarOrdenesTrabajo(...)`, saves.
2. `AsignarCocinero` command+handler — loads Pedido, finds the OT, calls `ot.AsignarCocinero`, role-checks, saves.
3. `MarcarOrdenTrabajoLista` command+handler — loads Pedido, calls `pedido.MarcarOrdenTrabajoLista`, saves.
4. `GetOrdenesByEstado` query+handler — kitchen board: cross-pedido query by `EstadoOT`. Needs one new `IPedidoRepository` method.

**Contracts (4-file pattern):** `OrdenTrabajoRequests.cs`, `OrdenTrabajoResponses.cs`, `OrdenTrabajoValidators.cs`, `OrdenTrabajoMappings.cs`. Optionally extend `PedidoResponse` with the OT list (breaking change).

**Persistence:** add `GetAllOrdenesTrabajoAsync(EstadoOT? estado, CancellationToken)` to `IPedidoRepository`; implement in `PedidoRepository` via projection on the owned collection — no new migration.

**API:** OT mutation sub-routes under `/pedidos/{pedidoId}/ordenes-trabalho/`; new `OrdenTrabajoEndpoints.cs` for `GET /ordenes-trabalho?estado=`; register in `Program.cs`.

**DI:** register all new handlers as `AddScoped`.

## 4. Approach Options

- **A — nested only** (`/pedidos/{id}/ordenes-trabalho/...`): aggregate-safe, no infra additions, but the cross-pedido kitchen board is awkward. Effort Low-Med.
- **B — standalone** (`/ordenes-trabalho`, pedidoId in body): clean kitchen board, but mutations need an OT→Pedido join (no DbSet for OTs) and break the aggregate-root visibility pattern. Effort Med-High.
- **C — hybrid (RECOMMENDED)**: mutations nested under `/pedidos/{pedidoId}/ordenes-trabalho/{otId}/...`; kitchen board read as top-level `GET /ordenes-trabalho?estado=`. Aggregate-safe + clean read, costs one query method on `IPedidoRepository`. Effort Medium.

## 5. Open Questions for the Proposal Round
1. Role per transition: is `Cocinero` the only role for `AsignarCocinero`/`MarcarLista`? Can `Administrador` also?
2. `GenerarOrdenesTrabajo` trigger: explicit action (mozo, after confirming prices) or automatic on a Pedido state transition?
3. Kitchen board filters: is `EstadoOT` enough, or also by cocinero/station/time window?
4. Realtime vs polling: does the kitchen display need push (SignalR/SSE) or is REST polling acceptable for v1?
5. Single-OT cancellation without cancelling the whole Pedido? (domain doesn't support it today)
6. Cocinero reassignment once `Preparandose`? (domain throws today)
7. Include OTs inline in `GET /pedidos/{id}` (breaking change) or separate endpoint only?

## 6. Risks
- Cross-pedido OT query performance: project to a flat DTO / direct SQL on `PedidoOrdenesTrabajo` to avoid loading full Pedido aggregates. Read-side only.
- Recipe snapshot resolution: Platos with empty `LineasReceta` fail the domain invariant; handler must detect early with a clear error.
- `AsignarCocinero` is public on `OrdenTrabajo` — handlers MUST load through the Pedido root. Cleaner fix: make it `internal` and add `Pedido.AsignarCocineroAOT(Guid otId, LegajoId, RolUsuario)`.
- `LegajoId` not validated against a real `Usuario`/role at the domain — Application must validate.
- No migration needed unless new columns (e.g. cooking-start timestamp) are added.
- Extending `PedidoResponse` with OTs is a breaking API contract change.

## Affected Areas
New: `Application/Pedidos/{GenerarOrdenesTrabajo,AsignarCocinero,MarcarOrdenTrabajoLista,GetOrdenesByEstado}/`, `Contracts/Pedidos/OrdenTrabajo{Requests,Responses,Validators,Mappings}.cs`, `Api/Endpoints/OrdenTrabajoEndpoints.cs`.
Modified: `Application/DependencyInjection.cs`, `Abstractions/Persistence/IPedidoRepository.cs`, `Infrastructure/Persistence/Repositories/PedidoRepository.cs`, `Api/Program.cs`, `Api/Endpoints/PedidoEndpoints.cs`, `Contracts/Pedidos/PedidoResponses.cs` (if OTs added).
