# Verification Report — ordentrabajo-workflow PR2

**Change:** ordentrabajo-workflow  
**Slice:** PR2 — Realtime Layer (SignalR, additive)  
**Branch:** feat/ot-workflow-pr2-signalr  
**Date:** 2026-06-17  
**Scope:** OW-15..OW-18 only (OW-01..OW-14 already verified + merged as PR1)  
**Mode:** Standard Mode (strict_tdd: false)

---

## Build Evidence

| Metric | Result |
|--------|--------|
| `dotnet build src/GastroGestion.sln` | **0 errors, 270 warnings (CA1707 naming — pre-existing)** |
| Uncommitted `.csproj` files | **None** |
| PR2 diff files | 8 files changed, 256 insertions (+20 deletions from ctor updates) |

---

## Test Suite Evidence

| Project | Tests | Passed | Failed | Skipped |
|---------|-------|--------|--------|---------|
| Domain.Tests | 166 | 166 | 0 | 0 |
| Application.Tests | 26 | 26 | 0 | 0 |
| Infrastructure.Tests | 33 | 33 | 0 | 0 |
| Api.Tests | 77 | 77 | 0 | 0 |
| **TOTAL** | **302** | **302** | **0** | **0** |

Expected: 302 (was 298 after PR1 + 4 new from OW-18). **MATCHES.**

---

## Task Completion

| Task | Description | Status |
|------|-------------|--------|
| OW-15 | IKitchenNotifier port (Application/Abstractions/Realtime) | [x] COMPLETE |
| OW-16 | KitchenHub + SignalRKitchenNotifier + DI wiring | [x] COMPLETE |
| OW-17 | Inject IKitchenNotifier into mutation handlers (post-commit) | [x] COMPLETE |
| OW-18 | 4 unit tests for notifier contract | [x] COMPLETE |

All 4 PR2 tasks checked. No unchecked implementation tasks.

---

## Spec Compliance Matrix (OT-05 scenarios)

| Scenario | Requirement | Implementation | Test Coverage | Status |
|----------|-------------|----------------|---------------|--------|
| OT-05-A: State change → hub broadcasts | AsignarCocinero/MarcarLista call NotifyOtChangedAsync | Both handlers call notifier post-commit | `AsignarCocinero_Success_CallsNotifierOnce`, `MarcarOrdenTrabajoLista_Success_CallsNotifierOnce` | PASS |
| OT-05-A: REST GET still authoritative | REST board unchanged by PR2 | PR2 does not modify OrdenTrabajoEndpoints.cs | PR1 integration tests unchanged (77 still green) | PASS |
| OT-05-B: Reconnect → GET returns full state | GET /ordenes-trabalho unaffected | Endpoint unchanged from PR1 | PR1 integration suite (KitchenEndpointTests) | PASS (structural — PR1 coverage) |

---

## Design Adherence (ADR-003)

### IKitchenNotifier port in Application layer
- File: `src/GastroGestion.Application/Abstractions/Realtime/IKitchenNotifier.cs`
- Interface uses `OrdenTrabajoBoardItem` (Application type) — no leakage of Infrastructure/Api types into Application. **PASS**

### KitchenHub at /hubs/kitchen
- `MapHub<KitchenHub>("/hubs/kitchen")` — matches design. **PASS**
- `OnConnectedAsync` adds connection to group "kitchen". **PASS**

### SignalRKitchenNotifier in Api layer (not Infrastructure)
- Namespace `GastroGestion.Api.Realtime` — correctly placed in Api, not Infrastructure. **PASS**

### DI registration
- `AddSignalR()` ✓ | `AddScoped<IKitchenNotifier, SignalRKitchenNotifier>()` ✓ | `MapHub<KitchenHub>("/hubs/kitchen")` ✓ — all 3 present in Program.cs. **PASS**

### GenerarOrdenesTrabajo does NOT push (ADR-003)
- `GenerarOrdenesTrabajoHandler.cs` has NO reference to `IKitchenNotifier` or `NotifyOtChangedAsync`. Confirmed by grep.
- Spec OT-05 triggers: "assign/ready/cancel" — "Creada" generation is NOT listed.
- Design ADR-003: "post-commit push not domain event" — OTs in Creada state, no listener.
- **ALIGNED with spec and design. PASS.**

### Broadcasts fire POST-COMMIT (critical)

AsignarCocineroHandler execution order:
1. `pedido.AsignarCocineroAOT(...)` — domain mutation
2. `await _uow.SaveChangesAsync(ct)` — commit
3. `await _kitchenNotifier.NotifyOtChangedAsync(boardItem, ct)` — broadcast

MarcarOrdenTrabajoListaHandler execution order:
1. `pedido.MarcarOrdenTrabajoLista(...)` — domain mutation
2. `await _uow.SaveChangesAsync(ct)` — commit
3. `await _kitchenNotifier.NotifyOtChangedAsync(boardItem, ct)` — broadcast

In both handlers, `NotifyOtChangedAsync` is AFTER `SaveChangesAsync`. Pre-commit broadcast is impossible by construction. **PASS.**

### Broadcast payload
- Spec OT-05: "otId, pedidoId, newEstado (string), cocineroAsignado (nullable)"
- Implementation: sends `OrdenTrabajoBoardResponse` — superset of required fields. All required fields present. **PASS (additive, not breaking).**

---

## Additive Guarantee (PR1 Non-Regression)

| Check | Result |
|-------|--------|
| PR1 test count unchanged | 77 Api.Tests + 166 Domain.Tests + 33 Infra.Tests = 276 unchanged |
| PR1 endpoint contracts | `OrdenTrabajoEndpoints.cs` NOT in PR2 diff (unchanged) |
| `PedidoResponse` unchanged | Not in PR2 diff |
| Existing handler ctor updates | Compile fix only, no behavioral change |
| All PR1 integration tests green | 77/77 passed |

**PR1 regression: NONE.**

---

## Issues

### CRITICAL
*None.*

### WARNING
*None.*

### SUGGESTION

**S-01 — Broad exception type in test**

The test `MarcarOrdenTrabajoLista_DomainThrows_DoesNotCallNotifier` uses `ThrowAsync<Exception>()` instead of the more specific `ThrowAsync<DomainException>()`. Low risk (test still covers the notifier-not-called assertion), but it weakens the failure signal. Non-blocking.

---

## Spec OT-05 Broadcast Field Note

The spec specifies a minimum payload of `{otId, pedidoId, newEstado (string), cocineroAsignado (nullable)}`. The actual broadcast sends `OrdenTrabajoBoardResponse` which includes `PedidoTipo`, `PlatoId`, and `LineaPedidoId` as additional fields. This is an additive extension — no client that only reads the specified fields is broken. No action required.

---

## Final Verdict

**PASS**

All OW-15..OW-18 requirements are fully satisfied:
- IKitchenNotifier port exists in Application layer with correct signature
- KitchenHub exists at /hubs/kitchen joining "kitchen" group on connect
- SignalRKitchenNotifier correctly adapts the port to `IHubContext<KitchenHub>`
- All 3 DI registrations are present in Program.cs
- Broadcasts fire POST-COMMIT in both mutation handlers (not pre-commit)
- GenerarOrdenesTrabajo correctly does NOT push (OTs created in Creada state, no listener per ADR-003 and OT-05 spec)
- 302/302 tests pass, 0 build errors
- PR1 not regressed

PR2 MERGED (commit 2b96b53).

1 SUGGESTION (non-blocking): tighten exception type assertion in test.
