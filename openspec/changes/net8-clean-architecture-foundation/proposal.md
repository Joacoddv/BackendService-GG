# Stand up the .NET 8 Clean Architecture foundation

Create a new, empty-but-runnable .NET 8 LTS solution structured with pragmatic Clean Architecture (no CQRS, no MediatR), so every later modernization phase has a stable skeleton to build into. This change produces the project layout, dependency wiring, repository hygiene, and a proof-of-life health endpoint. It deliberately ports **zero** business logic, entities, or persistence — those arrive in later changes via a strangler-style migration that keeps the legacy net48 solution untouched and referenceable.

## Why now

The legacy GastroGestion is a Windows-only .NET Framework 4.8 layered solution that cannot upgrade in place. The team has already decided (see engram `architecture/modernization-decisions`, obs #16) to rebuild it greenfield on .NET 8 with Clean Architecture. The system is split into **two separate repositories**: this repo is the backend only; the Blazor WebAssembly frontend lives in its own separate repo (`GastroGestionBlazor`). Nothing can be ported until the target backend solution exists. This is **phase 1 of 7** in the agreed roadmap (Bootstrap/scaffold → Domain port → Infrastructure/EF Core → Application → API+Security → Stock/Orden_Trabajo → Blazor frontend integration).

A foundation-only first slice also fixes two immediate problems that would otherwise poison the new repo from day one:

- **Build artifacts are committed.** The repo currently tracks `bin/`, `obj/`, `.vs/`, and `packages/`. The git status is dominated by churn in compiled DLLs and IDE cache files. A correct `.gitignore` must land before the new solution adds more.
- **No clean separation exists** between the legacy app and the new app. Without a deliberate layout decision, the two solutions would collide.

## What success looks like

- `dotnet build` succeeds on the new solution with .NET 8 SDK installed.
- `dotnet run` on the API serves a `GET /health` endpoint returning healthy, with Swagger UI reachable in Development.
- The legacy net48 solution still opens and builds unchanged.
- `git status` is quiet about build artifacts — only source is tracked going forward.
- The JWT signing key is read from configuration/user-secrets, never hardcoded.

## Scope

### In scope

| Area | What lands |
|------|-----------|
| Solution layout | New `GastroGestion.sln` under a `src/` + `tests/` convention; legacy solution coexists (see decision below) |
| Domain project | `GastroGestion.Domain` — class library, **no dependencies** |
| Application project | `GastroGestion.Application` — depends on Domain only |
| Infrastructure project | `GastroGestion.Infrastructure` — depends on Application; will later host EF Core 8 (no DbContext yet) |
| API project | `GastroGestion.Api` — ASP.NET Core Web API; depends on Application + Infrastructure + Contracts |
| Contracts project | `GastroGestion.Contracts` — API request/response DTOs; dependency-light (no Domain reference) |
| Test projects | `tests/GastroGestion.Domain.Tests` and `tests/GastroGestion.Application.Tests` — xUnit, empty/placeholder |
| Shared settings | `Directory.Build.props` (TargetFramework, `Nullable=enable`, `ImplicitUsings=enable`, langversion); `.editorconfig` |
| Base wiring | DI container composition root in the API; configuration + user-secrets pattern for the JWT key; Serilog skeleton; Swagger/OpenAPI skeleton; one `GET /health` endpoint |
| Repository hygiene | A proper .NET `.gitignore` excluding `bin/`, `obj/`, `.vs/`, `packages/` and build output |

### Non-goals (explicitly out of this slice)

- Porting any domain entity (Cliente, Plato, Pedido, Factura, Stock, etc.).
- Defining the EF Core 8 `DbContext`, entity configurations, or migrations.
- Any business logic, billing/IVA rules, or state machines.
- Auth **implementation** — we wire the config/secrets pattern for the key, but no login, token issuing, hashing, or `[Authorize]` policies.
- Any real (non-health) API endpoint.
- Any changes to the Blazor WASM frontend (separate repo — out of scope entirely).
- Designing or implementing the cross-repo contract-sharing strategy between backend and frontend.
- Deleting, rewriting, or fixing the legacy projects.

## Key decisions

These were confirmed in `architecture/modernization-decisions` (obs #16) and carry into this slice:

| Decision | Rationale |
|----------|-----------|
| .NET 8 LTS, greenfield solution | net48 cannot upgrade in place; the relevamiento (obs #12) documents pervasive legacy defects (plaintext passwords, hardcoded JWT secret, mixed controller generations, SQL typos) not worth carrying forward. |
| Clean Architecture, pragmatic — **no CQRS, no MediatR** | The domain is CRUD-heavy; CQRS/MediatR would be ceremony without payoff. Revisit only if a heavy-read case appears. |
| **Two separate repositories** — backend (this repo) and Blazor WASM frontend (`GastroGestionBlazor`) | `GastroGestionBlazor/` is a live separate git repository, not an empty marker. This change does not touch it. The frontend stays in its own repo. |
| Five runtime projects + two test projects | Domain/Application/Infrastructure/Api is the standard Clean Architecture core; Contracts holds API request/response DTOs and is the future OpenAPI contract source for the frontend. |
| `GastroGestion.Contracts` (renamed from "Shared") | Clarifies purpose: these are API request/response DTOs crossing the HTTP boundary, not general utilities. Dependency-light (no Domain reference). |
| JWT key from config/user-secrets | Directly remediates the hardcoded `"Aguante River Plate"` secret in the legacy `TokenService`. |
| Serilog skeleton (not SQL-table logging) | Replaces the legacy SQL-backed `LoggerManager`. |
| Mapping via Mapperly or manual (deferred) | Decided to avoid runtime AutoMapper; the actual mapping wiring is a later phase, but the Contracts project boundary is set up now. |

### New decision this slice must lock: where legacy lives during migration

**Proposed:** keep the legacy projects (`APIs`, `BLL`, `DLL`/DAL, `DTO`, `Dominio`, `Servicios`, `Apis Servicios`) **untouched, in place**, and add the new solution alongside under `src/` and `tests/`. The legacy `.sln` stays buildable so the strangler migration can open and reference legacy code while porting it, before legacy is retired in a final cleanup change.

Rationale: a physical "move legacy to `legacy/`" reorg in this same slice would add large, risky path churn on top of the new scaffold and inflate the diff. Coexistence in place is the lower-risk first slice; a deliberate `legacy/` relocation can be its own small change later if the root directory feels crowded.

## Impact

- **Repository:** new `src/` and `tests/` trees added; a `.gitignore` is introduced. After it lands, previously tracked `bin/`/`obj/`/`.vs/`/`packages/` files should be untracked (`git rm --cached`) in this change so the working tree stops churning. This is a large but mechanical removal of generated files — call it out for reviewers so the diff size is understood. `GastroGestionBlazor/` is added to `.gitignore` so the nested frontend repo is never tracked by this backend repo.
- **Build:** the repo will host **two target frameworks** — legacy net48 and new net8.0 — until legacy is retired. The new `Directory.Build.props` MUST be scoped so it does not leak its `net8.0` / nullable settings into the legacy projects (place it inside `src/` rather than at repo root, or exclude legacy explicitly).
- **Tooling:** building net48 requires the .NET Framework Developer Pack / Visual Studio on Windows; building net8.0 requires the .NET 8 SDK. Both must be present on dev machines during coexistence.
- **Team/workflow:** developers gain a second solution to open. The README/onboarding should state which solution is "the new one."

## Risks and open questions

| Risk / question | Mitigation / note |
|-----------------|-------------------|
| `Directory.Build.props` at repo root would force net8/nullable onto legacy net48 projects and break them. | Scope props under `src/`; keep root clean. **Must verify legacy still builds after this change.** |
| Untracking committed build artifacts produces a huge diff that hides real changes. | Separate the `git rm --cached` artifact-removal step in tasks; document it so reviewers don't drown. |
| Two SDKs / frameworks on one machine can confuse `dotnet build` at the repo root. | Build each solution explicitly by path; never `dotnet build` the whole folder blind. |
| Solution-name collision: legacy `GastroGestion.sln` already exists at repo root. | New solution lives under `src/GastroGestion.sln` (different directory avoids the clash); legacy `.sln` stays untouched. |
| `GastroGestionBlazor/` is a separate git repo nested inside the backend repo's working directory. | Add `GastroGestionBlazor/` to the backend repo's `.gitignore` so it is never tracked here. Do NOT delete it — it is a live frontend repo. |
| Cross-repo contract sharing between backend and frontend is unresolved. | **Deferred open decision** (not part of this foundation change): recommended approach is OpenAPI/Swagger-driven client generation (NSwag or Kiota) where the backend owns the contract via `GastroGestion.Contracts` and the frontend generates a typed client + DTOs. Alternative is publishing `GastroGestion.Contracts` as a shared NuGet package. The current frontend's manual copy of `Dominio`/`DTO` + runtime AutoMapper will be replaced in a later phase. `GastroGestion.Contracts` is positioned now as the future OpenAPI contract source. |
| Strict TDD not yet active (no test projects). | This slice creates the empty xUnit projects; strict TDD activates from the Domain-port change onward. |

## Next step

Proceed to `sdd-spec` and `sdd-design` (these can run in parallel). Spec captures the runnable-skeleton acceptance criteria (build succeeds, health endpoint, secrets pattern, gitignore); design locks the exact project graph, the `Directory.Build.props` placement, and the legacy-coexistence layout.
