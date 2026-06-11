# Tasks — net8-clean-architecture-foundation

**Generated:** 2026-06-10  
**Artifact store:** openspec  
**Change:** net8-clean-architecture-foundation  
**Delivery strategy:** ask-on-risk  

---

## Dependency order

```
TASK-01 (gitignore + untrack artifacts)  ← independent; must land first
  └── TASK-02 (src/ + tests/ folder tree + 7 project files)
        └── TASK-03 (Directory.Build.props + tests import + .editorconfig)
              └── TASK-04 (ProjectReference graph)
                    └── TASK-05 (NuGet packages)
                          └── TASK-06 (API composition root: Serilog, Swagger, /health, JWT config)
                                └── TASK-07 (src/GastroGestion.sln + all projects enrolled)
                                      └── TASK-08 (build + runtime verification)
```

Tasks TASK-03 and TASK-04 can be done in the same pass (they both operate on already-created project files) but TASK-04 must not precede TASK-03 because `Directory.Build.props` must exist before project references are verified as building.  
TASK-05 and TASK-06 are sequential: packages must be added before composition-root code that uses them.  
TASK-07 (solution enrollment) should be done after the project files exist; it is low-risk and can be folded into TASK-02 if done strictly by `dotnet sln add` — however, isolating it as the final wiring step keeps the diff cleaner.  
TASK-08 is verification-only; no code changes, just commands.

---

## TASK-01 — Root .gitignore and artifact untracking [x]

**Work unit:** One commit — isolated by design to keep the huge generated-file deletion out of the scaffold diff.  
**Conventional commit:** `chore: add .gitignore and stop tracking build artifacts`

### What to do

1. Create `.gitignore` at the repository root using the standard `dotnet new gitignore` output as the base, ensuring the following patterns are present at minimum:
   - `bin/`
   - `obj/`
   - `.vs/`
   - `packages/`
   - `*.user`
   - `*.suo`
   - `*.userosscache`
   - `*.sln.docstates`
   - `TestResults/`
   - `[Dd]ebug/`
   - `[Rr]elease/`
   - `artifacts/`
   - `GastroGestionBlazor/`   ← excludes the nested frontend repo from backend tracking
2. Run `git rm -r --cached bin/ obj/ .vs/ packages/` (restrict to these exact patterns — do NOT run `git rm -r --cached .`).
3. Verify working files still exist on disk after the `--cached` removal.
4. Stage and commit.

### Must NOT do

- Do not delete any file from disk.
- Do not add `Directory.Build.props` or `.editorconfig` in this task.
- Do not touch `GastroGestionBlazor/` content in any way.

### Verification

```
# .gitignore exists and contains required patterns
grep -e "bin/" -e "obj/" -e ".vs/" -e "packages/" -e "GastroGestionBlazor/" .gitignore

# Previously tracked artifacts are no longer indexed
git ls-files bin/ obj/ .vs/ packages/
# Expected output: empty

# Working files for GastroGestionBlazor still on disk (not deleted)
ls GastroGestionBlazor/

# GastroGestionBlazor not visible to git status
git status | grep -v GastroGestionBlazor
```

### Spec requirements satisfied

- **REQ-10** (Scenarios 10-A, 10-B, 10-C) — gitignore patterns present; previously tracked build artifacts removed from index.
- **REQ-11** (Scenarios 11-A, 11-B) — `GastroGestionBlazor/` entry in .gitignore; no frontend entries visible in `git status`.

---

## TASK-02 — Create src/ and tests/ folder tree with 7 project files [x]

**Work unit:** One commit — all empty project skeletons.  
**Conventional commit:** `feat: scaffold 7 net8 project stubs under src/ and tests/`

### What to do

Create the following directory and project file structure (empty projects — no source code yet, no references):

```
src/
  GastroGestion.Domain/GastroGestion.Domain.csproj          (classlib, net8.0)
  GastroGestion.Application/GastroGestion.Application.csproj (classlib, net8.0)
  GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj (classlib, net8.0)
  GastroGestion.Api/GastroGestion.Api.csproj                 (webapi, net8.0)
  GastroGestion.Api/appsettings.json                         (placeholder — see TASK-06)
  GastroGestion.Api/appsettings.Development.json             (placeholder — see TASK-06)
  GastroGestion.Contracts/GastroGestion.Contracts.csproj     (classlib, net8.0)

tests/
  GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj     (xunit, net8.0)
  GastroGestion.Application.Tests/GastroGestion.Application.Tests.csproj (xunit, net8.0)
```

Use `dotnet new` templates to generate each project. Do not hand-craft `.csproj` XML from scratch.

Example commands:
```
dotnet new classlib -n GastroGestion.Domain -o src/GastroGestion.Domain --framework net8.0
dotnet new classlib -n GastroGestion.Application -o src/GastroGestion.Application --framework net8.0
dotnet new classlib -n GastroGestion.Infrastructure -o src/GastroGestion.Infrastructure --framework net8.0
dotnet new webapi -n GastroGestion.Api -o src/GastroGestion.Api --framework net8.0 --use-minimal-apis
dotnet new classlib -n GastroGestion.Contracts -o src/GastroGestion.Contracts --framework net8.0
dotnet new xunit -n GastroGestion.Domain.Tests -o tests/GastroGestion.Domain.Tests --framework net8.0
dotnet new xunit -n GastroGestion.Application.Tests -o tests/GastroGestion.Application.Tests --framework net8.0
```

Remove the default generated placeholder files that the templates add (e.g. `Class1.cs`, `WeatherForecast.cs`, template controller stubs) so each project is truly empty. Keep the `appsettings*.json` files generated for Api — they will be populated in TASK-06.

### Must NOT do

- Do not add `<ProjectReference>` elements in this task.
- Do not add NuGet packages in this task.
- Do not add `src/GastroGestion.sln` in this task (deferred to TASK-07).
- Do not add `Directory.Build.props` in this task (next task).
- Do not set `UserSecretsId` yet (TASK-06).

### Verification

```
# All 7 .csproj files exist
ls src/GastroGestion.Domain/GastroGestion.Domain.csproj
ls src/GastroGestion.Application/GastroGestion.Application.csproj
ls src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
ls src/GastroGestion.Api/GastroGestion.Api.csproj
ls src/GastroGestion.Contracts/GastroGestion.Contracts.csproj
ls tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj
ls tests/GastroGestion.Application.Tests/GastroGestion.Application.Tests.csproj

# Each project builds individually (TFM check)
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
dotnet build tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj
```

### Spec requirements satisfied

- **REQ-02** (Scenarios 02-A) — seven projects exist with correct names and TFMs.

---

## TASK-03 — Directory.Build.props and .editorconfig [x]

**Work unit:** One commit — build settings baseline.  
**Conventional commit:** `build: add Directory.Build.props and .editorconfig`

### What to do

1. Create `src/Directory.Build.props`:

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

2. Create `tests/Directory.Build.props` that imports the src one (single source of truth):

```xml
<Project>
  <Import Project="../src/Directory.Build.props" />
</Project>
```

3. Create `.editorconfig` at the **repository root** (formatting-only rules; safe for net48):

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

### Must NOT do

- Do not place `Directory.Build.props` at the repository root — only at `src/` and `tests/`.
- Do not add hard analyzer/language rules that could affect legacy net48 in `.editorconfig`.

### Verification

```
# Files exist at correct locations
ls src/Directory.Build.props
ls tests/Directory.Build.props
ls .editorconfig

# src props NOT at repo root
ls Directory.Build.props   # must not exist

# Legacy solution still builds (props scoping test)
msbuild GastroGestion.sln

# Legacy project does not pick up net8.0
# Inspect APIs/APIs.csproj — TargetFramework must remain netcoreapp3.1
Select-String -Path "APIs/APIs.csproj" -Pattern "TargetFramework"
```

### Spec requirements satisfied

- **REQ-04** (Scenarios 04-A, 04-B) — `src/Directory.Build.props` with nullable and implicit usings; no net8 bleed into legacy.
- **REQ-12** (Scenario 12-A) — legacy project TargetFramework unchanged.
- **REQ-01** (partial) — legacy solution `GastroGestion.sln` untouched.

---

## TASK-04 — Wire ProjectReference dependency graph [x]

**Work unit:** One commit — reference graph only, no new source code.  
**Conventional commit:** `build: wire Clean Architecture project reference graph`

### What to do

Add `<ProjectReference>` elements to each `.csproj` according to the allowed reference matrix:

| Project | Add references to |
|---------|-------------------|
| `GastroGestion.Domain` | (none) |
| `GastroGestion.Application` | `../GastroGestion.Domain/GastroGestion.Domain.csproj` |
| `GastroGestion.Infrastructure` | `../GastroGestion.Application/GastroGestion.Application.csproj` |
| `GastroGestion.Api` | `../GastroGestion.Application/GastroGestion.Application.csproj`, `../GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj`, `../GastroGestion.Contracts/GastroGestion.Contracts.csproj` |
| `GastroGestion.Contracts` | (none) |
| `GastroGestion.Domain.Tests` | `../../src/GastroGestion.Domain/GastroGestion.Domain.csproj` |
| `GastroGestion.Application.Tests` | `../../src/GastroGestion.Application/GastroGestion.Application.csproj` |

Use `dotnet add reference` commands or edit `.csproj` directly — either is acceptable, but the result must be verifiable.

### Forbidden edges — verify absence

- `GastroGestion.Domain` must have zero `<ProjectReference>` entries.
- `GastroGestion.Contracts` must have zero `<ProjectReference>` entries.
- `GastroGestion.Api` must NOT reference `GastroGestion.Domain` directly.
- `GastroGestion.Infrastructure` must NOT reference `GastroGestion.Api` or `GastroGestion.Contracts`.
- `GastroGestion.Application` must NOT reference `GastroGestion.Infrastructure` or `GastroGestion.Api`.

### Verification

```
# Domain has zero references (REQ-03 Scenario 03-A)
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" -Pattern "ProjectReference"
# Expected: no matches

# Contracts has zero references (REQ-03 Scenario 03-B)
Select-String -Path "src/GastroGestion.Contracts/GastroGestion.Contracts.csproj" -Pattern "ProjectReference"
# Expected: no matches

# Api does NOT reference Domain directly (REQ-03 Scenario 03-C)
Select-String -Path "src/GastroGestion.Api/GastroGestion.Api.csproj" -Pattern "Domain.csproj"
# Expected: no matches

# Infrastructure does NOT reference Api or Contracts (REQ-03 Scenario 03-D)
Select-String -Path "src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj" -Pattern "Api.csproj|Contracts.csproj"
# Expected: no matches

# All projects build individually after reference changes
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
dotnet build tests/GastroGestion.Application.Tests/GastroGestion.Application.Tests.csproj
```

### Spec requirements satisfied

- **REQ-03** (Scenarios 03-A, 03-B, 03-C, 03-D) — Clean Architecture dependency constraints enforced.

---

## TASK-05 — Add NuGet packages [x]

**Work unit:** One commit — package additions only.  
**Conventional commit:** `build: add scaffold NuGet packages (Serilog, Swashbuckle, xunit, FluentAssertions)`

### What to do

Add the following packages using `dotnet add package`:

**`GastroGestion.Api`:**
```
dotnet add src/GastroGestion.Api package Serilog.AspNetCore
dotnet add src/GastroGestion.Api package Swashbuckle.AspNetCore
```

**`GastroGestion.Domain.Tests`:**
```
dotnet add tests/GastroGestion.Domain.Tests package FluentAssertions
dotnet add tests/GastroGestion.Domain.Tests package coverlet.collector
```
(xunit, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio are included by the xunit template — verify they exist; add only if missing)

**`GastroGestion.Application.Tests`:**
```
dotnet add tests/GastroGestion.Application.Tests package FluentAssertions
dotnet add tests/GastroGestion.Application.Tests package coverlet.collector
```

### Must NOT add

- `Microsoft.EntityFrameworkCore.*` — Infrastructure phase
- `Microsoft.AspNetCore.Authentication.JwtBearer` — API+Security phase
- `Riok.Mapperly` or `FluentValidation` — Application phase
- Any Identity / BCrypt packages

### Verification

```
# Serilog.AspNetCore present in Api
Select-String -Path "src/GastroGestion.Api/GastroGestion.Api.csproj" -Pattern "Serilog.AspNetCore"

# Swashbuckle present in Api
Select-String -Path "src/GastroGestion.Api/GastroGestion.Api.csproj" -Pattern "Swashbuckle.AspNetCore"

# FluentAssertions present in test projects
Select-String -Path "tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj" -Pattern "FluentAssertions"

# Projects still build after package adds
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
dotnet test tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj
```

### Spec requirements satisfied

- Enables REQ-05 (packages required for composition root implementation in TASK-06).
- Satisfies the design's package choice table (section 3 of design.md).

---

## TASK-06 — API composition root: Serilog, Swagger, health-check, JWT config, DI extension methods [x]

**Work unit:** One commit — the only task with non-trivial C# code.  
**Conventional commit:** `feat(api): wire composition root — Serilog, Swagger, /health, JWT config placeholder`

### What to do

#### 6a — DI extension methods in Application and Infrastructure

Create `src/GastroGestion.Application/DependencyInjection.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;

namespace GastroGestion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Placeholder — use cases and ports registered here in later phases
        return services;
    }
}
```

Create `src/GastroGestion.Infrastructure/DependencyInjection.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;

namespace GastroGestion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Placeholder — EF Core, repositories, external services registered here in later phases
        return services;
    }
}
```

#### 6b — appsettings.json

Replace the template-generated `appsettings.json` in `src/GastroGestion.Api/`:
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" }
    ]
  },
  "Jwt": {
    "Issuer": "GastroGestion",
    "Audience": "GastroGestion",
    "SigningKey": ""
  },
  "AllowedHosts": "*"
}
```

Replace `appsettings.Development.json`:
```json
{
  "Serilog": {
    "MinimumLevel": "Debug"
  }
}
```

#### 6c — UserSecretsId

Add `<UserSecretsId>` to `GastroGestion.Api.csproj` (generate a new GUID):
```xml
<PropertyGroup>
  <UserSecretsId>xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx</UserSecretsId>
</PropertyGroup>
```

#### 6d — Program.cs composition root

Replace the template `Program.cs` entirely:
```csharp
using GastroGestion.Application;
using GastroGestion.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// --- Application and Infrastructure layers ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// --- Health checks ---
builder.Services.AddHealthChecks();

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Startup guard: JWT signing key must be configured ---
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    // Fail fast with a clear message — never silently proceed with a missing key
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. " +
        "Set it via user-secrets (dev) or the Jwt__SigningKey environment variable (CI/prod).");
}

var app = builder.Build();

// --- Middleware pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.MapHealthChecks("/health");

app.Run();
```

### Must NOT do

- Do not use `"Aguante River Plate"` or any hardcoded JWT secret string anywhere.
- Do not add `[Authorize]`, auth middleware, or token issuing code.
- Do not add controller endpoints beyond `/health`.
- Do not add EF Core `DbContext`.
- Do not add domain entities.

### Verification

```
# No hardcoded secret in source (REQ-06 Scenario 06-A)
Select-String -Path "src/" -Pattern "Aguante River Plate" -Recurse
# Expected: no matches

# UserSecretsId set (REQ-06 Scenario 06-C)
Select-String -Path "src/GastroGestion.Api/GastroGestion.Api.csproj" -Pattern "UserSecretsId"
# Expected: one match with a GUID value

# AddApplication extension method exists in Application project (REQ-05 Scenario 05-A)
Select-String -Path "src/GastroGestion.Application/" -Pattern "AddApplication" -Recurse

# AddInfrastructure extension method exists in Infrastructure project (REQ-05 Scenario 05-B)
Select-String -Path "src/GastroGestion.Infrastructure/" -Pattern "AddInfrastructure" -Recurse

# Project builds
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
```

Note: runtime scenarios (REQ-05-C, REQ-06-B, REQ-07, REQ-08, REQ-09) are validated in TASK-08.

### Spec requirements satisfied

- **REQ-05** (Scenarios 05-A, 05-B, 05-C) — DI composition root with AddApplication, AddInfrastructure extension methods.
- **REQ-06** (Scenarios 06-A, 06-B, 06-C) — No hardcoded secret; UserSecretsId present; startup fails if key absent.
- **REQ-07** (Scenario 07-A) — `/health` mapped.
- **REQ-08** (Scenarios 08-A, 08-B) — Swagger gated on Development.
- **REQ-09** (Scenario 09-A) — Serilog as logging provider with console sink.

---

## TASK-07 — Create src/GastroGestion.sln and enroll all 7 projects [x]

**Work unit:** One commit — solution file only.  
**Conventional commit:** `build: create src/GastroGestion.slnx and add all 7 projects`  
**Note:** .NET 10 SDK generates `.slnx` (XML format) instead of `.sln`. All `dotnet` commands work identically with `.slnx`.

### What to do

```bash
cd src
dotnet new sln -n GastroGestion
dotnet sln GastroGestion.sln add GastroGestion.Domain/GastroGestion.Domain.csproj
dotnet sln GastroGestion.sln add GastroGestion.Application/GastroGestion.Application.csproj
dotnet sln GastroGestion.sln add GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
dotnet sln GastroGestion.sln add GastroGestion.Api/GastroGestion.Api.csproj
dotnet sln GastroGestion.sln add GastroGestion.Contracts/GastroGestion.Contracts.csproj
dotnet sln GastroGestion.sln add ../tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj
dotnet sln GastroGestion.sln add ../tests/GastroGestion.Application.Tests/GastroGestion.Application.Tests.csproj
cd ..
```

Confirm legacy `GastroGestion.sln` at the repo root is untouched (checksum or `git diff`).

### Verification

```
# Solution file exists
ls src/GastroGestion.sln

# Legacy solution file not modified
git diff GastroGestion.sln
# Expected: no diff

# All 7 projects listed in new solution
dotnet sln src/GastroGestion.sln list
# Expected: 7 entries
```

### Spec requirements satisfied

- **REQ-01** (Scenario 01-A partial) — `src/GastroGestion.sln` exists; legacy `.sln` untouched.
- **REQ-02** (Scenarios 02-A, 02-B) — all 7 projects enrolled in the solution.

---

## TASK-08 — Build and runtime verification (no code changes) [x]

**Work unit:** Verification-only task — runs commands, reports results, no commits. If any check fails, the relevant upstream task must be fixed before this task can close.

### Verification commands

#### Build checks

```bash
# Full solution build (REQ-01 Scenario 01-A, REQ-02 Scenario 02-A)
dotnet build src/GastroGestion.sln
# Expected: exits with code 0, no errors

# Test discovery and run (REQ-02 Scenario 02-B)
dotnet test src/GastroGestion.sln
# Expected: exits with code 0; both test projects discovered; 0 tests failed

# Legacy solution still builds (REQ-01 Scenario 01-B, REQ-04 Scenario 04-B, REQ-12 Scenario 12-A)
# Requires .NET Framework 4.8 Developer Pack / MSBuild
msbuild GastroGestion.sln
# Expected: exits with code 0; no errors caused by this change
```

#### Repository hygiene

```bash
# No build artifacts tracked (REQ-10 Scenarios 10-A, 10-C)
dotnet build src/GastroGestion.sln
git status
# Expected: no entries under src/**/bin/ or src/**/obj/

git ls-files bin/ obj/ .vs/ packages/
# Expected: empty output

# No hardcoded JWT secret (REQ-06 Scenario 06-A)
Select-String -Path "src/" -Pattern "Aguante River Plate" -Recurse -CaseSensitive:$false
# Expected: no matches

# GastroGestionBlazor not in git status (REQ-11 Scenario 11-B)
git status | Select-String "GastroGestionBlazor"
# Expected: no matches
```

#### Runtime checks (requires dotnet run)

```bash
# Set a test JWT signing key in user-secrets before running
dotnet user-secrets set "Jwt:SigningKey" "test-key-for-local-verification-only-32-chars" --project src/GastroGestion.Api

# Start the API (REQ-05 Scenario 05-C, REQ-09 Scenario 09-A)
dotnet run --project src/GastroGestion.Api
# Expected: starts without exception; Serilog writes structured log with timestamp and level

# In a separate terminal:
# Health check (REQ-07 Scenario 07-A)
Invoke-RestMethod -Uri "http://localhost:{port}/health"
# Expected: status 200, body "Healthy"

# Swagger in Development (REQ-08 Scenario 08-A)
Invoke-WebRequest -Uri "http://localhost:{port}/swagger/index.html"
# Expected: status 200, HTML content

# Startup failure without key (REQ-06 Scenario 06-B)
# Remove user-secrets value and try to run — process must exit non-zero with message
dotnet user-secrets remove "Jwt:SigningKey" --project src/GastroGestion.Api
dotnet run --project src/GastroGestion.Api
# Expected: exits non-zero; log includes "Jwt:SigningKey is not configured"
```

### Spec requirements satisfied (all remaining runtime scenarios)

- **REQ-01** Scenario 01-A, 01-B
- **REQ-02** Scenarios 02-A, 02-B
- **REQ-05** Scenario 05-C
- **REQ-06** Scenarios 06-A, 06-B
- **REQ-07** Scenario 07-A
- **REQ-08** Scenarios 08-A, 08-B (Production test: restart with `ASPNETCORE_ENVIRONMENT=Production`, `/swagger/index.html` → 404)
- **REQ-09** Scenario 09-A

---

## Parallel vs. sequential summary

| Task | Can run in parallel with | Blocked by |
|------|--------------------------|------------|
| TASK-01 | — (must be first) | — |
| TASK-02 | — | TASK-01 |
| TASK-03 | Can start as soon as TASK-02 commits exist | TASK-02 |
| TASK-04 | Can follow immediately after TASK-03 | TASK-03 |
| TASK-05 | Can follow immediately after TASK-04 | TASK-04 |
| TASK-06 | — | TASK-05 |
| TASK-07 | Can be done alongside TASK-04/05/06 if one developer; otherwise after TASK-06 | TASK-02 (projects must exist) |
| TASK-08 | — (must be last) | TASK-06, TASK-07 |

TASK-07 can technically be done as soon as project files exist (TASK-02), but it is listed after TASK-06 to avoid partial solution states where enrolled projects do not yet build. A single developer should do it last before TASK-08.

---

## Spec coverage matrix

| REQ | Scenarios | Covered by task(s) |
|-----|-----------|---------------------|
| REQ-01 | 01-A, 01-B | TASK-07, TASK-08 |
| REQ-02 | 02-A, 02-B | TASK-02, TASK-07, TASK-08 |
| REQ-03 | 03-A, 03-B, 03-C, 03-D | TASK-04 |
| REQ-04 | 04-A, 04-B | TASK-03 |
| REQ-05 | 05-A, 05-B, 05-C | TASK-06 |
| REQ-06 | 06-A, 06-B, 06-C | TASK-06, TASK-08 |
| REQ-07 | 07-A | TASK-06, TASK-08 |
| REQ-08 | 08-A, 08-B | TASK-06, TASK-08 |
| REQ-09 | 09-A | TASK-06, TASK-08 |
| REQ-10 | 10-A, 10-B, 10-C | TASK-01 |
| REQ-11 | 11-A, 11-B | TASK-01 |
| REQ-12 | 12-A | TASK-03 |

---

## Review Workload Forecast

### Estimated changed lines per task

| Task | Type | Estimated additions | Estimated deletions | Notes |
|------|------|---------------------|---------------------|-------|
| TASK-01 | chore | ~50 (.gitignore) | 1,000–5,000+ | Almost entirely deletions of previously-tracked build artifacts (bin/, obj/, .vs/, packages/). This is generated output, NOT logic. Reviewers should treat it as a mechanical untracking commit and skim it — the only substantive review is the .gitignore content (~50 lines). |
| TASK-02 | feat | ~200–300 | ~20 (removing template stubs) | 7 `.csproj` files + 2 `appsettings*.json` stubs + directory structure. Mostly boilerplate. |
| TASK-03 | build | ~60 | 0 | 2 `Directory.Build.props` files + `.editorconfig`. Small, high-value diff. |
| TASK-04 | build | ~30 | 0 | ProjectReference additions only — short XML edits in 5 files. |
| TASK-05 | build | ~30 | 0 | PackageReference additions only — short XML edits in 3 files. |
| TASK-06 | feat | ~120 | ~30 (replacing template Program.cs) | `Program.cs` (~60 lines) + 2 `DependencyInjection.cs` (~20 lines each) + `appsettings.json` (~15 lines). The only commit with real C# logic. |
| TASK-07 | build | ~50 | 0 | `.sln` file only — GUID-heavy but reviewer reads it as project list. |
| TASK-08 | — | 0 | 0 | Verification-only; no commit. |

**Total estimated additions (excluding TASK-01 artifact deletions):** ~490–590 lines added  
**Total estimated deletions (excluding TASK-01 artifact deletions):** ~50 lines  
**TASK-01 artifact deletions:** 1,000–5,000+ lines of generated output

### 400-line budget analysis

| Metric | Value |
|--------|-------|
| Logic/scaffold additions (TASK-02 through TASK-07) | ~490–590 lines |
| Artifact deletions (TASK-01) | 1,000–5,000+ (generated; not logic) |
| **400-line budget risk (logic lines)** | **Medium** |
| **400-line budget risk (total diff including TASK-01)** | **High** (but see note below) |
| Chained PRs recommended | **Yes — see recommendation below** |
| Decision needed before apply | **Yes** |

### Chained PRs recommendation

The raw total diff (TASK-01 artifact deletions + scaffold additions) will far exceed 400 lines. However, the risk profile is asymmetric:

- **TASK-01** produces thousands of deleted lines of generated build output — it is not logic, not risky, and can be reviewed in under 5 minutes by any reviewer who understands `.gitignore` + `git rm --cached`. Lumping it with the scaffold would drown the substantive review.
- **TASK-02 through TASK-07** together are ~540 logic lines, crossing the 400-line threshold modestly.

**Recommended split: two chained PRs**

| PR | Tasks included | Estimated diff | Review focus |
|----|----------------|----------------|--------------|
| PR #1 — Repository hygiene | TASK-01 | ~50 added + thousands deleted (generated) | .gitignore completeness; GastroGestionBlazor/ entry; artifacts untracked; GastroGestionBlazor not deleted |
| PR #2 — .NET 8 scaffold | TASK-02, TASK-03, TASK-04, TASK-05, TASK-06, TASK-07 + TASK-08 verification | ~540 added / ~50 deleted | Project structure, reference graph, build props scoping, composition root wiring, /health, Serilog, Swagger, no hardcoded secrets |

PR #2 alone is ~540 lines of additions. If the team wants to stay under 400, it can be further split:

| PR #2a — Structural scaffold | TASK-02, TASK-03, TASK-04, TASK-05, TASK-07 | ~370 lines | Project files, props, references, packages, solution |
| PR #2b — API runtime wiring | TASK-06 + TASK-08 verification | ~150 lines | Program.cs, DI extensions, /health, Serilog, Swagger, JWT guard |

The delivery_strategy is `ask-on-risk`: **a chain-strategy decision is required from the user before `sdd-apply` begins.**

> Note on TASK-01 diff interpretation: the thousands of deleted lines from `git rm --cached` represent compiled DLLs, Visual Studio cache files, NuGet package folders, and build output that were accidentally committed. They are binary or generated artifacts — not project logic. Reviewers should verify only that the `.gitignore` is complete and that no needed source file was accidentally removed. This is a 5-minute review regardless of the line count reported by git diff.
