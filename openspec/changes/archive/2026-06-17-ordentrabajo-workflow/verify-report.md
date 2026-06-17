# Verification Report: ordentrabajo-workflow PR1 (OW-01..OW-14) — RE-VERIFY

**Change:** ordentrabajo-workflow
**Branch:** feat/ot-workflow-pr1a
**PR:** #14
**Scope:** OW-01..OW-14 only (PR2 SignalR out of scope)
**Date:** 2026-06-17
**Run:** Re-verify after remediation of prior FAIL
**Verdict:** PASS WITH WARNINGS — 0 CRITICAL, 0 WARNING, 1 SUGGESTION (cosmetic)

---

## Build + Tests

- Build: 0 errors (265 pre-existing CA1707 warnings — unchanged, unrelated)
- Tests: 298/298 PASS
  - Domain.Tests: 166/166
  - Application.Tests: 22/22
  - Infrastructure.Tests: 33/33
  - Api.Tests: 77/77
- Uncommitted .csproj files: None
- All OW-01..OW-14 tasks marked complete

---

## Prior Findings — Resolution Status

### CRITICAL-01 — RESOLVED

**Was:** GET /ordenes-trabalho board had no role gate. MOZO received 200 OK.

**Evidence:**
- `OrdenTrabajoEndpoints.cs` lines 103–112: `ClaimTypes.Role` parsed via `Enum.TryParse<RolUsuario>`. Missing/unparseable → 403. Role not in `(Cocinero or Administrador)` → 403. Same pattern as mutation endpoints.
- Integration test `GET_OrdenesTrabajo_WrongRole_Returns403` (line 581): Mozo client → asserts `HttpStatusCode.Forbidden`. PASSING.

**Status: RESOLVED.**

---

### WARNING-01 — RESOLVED

**Was:** Spec OT-01-A said "HTTP 201 Created" — code correctly returns 204 NoContent.

**Evidence:**
- `openspec/changes/ordentrabajo-workflow/spec.md` line 37: "AND returns HTTP 204 NoContent"
- Engram spec (#93) OT-01-A annotated: [RECONCILED: was 201, code returns 204 — 204 is correct per RFC 9110]
- Integration test asserts `HttpStatusCode.NoContent`.

**Status: RESOLVED (spec content correct).**

---

### WARNING-02 — RESOLVED

**Was:** OT-03-D and OT-03-E had no integration tests.

**Evidence:**
- `POST_MarcarLista_OtNotInPreparandose_Returns422` (line 478): OT in Creada, marcar-lista → asserts 422. Covers OT-03-D.
- `POST_MarcarLista_PedidoNotFound_Returns404` (line 496): random Guids → asserts 404. Covers OT-03-E.
- Both passing (part of 77/77 Api.Tests).

**Status: RESOLVED.**

---

### SUGGESTION-01 — RESOLVED

**Was:** OT-04-B test did not assert Cancelada OTs are excluded.

**Evidence:**
- Test `GET_OrdenesTrabajo_NoFilter_Returns200_ExcludesCancelada` (line 531): cancels Pedido, cascades OTs to Cancelada, asserts `DoesNotContain` by pedidoId on board response.

**Status: RESOLVED.**

---

## Spec Compliance Matrix (PR1 scope)

| Scenario | Test | Result |
|----------|------|--------|
| OT-01-A 204 NoContent + OT Creada | POST_GenerarOrdenesTrabajo_HappyPath_Returns204_CreatesBoardItems | PASS |
| OT-01-B 422 unconfirmed price | POST_GenerarOrdenesTrabajo_UnconfirmedPrice_Returns422 | PASS |
| OT-01-C 422 empty recipe | POST_GenerarOrdenesTrabajo_EmptyRecipe_Returns422_NothingPersisted | PASS |
| OT-01-D 404 Pedido not found | POST_GenerarOrdenesTrabajo_PedidoNotFound_Returns404 | PASS |
| OT-01-E 409 OTs already exist | POST_GenerarOrdenesTrabajo_AlreadyGenerated_Returns409 | PASS |
| OT-02-A 200 Cocinero assigns | POST_AsignarCocinero_Cocinero_Returns200_OtIsPreparandose | PASS |
| OT-02-A 200 Admin assigns | POST_AsignarCocinero_Administrador_Returns200 | PASS |
| OT-02-B 403 Mozo | POST_AsignarCocinero_Mozo_Returns403 | PASS |
| OT-02-B 403 no role claim | POST_AsignarCocinero_WithoutRole_Returns403 | PASS |
| OT-02-B 403 bogus role | POST_AsignarCocinero_BogusRole_Returns403 | PASS |
| OT-02-C 422 OT not Creada | POST_AsignarCocinero_AlreadyPreparandose_Returns422 | PASS |
| OT-02-D 404 Pedido not found | POST_AsignarCocinero_PedidoNotFound_Returns404 | PASS |
| OT-03-A 200 OT Lista | POST_MarcarLista_Cocinero_Returns200_OtIsLista | PASS |
| OT-03-B 200 + Pedido auto-advances | POST_MarcarLista_LastOt_NonSalon_AutoAdvancesPedidoToListoParaEntregar | PASS |
| OT-03-C 403 wrong role | POST_MarcarLista_WrongRole_Returns403 | PASS |
| OT-03-D 422 OT not Preparandose | POST_MarcarLista_OtNotInPreparandose_Returns422 | PASS |
| OT-03-E 404 Pedido not found | POST_MarcarLista_PedidoNotFound_Returns404 | PASS |
| OT-04-A 200 estado filter | GET_OrdenesTrabajo_WithEstadoFilter_ReturnsOnlyMatching | PASS |
| OT-04-B 200 no filter + Cancelada excluded | GET_OrdenesTrabajo_NoFilter_Returns200_ExcludesCancelada | PASS |
| OT-04-C 403 MOZO | GET_OrdenesTrabajo_WrongRole_Returns403 | PASS |
| OT-04-D 401 unauthenticated | GET_OrdenesTrabajo_Unauthenticated_Returns401 | PASS |
| OT-06 PedidoResponse unchanged | GET_PedidoById_AfterGeneratingOTs_ResponseShapeUnchanged | PASS |

---

## Remaining Issues

### SUGGESTION (cosmetic, resolved in archive)

**SUGGESTION-01:** `openspec/changes/ordentrabajo-workflow/spec.md` header (line 6) read `**Status:** DRAFT`. Spec content is correct. Header updated to `**Status:** RECONCILED` during archive.

---

## Task Completion

OW-01..OW-14: all complete [x].
OW-15..OW-18 (PR2 SignalR): completed separately.

---

## Merge Readiness

PR #14 MERGED (commit 2fe7a75).
0 CRITICAL. 0 WARNING. 1 SUGGESTION (cosmetic — resolved).
