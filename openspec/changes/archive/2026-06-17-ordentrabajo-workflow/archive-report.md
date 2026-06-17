# Archive Report: ordentrabajo-workflow

**Change:** ordentrabajo-workflow (Phase 6 of 7 — Kitchen Workflow over API)  
**Archived:** 2026-06-17  
**Archive Location:** `openspec/changes/archive/2026-06-17-ordentrabajo-workflow/`  
**Status:** CLOSED AND ARCHIVED  

---

## Executive Summary

The `ordentrabajo-workflow` change is complete, verified, and archived. All 18 tasks (OW-01..OW-18) shipped across two chained PRs to main. The kitchen workflow (OrdenTrabajo) is now fully exposed over the API with REST endpoints, role-gated mutations, a flat kitchen board, and additive SignalR realtime push layer. Phase 6 of 7 complete.

---

## Delivery Summary

### PR1 — Core Workflow + REST Board (OW-01..OW-14)
- **Branch:** feat/ot-workflow-pr1a
- **PR:** #14
- **Merge Commit:** 2fe7a75
- **Tasks:** OW-01..OW-14 all complete
- **Tests:** 298/298 green (166 Domain, 22 Application, 33 Infrastructure, 77 Api)
- **Verdict:** PASS WITH WARNINGS (0 CRITICAL, 0 WARNING, 1 SUGGESTION — cosmetic spec header, now reconciled)

**Deliverables:**
- Domain hardening: `AsignarCocinero` internal + `Pedido.AsignarCocineroAOT` (ADR-001)
- 4 use cases: `GenerarOrdenesTrabajo`, `AsignarCocinero`, `MarcarOrdenTrabajoLista`, `GetOrdenesByEstado`
- Repository extensions: flat projection, batch load
- Contracts: 4-file pattern (requests, responses, validators, mappings)
- Endpoints: 3 nested mutations + 1 board GET, all role-gated
- DI: handlers registered, exception handling added

### PR2 — Realtime Layer (OW-15..OW-18)
- **Branch:** feat/ot-workflow-pr2-signalr
- **PR:** #15
- **Merge Commit:** 2b96b53
- **Tasks:** OW-15..OW-18 all complete
- **Tests:** 302/302 green (all prior 298 + 4 new from OW-18)
- **Verdict:** PASS (0 CRITICAL, 0 WARNING, 1 SUGGESTION non-blocking)

**Deliverables:**
- `IKitchenNotifier` port (Application abstraction)
- `KitchenHub` + `SignalRKitchenNotifier` adapter
- Mutation handlers wired for post-commit push
- Unit tests for notifier contract
- No REST contract changes; pure additive

---

## Specification Compliance

### Requirements Met (OT-01..OT-06)

| Requirement | Status | Notes |
|-------------|--------|-------|
| OT-01: Generate work orders (mozo, explicit) | PASS | HTTP 204, no auto-generation |
| OT-02: Assign cook (Cocinero/Admin, Creada→Preparandose) | PASS | Role gate via JWT, internal visibility |
| OT-03: Mark ready (Cocinero/Admin, Preparandose→Lista) | PASS | Auto-advances non-Salon Pedido |
| OT-04: Kitchen board (flat projection, no aggregate load) | PASS | GET /ordenes-trabalho?estado=, role-gated |
| OT-05: Realtime SignalR push (PR2 additive) | PASS | POST-commit broadcast, reconnect via REST |
| OT-06: Authentication + enum strings + PedidoResponse unchanged | PASS | JWT required, W-03 convention, no breaking change |

### Issues Resolved During Verification

1. **CRITICAL-01** (board role gate): Added ClaimTypes.Role parse, 403 on unauthorized. Verified in test `GET_OrdenesTrabajo_WrongRole_Returns403`. RESOLVED.
2. **WARNING-01** (HTTP 201 vs 204): Spec content correct (204 per RFC 9110), endpoint test assertion updated. RESOLVED.
3. **WARNING-02** (missing tests OT-03-D/E): Added integration tests for OT state validation and not-found path. RESOLVED.
4. **SUGGESTION-01** (Cancelada exclusion not tested): Added test `GET_OrdenesTrabajo_NoFilter_Returns200_ExcludesCancelada`. RESOLVED.
5. **SUGGESTION** (spec header cosmetic): Updated spec.md header from `**Status:** DRAFT` to `**Status:** RECONCILED` during archive. RESOLVED.

All findings remediated. No CRITICAL or WARNING issues remain in final state.

---

## Architecture Decisions (ADRs)

All 5 ADRs adopted and fully implemented:

- **ADR-001**: `AsignarCocinero` internal + `Pedido.AsignarCocineroAOT` — aggregate-safe, compiler-enforced, zero existing callers broken.
- **ADR-002**: Kitchen board via flat projection off `PedidoOrdenesTrabajo` — avoids N+1, no full aggregate load, read-side only.
- **ADR-003**: `IKitchenNotifier` port for SignalR — Application decoupled, additive, post-commit broadcast, REST authoritative for reconnects.
- **ADR-004**: Role enforcement at Application layer — mirrors Phase-5 pattern, JWT claim parse at endpoint, handler gate.
- **ADR-005**: Enum strings via global `JsonStringEnumConverter` — consistent with project W-03 convention, no new serialization logic.

---

## Test Coverage

**Total: 302/302 PASS**

- **Domain.Tests:** 166 (core logic, state machines, aggregate invariants)
- **Application.Tests:** 26 (handlers, early-fail recipe checks, role gates, notifier contract)
- **Infrastructure.Tests:** 33 (projections, batch loads, persistence)
- **Api.Tests:** 77 (endpoint scenarios, contract invariance, HTTP status codes, auth)

All test suites green. No regressions after visibility change (AsignarCocinero internal).

---

## Artifacts Archived

All 7 change artifacts moved to `openspec/changes/archive/2026-06-17-ordentrabajo-workflow/`:

1. **explore.md** (ID: #82) — Gap analysis, state machine, approach options
2. **proposal.md** (ID: #83) — Intent, scope, success criteria, affected areas
3. **spec.md** (ID: #84) — Delta spec OT-01..OT-06, Status: RECONCILED (reconciled 204, added note on cosmetic header fix)
4. **design.md** (ID: #85) — Architecture, locked signatures, ADR-001..ADR-005, routes table
5. **tasks.md** (ID: #86) — 18 tasks OW-01..OW-18, dependency order, workload forecast
6. **verify-report.md** (ID: #87) — PR1 verification: PASS WITH WARNINGS, all prior findings resolved
7. **verify-report-pr2.md** (ID: #88) — PR2 verification: PASS, additive guarantee confirmed

---

## Phase 6 Closure

**Scope:** Expose kitchen workflow (OrdenTrabajo) over API.  
**Status:** COMPLETE. Kitchen workflow fully exposed.

**Endpoints Delivered:**
- `POST /pedidos/{pedidoId}/ordenes-trabalho` — generate OTs (mozo/admin)
- `POST /pedidos/{pedidoId}/ordenes-trabalho/{otId}/asignar-cocinero` — assign cook (cocinero/admin)
- `POST /pedidos/{pedidoId}/ordenes-trabalho/{otId}/marcar-lista` — mark ready (cocinero/admin)
- `GET /ordenes-trabalho?estado=` — kitchen board (cocinero/admin, flat projection)
- SignalR hub `/hubs/kitchen` — realtime OT state deltas (PR2)

**No Follow-Ups:** All requirements, design decisions, and test cases finalized. Kitchen workflow ready for feature parity with strangler.

---

## Merged Commits

- **PR1 Commit:** 2fe7a75 (feat(contracts+api): OW-10..OW-11 — OrdenTrabajo contracts, endpoints, Program.cs)
- **PR2 Commit:** 2b96b53 (feat(app): OW-15..OW-18 — SignalR realtime layer)

Both commits are on branch `main` and available for deployment.

---

## Archive Metadata

| Field | Value |
|-------|-------|
| Change Name | ordentrabajo-workflow |
| Archive Date | 2026-06-17 |
| Archive Path | openspec/changes/archive/2026-06-17-ordentrabajo-workflow/ |
| Total Artifacts | 7 files |
| Phase | 6 of 7 |
| PR Count | 2 (chained, stacked-to-main) |
| Total Tasks | 18 (OW-01..OW-18) |
| Final Test Count | 302/302 PASS |
| Build Status | 0 errors, 270 pre-existing warnings |
| Verification Verdict | PR1: PASS WITH WARNINGS (all resolved) + PR2: PASS |

---

## Rollback Plan (if needed)

- **PR1 revert:** Remove new Application/Contracts/Api files + repository methods + DI. Revert `AsignarCocinero` visibility to `public`. No migration rollback.
- **PR2 revert:** Drop hub, adapter, DI binding, handler notifier calls. REST board unaffected (remains source of truth).

No migrations added; schema unchanged. Reverting is safe and has no data consequences.

---

End of Archive Report. Change is CLOSED.
