# Delta Spec — net8-clean-architecture-foundation

**Scope:** Foundation scaffold only. Defines what MUST be true once this change is applied. Does not describe how to implement anything.

---

## Non-goals (explicitly out of scope)

- No domain entities (Cliente, Plato, Pedido, Factura, Stock, etc.)
- No EF Core `DbContext`, entity configurations, or migrations
- No business logic, billing/IVA rules, or state machines
- No auth implementation (no login, no token issuing, no `[Authorize]` policies, no password hashing)
- No real API endpoints beyond `/health`
- No changes to the Blazor WASM frontend (separate repo — `GastroGestionBlazor` — out of scope entirely)
- No design or implementation of the cross-repo contract-sharing strategy between backend and frontend
- No changes to legacy net48 projects

---

## REQ-01 — New solution exists and is scoped under `src/`

**What must be true:**

- A file `src/GastroGestion.sln` exists and is valid.
- The legacy solution `GastroGestion.sln` at the repository root is untouched (same content as before this change).
- No `Directory.Build.props` or `.editorconfig` is placed at the repository root by this change.

### Scenario 01-A — New solution builds with .NET 8 SDK

```
Given  the .NET 8 SDK is installed
When   `dotnet build src/GastroGestion.sln` is executed from the repository root
Then   the command exits with code 0
And    no build errors are reported
And    build output lands under src/**/bin/ (never under the repo root bin/)
```

### Scenario 01-B — Legacy solution still builds

```
Given  the .NET Framework 4.8 Developer Pack and Visual Studio (or MSBuild) are installed
When   `msbuild GastroGestion.sln` is executed from the repository root
Then   the command exits with code 0
And    no projects in the legacy solution report errors caused by this change
```

---

## REQ-02 — Seven projects exist with correct names and TFMs

**What must be true:**

| Project name | Type | Target framework | Location |
|---|---|---|---|
| `GastroGestion.Domain` | Class library | `net8.0` | `src/GastroGestion.Domain/` |
| `GastroGestion.Application` | Class library | `net8.0` | `src/GastroGestion.Application/` |
| `GastroGestion.Infrastructure` | Class library | `net8.0` | `src/GastroGestion.Infrastructure/` |
| `GastroGestion.Api` | ASP.NET Core Web API | `net8.0` | `src/GastroGestion.Api/` |
| `GastroGestion.Contracts` | Class library | `net8.0` | `src/GastroGestion.Contracts/` |
| `GastroGestion.Domain.Tests` | xUnit test project | `net8.0` | `tests/GastroGestion.Domain.Tests/` |
| `GastroGestion.Application.Tests` | xUnit test project | `net8.0` | `tests/GastroGestion.Application.Tests/` |

- All seven `.csproj` files are referenced in `src/GastroGestion.sln`.
- Each project compiles without errors or warnings that are treated as errors.

### Scenario 02-A — All seven projects compile individually

```
Given  the .NET 8 SDK is installed
When   `dotnet build <project>.csproj` is run for each of the seven projects
Then   each command exits with code 0
```

### Scenario 02-B — Test projects run (placeholder, no tests yet)

```
Given  the .NET 8 SDK is installed
When   `dotnet test src/GastroGestion.sln` is executed
Then   the command exits with code 0
And    both test projects are discovered
And    zero tests are reported as failed (zero tests passing is acceptable at this stage)
```

Note: `GastroGestion.Contracts` replaces the former `GastroGestion.Shared` and holds API request/response DTOs. `GastroGestion.Web` (Blazor WASM) is NOT part of this solution — it lives in the separate `GastroGestionBlazor` repository.

---

## REQ-03 — Clean Architecture dependency constraints enforced

**What must be true (allowable project references only):**

| Project | May reference | Must NOT reference |
|---|---|---|
| `GastroGestion.Domain` | _(nothing)_ | Application, Infrastructure, Api, Contracts |
| `GastroGestion.Application` | Domain | Infrastructure, Api, Contracts |
| `GastroGestion.Infrastructure` | Application, Domain | Api, Contracts |
| `GastroGestion.Api` | Application, Infrastructure, Contracts | Domain (direct) |
| `GastroGestion.Contracts` | _(nothing)_ | Domain, Application, Infrastructure, Api |
| `GastroGestion.Domain.Tests` | Domain | Infrastructure, Api, Contracts |
| `GastroGestion.Application.Tests` | Application, Domain | Infrastructure, Api, Contracts |

Note: Api may only reach Domain transitively (through Application/Infrastructure), never with a direct `<ProjectReference>` to Domain. `GastroGestion.Contracts` is dependency-light (no Domain reference) so it can serve as the future OpenAPI contract source without coupling domain types to the HTTP boundary.

### Scenario 03-A — Domain has zero project references

```
Given  `GastroGestion.Domain/GastroGestion.Domain.csproj` is inspected
When   all `<ProjectReference>` elements are counted
Then   the count is 0
```

### Scenario 03-B — Contracts has zero project references

```
Given  `GastroGestion.Contracts/GastroGestion.Contracts.csproj` is inspected
When   all `<ProjectReference>` elements are counted
Then   the count is 0
```

### Scenario 03-C — Api does not directly reference Domain

```
Given  `GastroGestion.Api/GastroGestion.Api.csproj` is inspected
When   all `<ProjectReference Include="...">` paths are read
Then   none of them point to `GastroGestion.Domain.csproj`
```

### Scenario 03-D — Infrastructure does not reference Api or Contracts

```
Given  `GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj` is inspected
When   all `<ProjectReference>` paths are read
Then   none of them point to `GastroGestion.Api.csproj` or `GastroGestion.Contracts.csproj`
```

---

## REQ-04 — Shared compiler settings via `Directory.Build.props` scoped under `src/`

**What must be true:**

- A file `src/Directory.Build.props` exists.
- It sets at minimum: `<TargetFramework>net8.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- No `Directory.Build.props` is placed at the repository root by this change.
- The `tests/` projects inherit it (they live under the same filesystem tree scope) OR define their own equivalent settings.

### Scenario 04-A — Nullable and implicit usings are active in Domain

```
Given  `GastroGestion.Domain` is built
When   a source file with a nullable reference type warning is introduced (e.g. `string? x = null; Console.Write(x.Length);`)
Then   the compiler emits CS8602 (dereference of possibly null reference), confirming nullable analysis is enabled
```

### Scenario 04-B — Legacy projects do NOT inherit net8 settings

```
Given  the legacy `GastroGestion.sln` is built with MSBuild targeting net48
When   the build is inspected for TargetFramework
Then   no legacy .csproj reports `net8.0` as its resolved TargetFramework
```

---

## REQ-05 — DI composition root wired in Api

**What must be true:**

- `GastroGestion.Api` has a composition root (`Program.cs` or equivalent) that registers at minimum:
  - A placeholder service registration from Application layer (e.g. `AddApplication()`).
  - A placeholder service registration from Infrastructure layer (e.g. `AddInfrastructure()`).
  - The health-check service (`services.AddHealthChecks()`).
  - Swagger/OpenAPI services (`services.AddEndpointsApiExplorer()`, `services.AddSwaggerGen()`).
  - Serilog as the logging provider.
- Both `AddApplication()` and `AddInfrastructure()` are extension methods defined in their respective projects (Application and Infrastructure), not inline in Api.

### Scenario 05-A — Application registers via extension method

```
Given  `GastroGestion.Application` is inspected
When   its source files are scanned for a static extension method on `IServiceCollection`
Then   at least one method named `AddApplication` is found in the Application project
```

### Scenario 05-B — Infrastructure registers via extension method

```
Given  `GastroGestion.Infrastructure` is inspected
When   its source files are scanned for a static extension method on `IServiceCollection`
Then   at least one method named `AddInfrastructure` is found in the Infrastructure project
```

### Scenario 05-C — Api starts without exceptions

```
Given  a default development `appsettings.Development.json` or user-secrets provide a JWT signing key
When   `dotnet run --project src/GastroGestion.Api` is executed
Then   the process starts without throwing an unhandled exception
And    the Kestrel server begins listening on at least one port
```

---

## REQ-06 — JWT signing key is read from configuration; never hardcoded

**What must be true:**

- The string `"Aguante River Plate"` (or any hardcoded JWT secret) does NOT appear anywhere in the new solution source files.
- A configuration key (e.g. `Jwt:SigningKey`) is read from the configuration system.
- `user-secrets` is the intended non-production source; the project must have `UserSecretsId` set.
- The application fails to start with a clear error if the key is absent (no silent default).

### Scenario 06-A — No hardcoded secret in source

```
Given  all .cs files under `src/` are scanned
When   a full-text search is performed for "Aguante River Plate" (case-insensitive)
Then   zero matches are found
```

### Scenario 06-B — Missing signing key causes startup failure

```
Given  no `Jwt:SigningKey` value is present in any configuration source (appsettings, env vars, user-secrets)
When   `dotnet run --project src/GastroGestion.Api` is executed
Then   the process exits with a non-zero code
And    the log or stderr output includes a message indicating the JWT signing key is missing or not configured
```

### Scenario 06-C — UserSecretsId is set in Api project

```
Given  `GastroGestion.Api/GastroGestion.Api.csproj` is inspected
When   the file is parsed for `<UserSecretsId>`
Then   a non-empty GUID value is present
```

---

## REQ-07 — Health-check endpoint returns 200

**What must be true:**

- A `GET /health` endpoint is registered and returns HTTP 200 with status `Healthy` when no checks fail.
- The endpoint is reachable without authentication.

### Scenario 07-A — GET /health returns 200

```
Given  the Api is running locally (dotnet run)
When   an HTTP GET request is sent to `http://localhost:{port}/health`
Then   the response status code is 200
And    the response body contains "Healthy"
```

---

## REQ-08 — Swagger UI is available in Development environment

**What must be true:**

- In Development environment, the Swagger UI middleware is enabled.
- `GET /swagger/index.html` returns HTTP 200 in Development.
- Swagger is NOT enabled in Production (conditional on environment).

### Scenario 08-A — Swagger UI accessible in Development

```
Given  the Api is running with `ASPNETCORE_ENVIRONMENT=Development`
When   an HTTP GET request is sent to `http://localhost:{port}/swagger/index.html`
Then   the response status code is 200
And    the response body contains HTML content (not a 404 or redirect)
```

### Scenario 08-B — Swagger not exposed in Production

```
Given  the Api is running with `ASPNETCORE_ENVIRONMENT=Production`
When   an HTTP GET request is sent to `http://localhost:{port}/swagger/index.html`
Then   the response status code is 404
```

---

## REQ-09 — Serilog is the logging provider

**What must be true:**

- Serilog is configured as the sole `ILogger` provider via `UseSerilog()` or `AddSerilog()`.
- At minimum, a console sink is registered.
- The default ASP.NET Core `ILoggerFactory` is NOT used alongside Serilog without clearing default providers.

### Scenario 09-A — Structured log output on startup

```
Given  the Api is running with Serilog configured
When   the application starts
Then   at least one structured log entry is written to the console
And    the log entry includes a timestamp and a message level (e.g. "Information")
```

---

## REQ-10 — Repository hygiene: build artifacts are not tracked

**What must be true:**

- A `.gitignore` file exists at the repository root.
- It contains patterns that exclude at minimum: `bin/`, `obj/`, `.vs/`, `packages/`, `*.user`, `*.suo`, `*.userosscache`, `*.sln.docstates`.
- After this change is merged, `bin/`, `obj/`, and `.vs/` directories are NOT tracked by git (i.e. were removed from the index via `git rm --cached` or were never staged).

### Scenario 10-A — dotnet build artifacts are gitignored

```
Given  the `.gitignore` at the repository root is in effect
When   `dotnet build src/GastroGestion.sln` is executed
Then   `git status` shows no new untracked or modified entries under `src/**/bin/` or `src/**/obj/`
```

### Scenario 10-B — .gitignore contains required patterns

```
Given  the `.gitignore` file at the repository root is read
When   its content is scanned for exclusion patterns
Then   patterns for `bin/`, `obj/`, `.vs/`, and `packages/` are present
```

### Scenario 10-C — Previously tracked artifacts removed from index

```
Given  the repository before this change had `bin/`, `obj/`, `.vs/` tracked
When   this change is applied and `git ls-files` is queried for those paths
Then   no files under `bin/`, `obj/`, or `.vs/` appear in the tracked file list
```

---

## REQ-11 — Frontend repo excluded from backend tracking

**Context:** `GastroGestionBlazor/` is a separate git repository nested inside the backend repo's working directory. It contains a live Blazor WASM frontend (auth wired, Clientes + Ingredientes pages, HTTP services). It must NOT be deleted, and it must NOT be tracked by the backend repo.

**What must be true:**

- The `.gitignore` at the repository root contains an entry for `GastroGestionBlazor/` so the backend repo never tracks the nested frontend repo.
- `git status` in the backend repo shows no untracked entries under `GastroGestionBlazor/`.
- `GastroGestion.Web` does NOT appear in `src/GastroGestion.sln` — the Blazor frontend is entirely out of scope for the backend solution.

### Scenario 11-A — GastroGestionBlazor/ is gitignored in backend repo

```
Given  the `.gitignore` at the repository root is read
When   its content is scanned for exclusion entries
Then   an entry matching `GastroGestionBlazor/` (or equivalent glob) is present
```

### Scenario 11-B — Backend git status shows no frontend entries

```
Given  the backend repo has the updated `.gitignore` in effect
When   `git status` is run at the repository root
Then   no entries under `GastroGestionBlazor/` appear as untracked or modified
```

---

## REQ-12 — `src/Directory.Build.props` does not affect legacy projects

**What must be true:**

- `src/Directory.Build.props` sets `<TargetFramework>net8.0</TargetFramework>` only for projects that are physically under `src/`.
- Legacy projects (`APIs/`, `BLL/`, `DLL/`, etc.) at the repository root do not inherit this props file.

### Scenario 12-A — Legacy project TargetFramework unchanged

```
Given  the legacy `APIs/APIs.csproj` is inspected after this change
When   its resolved `<TargetFramework>` is read
Then   it is `netcoreapp3.1` (or its existing value), NOT `net8.0`
```

Note: The legacy API targets `netcoreapp3.1` per the current `.csproj`. This scenario confirms that `src/Directory.Build.props` does not override it.

---

## Dependency map (spec cross-reference)

```
REQ-01 (solution exists)
  └── REQ-02 (7 projects)
        ├── REQ-03 (dependency rules)
        ├── REQ-04 (build props)
        └── REQ-05 (DI root)
              ├── REQ-06 (JWT key from config)
              ├── REQ-07 (health endpoint)
              ├── REQ-08 (Swagger)
              └── REQ-09 (Serilog)
REQ-10 (gitignore) — independent
REQ-11 (frontend repo gitignored) — independent
REQ-12 (props scoping) — independent
```
