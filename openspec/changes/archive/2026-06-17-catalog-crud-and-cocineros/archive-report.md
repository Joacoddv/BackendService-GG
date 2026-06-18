# Archive Report — catalog-crud-and-cocineros

**Change:** `catalog-crud-and-cocineros`
**Archived:** 2026-06-17
**Project:** `backendservice-gg`
**Artifact store:** hybrid (openspec files + engram)

---

## Executive Summary

The `catalog-crud-and-cocineros` change is fully implemented, verified, and merged to main. All 52 tasks (CCC-T01..T52) were completed across 3 chained PRs that are now merged to main. The 413-test suite is green (0 failures). Delta specs have been merged into the three main spec files. The change unblocks the frontend Blazor wave: Slice C2 (`asignar-cocinero` picker) and Cliente/Ingrediente CRUD UI screens can now proceed.

---

## Final State

### Merged PRs

| PR | Branch | Merge Commit | Scope | Tasks | Verdict |
|----|--------|-------------|-------|-------|---------|
| PR A #19 | feat/cocineros-list | 8251125 | Cocineros GET endpoint | CCC-T01..T12 (12 tasks) | PASS WITH WARNINGS (0 CRITICAL) |
| PR B #20 | feat/cliente-crud | 60bd611 | Cliente CRUD (edit, soft-delete, search) | CCC-T13..T32 (20 tasks) | PASS WITH WARNINGS (0 CRITICAL) |
| PR C #21 | feat/ingrediente-crud | b3af61e | Ingrediente CRUD (edit, soft-delete, search) | CCC-T33..T52 (20 tasks) | PASS (0 CRITICAL, 1 WARNING) |

**Total tasks:** 52 / 52 complete.
**Total tests after PR C:** 413 / 413 passing (0 failures, 0 regressions vs 302-test baseline).

---

## Task Completion Reconciliation

The persisted `tasks.md` file has stale unchecked boxes for CCC-T13..T52 (PRs B and C). This is a known stale-checkbox artifact — `sdd-apply` did not update the tasks file after applying PRs B and C, but apply-progress and all three verify reports confirm every task complete. This archive performs the exceptional mechanical reconciliation with the following proof:

- **apply-progress**: all 52 tasks marked `[x]` in the Engram apply-progress observation.
- **verify-report-prA** (obs #153): CCC-T01..T12 — 12/12 COMPLETE, PASS WITH WARNINGS.
- **verify-report-prB** (obs #155): CCC-T13..T32 — 20/20 COMPLETE, PASS WITH WARNINGS.
- **verify-report-prC** (obs #156): CCC-T33..T52 — 20/20 COMPLETE, PASS (0 CRITICAL).
- **PRs merged**: GitHub confirms all 3 PRs merged to main with 413 tests green.

---

## Per-PR Verdict Detail

### PR A — Cocineros (CCC-A01, CCC-T01..T12)

- **Build:** 0 errors, 289 CA1707 style warnings (pre-existing)
- **Tests:** 158 total, 14 new — Application (+3), Infrastructure (+3), Api (+8)
- **CRITICAL:** 0
- **WARNING:** W-01 — spec text said anon → 403; implementation correctly returns 401 from ASP.NET auth middleware. Spec corrected at archive.
- **Design coherence:** ADR-CCC-4 (manual ClaimTypes.Role parse at endpoint edge) confirmed.

### PR B — Cliente CRUD (CCC-B01..B03, CCC-T13..T32)

- **Build:** 0 errors, 325 CA1707 warnings (pre-existing)
- **Tests:** 369 total — Domain (173), Application (41), Infrastructure (41), Api (114)
- **CRITICAL:** 0
- **WARNINGs (2):** (1) `NumeroCliente` not exposed as a distinct field in `ClienteResponse` — not a behavioral defect. (2) No explicit anon test for PUT/DELETE (`RequireAuthorization()` covers it at framework level).
- **Design coherence:** ADR-CCC-1 (pre-check ConflictException), ADR-CCC-3 (GetAllAsync intact), ADR-CCC-4 confirmed.

### PR C — Ingrediente CRUD (CCC-C01..C03, CCC-T33..T52)

- **Build:** 0 errors, 360 CA1707 warnings (pre-existing)
- **Tests:** 413 total — Domain (179), Application (53), Infrastructure (46), Api (135)
- **CRITICAL:** 0
- **WARNING (1):** W-01 — empty `Nombre` returns 400 (FluentValidation filter fires before handler) instead of 422 as spec said. Behavior is correct and consistent with PR B; spec corrected at archive.
- **SUGGESTION (1):** S-01 — `NombreExistsForOtherAsync` uses collation-dependent equality; acceptable for SQL Server.
- **Design coherence:** ADR-CCC-1 (UnidadBase structurally absent from DTO), ADR-CCC-3, ADR-CCC-4 confirmed.

---

## Spec Corrections Applied at Archive

Two spec-vs-implementation mismatches were identified in verify reports and corrected in the main spec store at archive time:

1. **401 vs 403 for unauthenticated callers** (W-01 from PR A and others): The spec drafts stated unauthenticated → 403. The implementation correctly returns 401 from ASP.NET Core's `RequireAuthorization()` middleware before the endpoint handler (and its role gate) is ever reached. 403 is reserved for authenticated callers whose role is insufficient. `openspec/specs/Api/spec.md` now reflects this distinction explicitly.

2. **400 vs 422 for empty-field validation** (W-01 from PR C): The spec said empty `Nombre` → 422. The FluentValidation `WithValidation<T>()` endpoint filter short-circuits at 400 before the handler or domain runs. 422 is reserved for `DomainException`/`ValidationException` violations. `openspec/specs/Api/spec.md` now states this boundary explicitly. This is consistent with the Phase-4 deferred item (open item §4 in Api spec).

---

## Delta Specs Merged to Main Spec Store

| File | Additions |
|------|-----------|
| `openspec/specs/Api/spec.md` | Added CCC-A01 through CCC-C03 requirements; updated endpoint signatures table (5 new endpoints: 2 PUT, 2 DELETE, 1 GET cocineros; updated GET clientes/ingredientes signatures); updated delivery status; added spec corrections note; updated test count to 413. |
| `openspec/specs/Domain/spec.md` | Added scenarios 03-F/G/H for `Cliente.ActualizarDatos` (CUIT-required-for-ResponsableInscripto, NumeroCliente immutable, Activo unchanged); added scenarios 04-C/D for `Ingrediente.ActualizarNombre` (UnidadBase immutable, blank rejected); noted `Desactivar()` reuse. |
| `openspec/specs/Infrastructure/spec.md` | Added REQ-16 (`IUsuarioRepository.GetByRolAsync` — active-only filter), REQ-17 (`IClienteRepository.SearchAsync + CuitExistsForOtherAsync`), REQ-18 (`IIngredienteRepository.SearchAsync + NombreExistsForOtherAsync`); confirmed `GetAllAsync` left intact on all repos. |

---

## Engram Observation IDs (Traceability)

| Artifact | Engram Observation ID | Topic Key |
|----------|-----------------------|-----------|
| Proposal | (file-only, no separate engram obs) | — |
| Spec | (file-only) | — |
| Design | (file-only) | — |
| Tasks | (file-only) | — |
| Verify Report PR A | #153 | `sdd/catalog-crud-and-cocineros/verify-report-prA` |
| Verify Report PR B | #155 | `sdd/catalog-crud-and-cocineros/verify-report-prB` |
| Verify Report PR C | #156 | `sdd/catalog-crud-and-cocineros/verify-report-prC` |
| Archive Report | (this file + engram) | `sdd/catalog-crud-and-cocineros/archive-report` |

---

## What This Change Unblocks

- **Frontend Slice C2 — asignar-cocinero picker**: `GET /usuarios/cocineros` is now available, returning active cocineros (id + display name) for the assignment UI.
- **Frontend Cliente CRUD screens**: `PUT /clientes/{id}`, `DELETE /clientes/{id}`, and `GET /clientes?nombre=&incluirInactivos=` are available for admin catalog management.
- **Frontend Ingrediente CRUD screens**: `PUT /ingredientes/{id}`, `DELETE /ingredientes/{id}`, and `GET /ingredientes?nombre=&incluirInactivos=` are available.

---

## SDD Cycle Status

COMPLETE. The change was fully planned (proposal → spec → design → tasks), implemented (3 chained PRs, standard mode, tests alongside), verified (3 verify reports, 0 CRITICAL issues), and archived.
