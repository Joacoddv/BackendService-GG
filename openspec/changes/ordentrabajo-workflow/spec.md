# Delta Specification: ordentrabajo-workflow (Phase 6 of 7)

**Change:** ordentrabajo-workflow
**Date:** 2026-06-16
**PR Slices:** PR1 — Core workflow | PR2 — Realtime layer (SignalR, additive)
**Status:** DRAFT

---

## Scope Summary

This delta spec defines what MUST be true after the `ordentrabajo-workflow` change is applied. It covers five numbered requirement areas: generating work orders, assigning a cook, marking an order ready, reading the kitchen board, and the additive realtime push layer. `GET /pedidos/{id}` is byte-for-byte unchanged.

---

## ADDED Requirements

---

### OT-01 — Generate Work Orders `[PR1]`

**Layer:** Application + Domain

The system MUST expose a command that generates one `OrdenTrabajo` per `LineaPedido` for a given `Pedido`, all-or-nothing, as an explicit mozo action (not triggered automatically).

The system MUST pre-validate that every `LineaPedido` in the `Pedido` has a confirmed price before calling the domain method.

The system MUST pre-validate that every `PlatoId` referenced by the lines has a non-empty `LineasReceta` snapshot. If any `Plato` has an empty recipe, the handler MUST fail early with HTTP 422 and a `ProblemDetails` body before delegating to the domain.

The system MUST NOT allow re-generation if `OrdenesTrabajo` already exist for the `Pedido`.

#### Scenario OT-01-A: Happy path — all lines priced, all recipes populated

- GIVEN a `Pedido` with two `LineaPedido` entries, each with a confirmed price, each `PlatoId` with a non-empty recipe snapshot
- WHEN a mozo calls `POST /pedidos/{pedidoId}/ordenes-trabajo`
- THEN the system creates one `OrdenTrabajo` per line with `Estado = Creada`
- AND returns HTTP 204 NoContent

#### Scenario OT-01-B: Failure — line without confirmed price

- GIVEN a `Pedido` where at least one `LineaPedido` has no confirmed price
- WHEN a mozo calls `POST /pedidos/{pedidoId}/ordenes-trabajo`
- THEN the system returns HTTP 422 with a `ProblemDetails` body
- AND no `OrdenTrabajo` is persisted

#### Scenario OT-01-C: Failure — Plato with empty recipe

- GIVEN a `Pedido` where a referenced `PlatoId` has an empty `LineasReceta`
- WHEN a mozo calls `POST /pedidos/{pedidoId}/ordenes-trabajo`
- THEN the system returns HTTP 422 with a `ProblemDetails` body describing the offending `PlatoId`
- AND no `OrdenTrabajo` is persisted

#### Scenario OT-01-D: Failure — Pedido not found

- GIVEN a `pedidoId` that does not exist
- WHEN any authenticated user calls `POST /pedidos/{pedidoId}/ordenes-trabajo`
- THEN the system returns HTTP 404

#### Scenario OT-01-E: Failure — OTs already generated

- GIVEN a `Pedido` that already has `OrdenesTrabajo`
- WHEN a mozo calls `POST /pedidos/{pedidoId}/ordenes-trabajo`
- THEN the system returns HTTP 409

---

### OT-02 — Assign Cook (Creada → Preparandose) `[PR1]`

**Layer:** Application + Domain

The system MUST allow users with role `COCINERO` or `ADMINISTRADOR` to assign a cook (legajo) to an `OrdenTrabajo`, transitioning it from `Creada` to `Preparandose`.

The role MUST be read from the `ClaimTypes.Role` JWT claim at the Application layer. Users with any other role MUST receive HTTP 403.

The `OrdenTrabajo.AsignarCocinero` method MUST be made `internal`; all Application-layer mutations MUST route through `Pedido.AsignarCocineroAOT(otId, legajoId, rol)`.

#### Scenario OT-02-A: Happy path

- GIVEN an `OrdenTrabajo` in state `Creada`
- WHEN a user with role `COCINERO` calls `PATCH /pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero`
- THEN `Estado` transitions to `Preparandose`, `CocineroAsignado` is set
- AND returns HTTP 200

#### Scenario OT-02-B: Failure — wrong role

- GIVEN an authenticated user with role `MOZO`
- WHEN they call the assign endpoint
- THEN the system returns HTTP 403

#### Scenario OT-02-C: Failure — OT not in Creada

- GIVEN an `OrdenTrabajo` in state `Preparandose` or `Lista`
- WHEN a valid user calls the assign endpoint
- THEN the system returns HTTP 422 with a `ProblemDetails` body

#### Scenario OT-02-D: Failure — Pedido or OT not found

- GIVEN a `pedidoId` or `otId` that does not exist
- WHEN a valid user calls the assign endpoint
- THEN the system returns HTTP 404

---

### OT-03 — Mark Order Ready (Preparandose → Lista) `[PR1]`

**Layer:** Application + Domain

The system MUST allow users with role `COCINERO` or `ADMINISTRADOR` to mark an `OrdenTrabajo` as ready, transitioning it from `Preparandose` to `Lista`.

After the transition, if all `OrdenesTrabajo` for the owning `Pedido` are `Lista` AND the `Pedido` is not a salon-type order, the system MUST automatically advance the `Pedido` to `ListoParaEntregar`.

Role enforcement follows the same pattern as OT-02 (`ClaimTypes.Role` from JWT, HTTP 403 on unauthorized role).

#### Scenario OT-03-A: Happy path — OT marked ready, Pedido not yet complete

- GIVEN an `OrdenTrabajo` in state `Preparandose` and at least one sibling OT still in progress
- WHEN a user with role `COCINERO` calls `PATCH /pedidos/{pedidoId}/ordenes-trabajo/{otId}/lista`
- THEN `Estado` transitions to `Lista`
- AND the `Pedido` state is unchanged
- AND returns HTTP 200

#### Scenario OT-03-B: Happy path — last OT, Pedido auto-advances

- GIVEN an `OrdenTrabajo` in state `Preparandose` and it is the last non-`Lista` OT for a non-salon `Pedido`
- WHEN a user with role `COCINERO` calls the ready endpoint
- THEN the OT transitions to `Lista`
- AND the `Pedido` transitions to `ListoParaEntregar`
- AND returns HTTP 200

#### Scenario OT-03-C: Failure — wrong role

- GIVEN an authenticated user with role `CAJERO`
- WHEN they call the ready endpoint
- THEN the system returns HTTP 403

#### Scenario OT-03-D: Failure — OT not in Preparandose

- GIVEN an `OrdenTrabajo` in state `Creada` or `Lista`
- WHEN a valid user calls the ready endpoint
- THEN the system returns HTTP 422 with a `ProblemDetails` body

#### Scenario OT-03-E: Failure — Pedido or OT not found

- GIVEN a `pedidoId` or `otId` that does not exist
- THEN the system returns HTTP 404

---

### OT-04 — Kitchen Board Read `[PR1]`

**Layer:** Api + Infrastructure

The system MUST expose `GET /ordenes-trabajo?estado={EstadoOT}` returning a flat projection of all `OrdenesTrabajo` across all `Pedidos`.

The flat projection MUST include: `Id`, `PlatoId`, `LineaPedidoId`, `PedidoId`, `Estado` (serialized as string per convention W-03), `CocineroAsignado`.

The `estado` query parameter MUST be optional; when omitted, all non-`Cancelada` orders MUST be returned.

Access MUST be restricted to users with role `COCINERO` or `ADMINISTRADOR`.

The query MUST NOT load full `Pedido` aggregates; it MUST project directly from the `PedidoOrdenesTrabajo` table.

#### Scenario OT-04-A: Board read with estado filter

- GIVEN multiple `OrdenesTrabajo` in various states across multiple `Pedidos`
- WHEN a `COCINERO` calls `GET /ordenes-trabajo?estado=Creada`
- THEN the response contains only `OrdenesTrabajo` with `Estado = "Creada"` as a flat list
- AND returns HTTP 200

#### Scenario OT-04-B: Board read without filter

- GIVEN multiple `OrdenesTrabajo` in states `Creada`, `Preparandose`, `Lista`, and `Cancelada`
- WHEN a `COCINERO` calls `GET /ordenes-trabajo`
- THEN the response contains all OTs with `Estado` NOT equal to `"Cancelada"`
- AND returns HTTP 200

#### Scenario OT-04-C: Failure — unauthorized role

- GIVEN an authenticated user with role `MOZO`
- WHEN they call `GET /ordenes-trabajo`
- THEN the system returns HTTP 403

#### Scenario OT-04-D: Failure — unauthenticated

- GIVEN a request with no JWT token
- WHEN calling `GET /ordenes-trabajo`
- THEN the system returns HTTP 401

---

### OT-05 — Realtime OT State Push (SignalR) `[PR2]`

**Layer:** Api (SignalR Hub)

> **PR2 scope only.** This requirement is NOT part of PR1. The REST board (OT-04) remains the authoritative source for initial load and reconnect recovery.

The system SHOULD push a real-time notification to a "kitchen" SignalR group whenever an `OrdenTrabajo` changes state (assigned, ready, or cancelled).

The push payload MUST include at minimum: `otId`, `pedidoId`, `newEstado` (as string), `cocineroAsignado` (nullable).

The REST board endpoint (OT-04) MUST remain unchanged and MUST serve as the reconnection-recovery path for kitchen clients.

#### Scenario OT-05-A: State change pushes delta to kitchen group

- GIVEN a kitchen client connected to the SignalR hub
- WHEN an OT transitions state via any OT mutation endpoint
- THEN the hub broadcasts the state-change delta to the "kitchen" group
- AND the REST board still reflects the same state on next GET

#### Scenario OT-05-B: Client reconnect recovers via REST

- GIVEN a kitchen client that reconnected after a hub disconnect
- WHEN it calls `GET /ordenes-trabajo`
- THEN it receives the current full board state, matching the authoritative database state

---

## Cross-Cutting Requirements

### OT-06 — Authentication and Role Enforcement `[PR1]`

All OT endpoints MUST require a valid JWT bearer token. Unauthenticated requests MUST return HTTP 401.

Enum values in all OT responses MUST be serialized as strings (project convention W-03). This applies to `EstadoOT` in all OT response DTOs.

`GET /pedidos/{id}` and `PedidoResponse` MUST remain byte-for-byte unchanged after this change.

---

## Non-Goals (Explicitly Out of Scope)

| Item | Reason |
|------|--------|
| Single-OT cancellation | Domain has no standalone cancel path; cancel is Pedido-cascade only |
| Cocinero reassignment once `Preparandose` | Domain throws; no v1 path |
| Extending `PedidoResponse` with OT list | Breaking contract change — explicitly avoided |
| Polling-only kitchen board as final UX | REST stays authoritative; realtime is PR2 goal |
| Board filters beyond `EstadoOT` | Out of scope for v1 (cocinero/station/time-window deferred) |
| New database migration | `PedidoOrdenesTrabajo` table already exists; no new columns |
