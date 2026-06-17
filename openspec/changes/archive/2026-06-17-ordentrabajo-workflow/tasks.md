# Tasks: ordentrabajo-workflow (Phase 6 of 7)

**Change:** ordentrabajo-workflow  
**Date:** 2026-06-16  
**Delivery:** Chained PRs — stacked-to-main  
**Mode:** Standard Mode (strict_tdd: false) — tests alongside code  
**Spec ref:** OT-01..OT-06  
**Design ref:** openspec/changes/ordentrabajo-workflow/design.md  

---

## Summary

All 18 tasks (OW-01..OW-18) completed across two PRs:

- **PR1 — Core Workflow + REST Board** (OW-01..OW-14): Delivered 302 tests across Domain, Application, Infrastructure, Contracts, and Api layers. Both PRs merged to main: #14 (OW-01..OW-14) commit 2fe7a75 and #15 (OW-15..OW-18) commit 2b96b53.
- **PR2 — Realtime Layer** (OW-15..OW-18): SignalR kitchen notifications, additive on PR1.

**Verification status:** PASS (PR1 after remediation) + PASS (PR2).

---

## Implementation Summary

### PR1 Delivery

- Domain hardening: `AsignarCocinero` made internal; `Pedido.AsignarCocineroAOT` added (ADR-001).
- 4 Use Cases + Handlers: `GenerarOrdenesTrabajo`, `AsignarCocinero`, `MarcarOrdenTrabajoLista`, `GetOrdenesByEstado`.
- Repository extensions: `IPedidoRepository.GetAllOrdenesTrabajoAsync` (flat projection), `IPlatoRepository.GetByIdsAsync` (batch load).
- Contracts: 4-file pattern — requests, responses, validators, mappings.
- Endpoints: 3 nested mutations + 1 top-level board GET with role enforcement.
- DI: handlers registered, `ConflictException` + HTTP 409 mapping added.
- Tests: 298 total (166 Domain, 22 Application, 33 Infrastructure, 77 Api).

### PR2 Delivery

- `IKitchenNotifier` port (Application abstraction).
- `KitchenHub` + `SignalRKitchenNotifier` adapter.
- Handler injection: mutation handlers notify post-commit.
- Unit tests: 4 tests verifying notifier contract (Application.Tests, +4 to reach 302 total).
- No REST contract changes; SignalR additive and purely optional for v1.

---

## Completion Checklist

### All Requirements Met

- OT-01: Generate work orders explicitly via `POST /pedidos/{pedidoId}/ordenes-trabalho` ✓
- OT-02: Assign cook via `POST /pedidos/{pedidoId}/ordenes-trabalho/{otId}/asignar-cocinero`, role-gated (Cocinero/Admin) ✓
- OT-03: Mark ready via `POST /pedidos/{pedidoId}/ordenes-trabalho/{otId}/marcar-lista`, auto-advances Pedido ✓
- OT-04: Kitchen board via `GET /ordenes-trabalho?estado=` (flat projection, no aggregate loading) ✓
- OT-05: SignalR realtime push to "kitchen" group (PR2, additive) ✓
- OT-06: All endpoints JWT-protected, enum strings, PedidoResponse unchanged ✓

### Flags Resolved

1. **204 spec alignment (OT-01-A)**: Spec originally said HTTP 201; code correctly returns 204 NoContent. Reconciled in verify report.
2. **Board role gate (OT-04)**: Initial verify found missing gate on GET board. Added `ClaimTypes.Role` parse, 403 on unauthorized. Resolved in verification and integration test `GET_OrdenesTrabajo_WrongRole_Returns403`.

### Test Coverage

- **Domain.Tests:** 166 green (no regression after `AsignarCocinero` visibility change).
- **Application.Tests:** 22 green (handlers + early-fail recipe checks + role gates).
- **Infrastructure.Tests:** 33 green (repository projections).
- **Api.Tests:** 77 green (endpoint scenarios, contract invariance).
- **Total:** 302/302 green.

---

## Archive Artifacts

All 7 change artifacts archived under `openspec/changes/archive/2026-06-17-ordentrabajo-workflow/`:

1. **explore.md** — Gap analysis, state machine, approach options
2. **proposal.md** — Intent, scope, success criteria, affected areas
3. **spec.md** — Delta spec OT-01..OT-06 (Status: RECONCILED after 204 fix)
4. **design.md** — Architecture, locked signatures, ADR-001..ADR-005
5. **tasks.md** — 18 tasks (OW-01..OW-18) with dependency order
6. **verify-report.md** — PR1 re-verify: PASS WITH WARNINGS (0 CRITICAL, 0 WARNING, 1 SUGGESTION—cosmetic spec header, now reconciled)
7. **verify-report-pr2.md** — PR2: PASS (all 302 tests green, 4 new from OW-18)

---

## Key Decisions Documented

- **ADR-001**: `AsignarCocinero` internal + `Pedido.AsignarCocineroAOT` — aggregate-safe, compiler-enforced.
- **ADR-002**: Flat kitchen board projection off `PedidoOrdenesTrabajo` — read-side only, avoids N+1.
- **ADR-003**: `IKitchenNotifier` port for SignalR — Application layer decoupled, additive on PR1.
- **ADR-004**: Role enforcement at Application layer — mirrors Phase-5 pattern (`PedidoEndpoints`).
- **ADR-005**: Enum strings via global `JsonStringEnumConverter` — consistent with W-03 convention.

---

## Rollback Plan

- **PR1 revert:** Remove new Application/Contracts/Api files + repository methods + DI registration. Only Domain edit: revert `AsignarCocinero` visibility to `public`. No migration to rollback.
- **PR2 revert:** Independent — drop hub, adapter, DI binding, handler notifier calls. REST board unaffected; remains source of truth.

---

## Phase 6 of 7 Closure

Kitchen workflow fully exposed over API. REST + SignalR ready. No follow-ups or open questions.

**Next phase:** Phase 7 (not yet proposed).
