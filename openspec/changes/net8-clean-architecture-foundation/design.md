# Technical Design — .NET 8 Clean Architecture Foundation

This design locks the exact physical layout, project graph, build-property scoping, configuration model, and repository-hygiene mechanics for **phase 1 of 7** of the GastroGestion modernization. It produces an empty-but-runnable .NET 8 LTS solution using **pragmatic Clean Architecture** (no CQRS, no MediatR). It ports zero business logic. The legacy net48 solution stays untouched and buildable for the strangler migration that follows.

> Scope guard: this document decides the **HOW at architecture level** — folders, references, packages, wiring. It does NOT define entities, `DbContext`, mapping code, auth implementation, or real endpoints. Those are later phases.

---

## Quick path (what gets built, in order)

1. Add a root `.gitignore` (including `GastroGestionBlazor/`), then untrack already-committed build artifacts (`git rm --cached`) as an **isolated step**.
2. Create `src/` and `tests/` trees with the 7 projects and the new `src/GastroGestion.sln`.
3. Add `src/Directory.Build.props` (scoped to the new tree only) and a root `.editorconfig`.
4. Wire the Clean Architecture `ProjectReference` graph.
5. Wire the API composition root: Serilog, Swagger, config/user-secrets pattern, and the `GET /health` endpoint.
6. Prove it: `dotnet build src/GastroGestion.sln` succeeds and `GET /health` returns healthy.

---

## 1. Solution and folder layout

The new solution is **physically isolated** from legacy by living entirely under `src/` and `tests/`. Legacy projects (`APIs`, `BLL`, `DLL`, `DTO`, `Dominio`, `Servicios`, `Apis Servicios`) and the legacy root `GastroGestion.sln` stay exactly where they are.

```text
GastroGestion/                         (repo root — legacy lives here, untouched)
├─ .gitignore                          (NEW — root, .NET standard ignore; includes GastroGestionBlazor/)
├─ .editorconfig                       (NEW — root, applies repo-wide formatting)
├─ GastroGestion.sln                   (LEGACY net48 solution — unchanged)
├─ APIs/ BLL/ DLL/ DTO/ Dominio/ ...   (LEGACY net48 projects — unchanged)
├─ GastroGestionBlazor/                (SEPARATE GIT REPO — frontend, NOT tracked here, gitignored)
│
├─ src/                                (NEW — net8.0 runtime projects)
│  ├─ GastroGestion.sln                (NEW solution; distinct directory avoids name clash)
│  ├─ Directory.Build.props            (NEW — net8 shared props, SCOPED to src/ + tests/)
│  ├─ GastroGestion.Domain/
│  │  └─ GastroGestion.Domain.csproj
│  ├─ GastroGestion.Application/
│  │  └─ GastroGestion.Application.csproj
│  ├─ GastroGestion.Infrastructure/
│  │  └─ GastroGestion.Infrastructure.csproj
│  ├─ GastroGestion.Api/
│  │  ├─ GastroGestion.Api.csproj
│  │  ├─ Program.cs                    (composition root: Serilog, Swagger, /health)
│  │  ├─ appsettings.json
│  │  └─ appsettings.Development.json
│  └─ GastroGestion.Contracts/         (API request/response DTOs — future OpenAPI contract source)
│     └─ GastroGestion.Contracts.csproj
│
└─ tests/                              (NEW — net8.0 test projects)
   ├─ GastroGestion.Domain.Tests/
   │  └─ GastroGestion.Domain.Tests.csproj
   └─ GastroGestion.Application.Tests/
      └─ GastroGestion.Application.Tests.csproj
```

**Decision — solution name & location.** The new solution is `src/GastroGestion.sln`. It shares the *name* with legacy `GastroGestion.sln` but lives in a different directory, so there is no file collision and no rename of legacy. Tooling always targets a solution by explicit path (`dotnet build src/GastroGestion.sln`), so the duplicate name is harmless and keeps the brand consistent. (Open question from the proposal resolved: keep the brand name, separate by directory — no `GastroGestion.Modern.sln`.)

**Decision — `Directory.Build.props` placement.** It lives at `src/Directory.Build.props`, NOT at repo root. MSBuild walks **up** the directory tree from each project to find the nearest `Directory.Build.props`. Legacy projects sit at the repo root (above `src/`), so a props file inside `src/` is invisible to them. This is the single most important coexistence guard: a root-level props file would impose `net8.0` / nullable on the net48 projects and break the legacy build.

> Note: `src/` and `tests/` are sibling folders, so `tests/` projects do NOT automatically inherit `src/Directory.Build.props`. We handle this in section 5.

---

## 2. Per-project responsibility and reference graph

The dependency rule of Clean Architecture: **dependencies point inward; Domain depends on nothing.**

| Project | Type | Responsibility (this slice) | References |
|---------|------|------------------------------|------------|
| `GastroGestion.Domain` | classlib | Entities/value objects/domain interfaces home. Empty now. | **none** |
| `GastroGestion.Application` | classlib | Use-case/service layer + abstraction ports. Empty now. | Domain |
| `GastroGestion.Infrastructure` | classlib | EF Core, external services (later). Empty now. | Application |
| `GastroGestion.Api` | webapi | HTTP host + composition root. Health endpoint only. | Application, Infrastructure, Contracts |
| `GastroGestion.Contracts` | classlib | API request/response DTOs. Future OpenAPI contract source for frontend client generation. Empty now. | **none** |
| `GastroGestion.Domain.Tests` | xunit | Tests for Domain. Placeholder test. | Domain |
| `GastroGestion.Application.Tests` | xunit | Tests for Application. Placeholder test. | Application |

### Reference graph (the contract)

```text
            Domain  ◄────────────  Application  ◄──────  Infrastructure
              ▲                        ▲                       ▲
              │                        │                       │
        Domain.Tests          Application.Tests                │
                                                               │
                                       Api ──────────────► Application
                                       Api ──────────────► Infrastructure
                                       Api ──────────────► Contracts

                                   Contracts  (no outbound references)
```

**Decision — `Contracts` references nothing (not Domain).** `Contracts` holds API request/response DTOs that cross the HTTP boundary. If `Contracts` referenced `Domain`, domain types would risk leaking to API consumers and the boundary would blur. Keeping `Contracts` dependency-free makes it a pure contract surface and the future OpenAPI contract source. Mapping between Domain types and Contracts DTOs happens in `Application`/`Api` via Mapperly or manual code (deferred phase).

**Decision — `Api` references `Application`, `Infrastructure`, and `Contracts`.** `Api` is the composition root; it knows about concrete infrastructure for DI wiring, and it knows about Contracts to bind HTTP request/response types. This keeps `Application` and `Infrastructure` free of HTTP concerns.

**Decision — Blazor WASM frontend is out of scope.** The frontend lives in the separate `GastroGestionBlazor` repository and is not referenced in `src/GastroGestion.sln`. The cross-repo contract strategy is a deferred open decision (see section 10).

**Forbidden edges (must never appear):** Domain→anything, Application→Infrastructure, Application→Api, Contracts→Domain/Application/Infrastructure/Api. These are the Clean Architecture invariants this scaffold exists to protect.

---

## 3. Package choices (scaffold only)

Pin to the .NET 8 LTS line. **No EF Core, no auth packages yet** — they arrive in their dedicated phases.

| Project | Package | Version | Why |
|---------|---------|---------|-----|
| `Api` | `Serilog.AspNetCore` | 8.0.x | Structured logging skeleton; replaces legacy SQL-table logger. |
| `Api` | `Swashbuckle.AspNetCore` | 6.6.x | OpenAPI/Swagger UI in Development. |
| `*.Tests` | `Microsoft.NET.Test.Sdk` | 17.10.x | Test host. |
| `*.Tests` | `xunit` | 2.8.x | Test framework. |
| `*.Tests` | `xunit.runner.visualstudio` | 2.8.x | IDE/CLI test discovery. |
| `*.Tests` | `FluentAssertions` | 6.12.x | Readable assertions for the strict-TDD phases ahead. |
| `*.Tests` | `coverlet.collector` | 6.0.x | Coverage collection (template default). |

> **Explicitly deferred (do NOT add now):** `Microsoft.EntityFrameworkCore.*` (Infrastructure phase), `Microsoft.AspNetCore.Authentication.JwtBearer` / Identity / BCrypt (API+Security phase), `Riok.Mapperly` or `FluentValidation` (Application phase). Adding them in this slice would be dead weight with nothing to wire.

> Versions use the latest 8.0/stable patch available at build time. Exact patch numbers are confirmed when the `dotnet add package` runs; the major/minor line above is the contract.

---

## 4. Configuration and secrets

**Principle: no secret is ever hardcoded.** This directly remediates the legacy hardcoded JWT secret (`"Aguante River Plate"`).

### `appsettings.json` (committed, no secrets)

```jsonc
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [ { "Name": "Console" } ]
  },
  "Jwt": {
    "Issuer": "GastroGestion",
    "Audience": "GastroGestion",
    "SigningKey": ""          // placeholder ONLY — real value comes from user-secrets/env
  },
  "AllowedHosts": "*"
}
```

The `Jwt` block is laid out now so later phases have a stable shape to bind, but the `SigningKey` stays empty in committed config.

### Secret sourcing

| Environment | Source of `Jwt:SigningKey` |
|-------------|----------------------------|
| Local dev | .NET **user-secrets** (`dotnet user-secrets set "Jwt:SigningKey" "<dev-key>"`) — stored outside the repo in the user profile, never committed. |
| CI / production | Environment variable `Jwt__SigningKey` (double-underscore convention) or a secrets vault. |

The default ASP.NET Core configuration builder already layers `appsettings.json` → `appsettings.{Environment}.json` → user-secrets (Development) → environment variables, last-wins. So the API reads `Configuration["Jwt:SigningKey"]` and the right source wins per environment **without any custom code**. We enable user-secrets on `GastroGestion.Api.csproj` via a `<UserSecretsId>` so the dev flow works out of the box.

> This slice does NOT consume the key (no auth yet). It only proves the *pattern* is in place: the placeholder is empty, user-secrets is enabled, and there is no secret in source control.

---

## 5. `Directory.Build.props` and `.editorconfig`

### `src/Directory.Build.props` (scoped — applies to `src/` projects only)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

> All five `src/` runtime projects and both `tests/` projects inherit these settings. The Blazor frontend is out of scope and lives in its own separate repo.

### `tests/` framework inheritance

Because `tests/` is a sibling of `src/`, it will NOT see `src/Directory.Build.props`. Two acceptable options:

| Option | Mechanism | Chosen |
|--------|-----------|--------|
| A | Add a tiny `tests/Directory.Build.props` that imports the `src/` one: `<Import Project="../src/Directory.Build.props" />`. | ✅ **Chosen** — keeps one source of truth for shared flags. |
| B | Repeat the props inline in each test `.csproj`. | Rejected — duplication drifts over time. |

**Decision:** add `tests/Directory.Build.props` that imports `../src/Directory.Build.props`. Both trees share net8/nullable/analyzer settings; legacy at the root still sees nothing.

### Root `.editorconfig` (repo-wide formatting baseline)

Placed at repo root so both new and legacy code share consistent whitespace, but it carries **only formatting** rules (safe for net48). Key rules:

```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
indent_style = space
trim_trailing_whitespace = true

[*.cs]
indent_size = 4
dotnet_sort_system_directives_first = true
csharp_new_line_before_open_brace = all
csharp_prefer_braces = true:suggestion
dotnet_style_namespace_match_folder = true:suggestion

[*.{json,xml,csproj,props}]
indent_size = 2
```

> We deliberately keep `.editorconfig` to formatting-level rules at the root. Hard analyzer/language rules that could fail the net48 build live in `src/Directory.Build.props`, not in the shared editorconfig.

---

## 6. Health-check endpoint (the "it runs" proof)

The acceptance proof that the skeleton is alive.

| Aspect | Decision |
|--------|----------|
| Registration | `builder.Services.AddHealthChecks();` |
| Mapping | `app.MapHealthChecks("/health");` (minimal-API style in `Program.cs`) |
| Route | `GET /health` |
| Success response | HTTP `200` with body `Healthy` (ASP.NET default health writer). |
| Swagger | `AddEndpointsApiExplorer()` + `AddSwaggerGen()`; UI mapped only in `Development`. |
| Serilog | `builder.Host.UseSerilog(...)` reading from configuration; request logging via `app.UseSerilogRequestLogging()`. |

`Program.cs` is the **composition root** and the only file with meaningful wiring this slice: Serilog bootstrap, Swagger (Development-gated), health checks, and the `/health` map. No controllers, no other endpoints.

---

## 7. Strangler coexistence (two frameworks, one repo)

| Concern | How it is handled |
|---------|-------------------|
| Build isolation | New code only under `src/`+`tests/`; legacy only at root. No shared project files. |
| Props leakage | `Directory.Build.props` lives in `src/` (and imported into `tests/`), never at root — legacy net48 never sees net8 props. **Verification gate: legacy `GastroGestion.sln` must still build after this change.** |
| SDK confusion | Always build by explicit solution path. Never run `dotnet build` blind at the repo root (it would try to build legacy net48 projects with the net8 SDK). |
| Tooling on dev machines | net48 needs the .NET Framework Developer Pack / VS; net8 needs the .NET 8 SDK. Both required during coexistence. |
| Onboarding clarity | README/onboarding note (later doc task) states `src/GastroGestion.sln` is "the new solution." |

**Decision — coexist in place, do NOT relocate legacy to `legacy/`.** Moving the legacy projects in this same slice would add massive path churn on top of the new scaffold and inflate the diff dangerously. A deliberate `legacy/` relocation, if ever wanted, is its own small future change. This slice only *adds*; it does not move legacy.

---

## 8. Repository hygiene (`.gitignore` + untracking artifacts)

The repo currently tracks `bin/`, `obj/`, `.vs/`, `packages/`, and even compiled DLLs. This must be fixed before the new solution adds more generated files.

### Strategy

1. **Add root `.gitignore`** based on the standard .NET / `dotnet new gitignore` template, ignoring at minimum: `bin/`, `obj/`, `.vs/`, `packages/`, `*.user`, `TestResults/`, `[Dd]ebug/`, `[Rr]elease/`, `artifacts/`. Also add `GastroGestionBlazor/` to exclude the nested frontend repo from backend tracking. One ignore at root covers both legacy and new trees.
2. **Untrack already-committed artifacts** with `git rm -r --cached` for the patterns above, then commit. The working files stay on disk; only the index entries are removed.
3. **Isolate the untracking commit.** This removal touches thousands of generated files and produces a huge diff. It MUST be its own commit (or its own task), separate from the scaffold commit, so reviewers can `git log` past it and the real scaffold diff stays readable.

### Large-diff risk callout

| Risk | Mitigation |
|------|------------|
| The `git rm --cached` diff drowns the real scaffold changes. | Separate, clearly-labelled commit: `chore: stop tracking build artifacts`. Reviewers skim it as mechanical, not logical. |
| Accidentally untracking a needed file. | Only untrack patterns that the new `.gitignore` lists; restrict to generated-artifact globs (`bin/`, `obj/`, `.vs/`, `packages/`, `TestResults/`). Never blanket `git rm -r --cached .` without re-add review. |
| Legacy `packages/` removal breaks legacy restore. | net48 `packages/` is restored by NuGet on build; untracking does not delete from disk, and a fresh clone restores via `packages.config`. Verify legacy build after untracking. |

---

## 9. The `GastroGestionBlazor/` repository (two-repo topology)

`GastroGestionBlazor/` is NOT an empty marker. It is a **separate git repository** nested inside the backend repo's working directory. It contains a live Blazor WASM frontend with auth wired, Clientes and Ingredientes pages, HTTP services, and a manual copy of the legacy `Dominio`/`DTO` files (using runtime AutoMapper).

**Decision (confirmed):** the system uses **two separate repositories** — this repo is the backend only; `GastroGestionBlazor` is the frontend. This change does NOT touch the frontend repo in any way.

**Action for this change:** add `GastroGestionBlazor/` to the backend repo's root `.gitignore` so the backend repo never tracks the nested frontend repo. This is all that is needed.

**Deferred open decision — cross-repo contract strategy:** the frontend currently carries a manual copy of `Dominio`/`DTO` + runtime AutoMapper. This is a known technical debt to resolve in a later phase. The recommended approach is OpenAPI/Swagger-driven client generation: the backend publishes a Swagger spec from `GastroGestion.Contracts`; the frontend uses NSwag or Kiota to generate a typed HTTP client and DTOs. An alternative is publishing `GastroGestion.Contracts` as a shared NuGet package. Either way, `GastroGestion.Contracts` is positioned now as the backend's authoritative contract source. No action taken in this slice.

---

## 10. ADR-style decision log

| # | Decision | Alternatives rejected | Rationale |
|---|----------|-----------------------|-----------|
| D1 | Pragmatic Clean Architecture, no CQRS/MediatR | CQRS + MediatR pipeline | Domain is CRUD-heavy; CQRS adds ceremony (separate read/write models, handler explosion) with no payoff. Revisit only for a genuine heavy-read case. |
| D2 | One backend solution under `src/`, legacy stays at root; frontend in its own separate repo | (a) Single monorepo for backend + frontend; (b) immediately move legacy to `legacy/` | `GastroGestionBlazor` is already a live separate repo — keeping it separate is the actual state. One backend repo keeps strangler references trivial. Relocating legacy now = huge risky path churn on top of scaffold. Coexist-in-place is the lowest-risk first slice. |
| D3 | New solution = `src/GastroGestion.sln` (same brand name, different dir) | `GastroGestion.Modern.sln`; rename legacy | Different directory removes the file clash; explicit-path builds make the duplicate name harmless; keeps brand consistent and avoids touching legacy. |
| D4 | `Directory.Build.props` inside `src/` (+ import into `tests/`) | Root-level props; per-project duplication | MSBuild upward walk means a `src/` props file is invisible to root-level legacy net48 projects — the key guard against breaking legacy. Single source of truth. |
| D5 | `Contracts` references nothing (not Domain) | `Contracts → Domain` | Keeps the HTTP contract surface free of domain types; prevents domain leaking to API consumers; mapping bridges domain types and DTOs in Application/Api. Positions `Contracts` as the future OpenAPI contract source for frontend client generation. |
| D6 | Frontend lives in its own separate repo (`GastroGestionBlazor`); gitignored in backend repo | Fold Blazor WASM into the backend solution; salvage its DTOs | `GastroGestionBlazor` is a live separate repo, not an empty marker. Two-repo topology is the actual state. Cross-repo contract sharing (OpenAPI client generation vs. shared NuGet) is a deferred decision. |
| D7 | No EF Core / auth / mapping / validation packages this slice | Add them now "to save a step" | They would be dead weight with nothing to wire and would muddy the scaffold's "empty-but-runnable" contract. Each lands in its dedicated phase. |
| D8 | Secrets via user-secrets (dev) + env vars (CI/prod); empty placeholder in committed config | Hardcode a key; commit a real key | Directly remediates legacy hardcoded JWT secret; standard ASP.NET Core config layering needs no custom code. |
| D9 | `.editorconfig` at root carries formatting only; hard analyzer rules in `src/` props | Full analyzer ruleset at root | A root analyzer ruleset could fail the legacy net48 build. Formatting is safe to share; enforcement is scoped to new code. |

---

## Checklist (reviewer can confirm)

- [ ] `src/GastroGestion.sln` exists with all 7 projects; legacy root `.sln` untouched.
- [ ] Reference graph matches section 2; no forbidden edges (Domain→none, App→Domain only, Contracts→none, etc.).
- [ ] `Directory.Build.props` is in `src/` (not root); `tests/Directory.Build.props` imports it.
- [ ] No EF Core / auth packages present; only the section-3 scaffold packages.
- [ ] `appsettings.json` has an empty `Jwt:SigningKey`; user-secrets enabled on `Api`; no secret in source.
- [ ] `GET /health` returns 200 `Healthy`; Swagger reachable in Development.
- [ ] Root `.gitignore` added; `GastroGestionBlazor/` entry present; build artifacts untracked in a separate, labelled commit.
- [ ] `GastroGestionBlazor/` is NOT deleted — it is gitignored in the backend repo only.
- [ ] **Legacy `GastroGestion.sln` still builds after the change** (props-leak verification).

## Next step

Proceed to `sdd-tasks` (requires both spec and this design). Tasks will sequence: gitignore+untrack+gitignore-frontend-repo (isolated) → scaffold 7 projects → props/editorconfig → reference wiring → API composition root + health → build/health verification.
