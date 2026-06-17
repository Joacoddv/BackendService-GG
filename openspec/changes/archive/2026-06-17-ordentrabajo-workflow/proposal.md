# Proposal: OrdenTrabajo (Kitchen) Workflow over the API

## Intent

The `OrdenTrabajo` (kitchen "comanda") is fully modeled in the Domain and persisted (`PedidoOrdenesTrabajo` table) but has ZERO Application use cases, Contracts, or endpoints. The kitchen workflow — generate work orders, assign a cook, mark ready, and see a live kitchen board — is invisible to API consumers. This change exposes it.

## Why now

Phase 6 of 7. Stock (the other half of the original "Stock/Orden_Trabajo" phase) is done; the kitchen side is the last unexposed core workflow before the strangler reaches feature parity. JWT + role extraction (Phase 5) is in place, so role-gated kitchen actions are now possible.

## What success looks like

A mozo can explicitly generate work orders for a Pedido after prices are confirmed; a cook (or admin) can pick up an OT, then mark it ready; the kitchen sees a board of pending/in-progress OTs and gets live updates. `GET /pedidos/{id}` is unchanged.

## Scope

### In Scope
- Use cases: `GenerarOrdenesTrabajo` (explicit mozo action), `AsignarCocinero`, `MarcarOrdenTrabajoLista`, `GetOrdenesByEstado` (kitchen board).
- REST (source of truth): `GET /ordenes-trabajo?estado=` (top-level board) + mutation sub-routes nested under `/pedidos/{pedidoId}/ordenes-trabajo/{otId}/...`.
- Contracts: `OrdenTrabajo{Requests,Responses,Validators,Mappings}.cs` (new `OrdenTrabajoResponse`).
- Persistence: add `GetAllOrdenesTrabajoAsync(EstadoOT? estado, ...)` to `IPedidoRepository`, projecting to a flat DTO (no full-aggregate load).
- Role enforcement at Application layer (JWT `ClaimTypes.Role`): `AsignarCocinero` + `MarcarOrdenTrabajoLista` allow **COCINERO + ADMINISTRADOR**, mirroring `TransicionarEstadoPedidoHandler`.
- Domain hardening: make `OrdenTrabajo.AsignarCocinero` `internal`, add `Pedido.AsignarCocineroAOT(otId, legajo, rol)` so mutations always go through the aggregate root.
- SignalR realtime layer (PR2, additive): a hub pushing OT state-change deltas to a "kitchen" group, layered on top of the REST board.

### Out of Scope (Non-Goals for v1)
- Single-OT cancellation (domain has no standalone path; cancel is Pedido-cascade only).
- Cocinero reassignment once `Preparandose` (domain throws today).
- Extending `PedidoResponse` with the OT list (breaking contract change — explicitly avoided).
- Polling-only kitchen board as the final UX (REST stays, but realtime is the target).
- Board filters beyond `EstadoOT` (cocinero/station/time-window).
- New migration / cooking-start timestamp columns.

## Capabilities

### New Capabilities
- None (extends existing layer specs).

### Modified Capabilities
- `Api`: new OT requirements — kitchen board read, nested OT mutations, SignalR hub, OT contracts (extends REQ catalog; resolves carry-forward Phase-6 follow-up).
- `Infrastructure`: `IPedidoRepository` gains a projected cross-Pedido OT query method.

## Approach

Approach **C (hybrid)** from exploration: aggregate-safe nested mutations + clean top-level board read. No new migration. The board query projects directly off `PedidoOrdenesTrabajo` to a flat DTO to avoid loading full Pedido aggregates (read-side performance). `GenerarOrdenesTrabajo` resolves recipe snapshots per `PlatoId` via `IPlatoRepository`; if a Plato has empty `LineasReceta` the handler **fails early with a clear error** rather than letting the domain invariant throw opaquely. `LegajoId`/cocinero existence and role are validated at the Application layer (no domain check).

## Proposed PR Slicing (chained, stacked-to-main)

- **PR1 — Core workflow**: 4 use cases, repository query method, Contracts, REST board + nested mutations, role gates, domain `AsignarCocineroAOT` hardening, DI. Ships first; fully usable via REST polling.
- **PR2 — Realtime layer**: SignalR hub + "kitchen" group push of OT deltas, wired into the mutation handlers. Purely additive on PR1; REST remains the source of truth and reconnection-recovery path.

This split aligns with stacked-to-main chained PRs: PR1 is autonomous and shippable; PR2 layers on without altering PR1 contracts.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/Pedidos/{GenerarOrdenesTrabajo,AsignarCocinero,MarcarOrdenTrabajoLista,GetOrdenesByEstado}/` | New | 4 use cases + role gates |
| `Contracts/Pedidos/OrdenTrabajo{Requests,Responses,Validators,Mappings}.cs` | New | OT contract set |
| `Api/Endpoints/OrdenTrabajoEndpoints.cs` | New | Board read + nested mutations |
| `Api/Hubs/KitchenHub.cs` (PR2) | New | SignalR delta push |
| `Domain/Pedidos/OrdenTrabajo.cs`, `Pedido.cs` | Modified | `AsignarCocinero` → internal; add `AsignarCocineroAOT` |
| `Abstractions/Persistence/IPedidoRepository.cs`, `Infrastructure/Persistence/Repositories/PedidoRepository.cs` | Modified | Projected board query |
| `Application/DependencyInjection.cs`, `Api/Program.cs` | Modified | Register handlers + hub |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Cross-Pedido board query loads full aggregates | Med | Project to flat DTO directly off owned collection; read-only |
| Empty-recipe Plato breaks `GenerarOrdenesTrabajo` | Med | Handler pre-checks snapshots, fails early with clear error |
| `AsignarCocinero` bypasses aggregate root | Med | Make internal, route via `Pedido.AsignarCocineroAOT` |
| Invalid `LegajoId`/role | Med | Application-layer validation (JWT role + cocinero lookup) |
| SignalR scope creep into PR1 | Low | Hard PR boundary; realtime is PR2 only |

## Rollback Plan

PR2 reverts independently (drop hub + push calls; REST board unaffected). PR1 reverts by removing new Application/Contracts/Api files and the repository method + DI registrations; no migration was added, so the schema is untouched. The `AsignarCocinero` visibility change is the only Domain edit to revert.

## Dependencies

- Phase 5 JWT role extraction (`ClaimTypes.Role`) — already in place.
- SignalR package (PR2 only).

## Success Criteria

- [ ] Mozo can `GenerarOrdenesTrabajo` for a priced Pedido; one OT per LineaPedido.
- [ ] COCINERO/ADMINISTRADOR can assign a cook and mark an OT ready; other roles get 403.
- [ ] `GET /ordenes-trabalho?estado=` returns the board without loading full aggregates.
- [ ] `GET /pedidos/{id}` response is byte-for-byte unchanged.
- [ ] PR2 pushes OT deltas to the kitchen group; reconnect recovers via REST.
