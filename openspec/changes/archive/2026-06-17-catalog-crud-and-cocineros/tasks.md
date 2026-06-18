# Tasks: catalog-crud-and-cocineros

> **Archive note (2026-06-17):** Tasks CCC-T13..T52 appear unchecked in this file
> (stale-checkbox artifact — `sdd-apply` did not update the tasks file for PRs B and C).
> All 52 tasks are confirmed complete by apply-progress and verify reports:
> verify-report-prA (engram #153), verify-report-prB (engram #155),
> verify-report-prC (engram #156). PRs #19, #20, #21 merged to main. 413 tests green.
> Stale checkboxes reconciled at archive per SKILL.md exceptional-repair clause.

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~300–380 total (PR A ~80, PR B ~160, PR C ~140) |
| 400-line budget risk | Low (each PR individually) / Medium (if merged as one) |
| Chained PRs recommended | Yes |
| Suggested split | PR A (cocineros) → PR B (cliente CRUD) → PR C (ingrediente CRUD) |
| Delivery strategy | stacked-to-main |
| Chain strategy | stacked-to-main |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| A | Cocineros GET endpoint, no domain change | PR A | base = main; independently buildable+green |
| B | Cliente edit + soft-delete + search | PR B | base = PR A branch (stacked); independently buildable+green |
| C | Ingrediente edit + soft-delete + search | PR C | base = PR B branch (stacked); independently buildable+green |

---

## PR A — Cocineros (CCC-A01) — MERGED #19 @ 8251125

### Phase A1: Infrastructure

- [x] **CCC-T01** [A · Infra · Port] `src/GastroGestion.Application/Abstractions/Persistence/IUsuarioRepository.cs` — add `Task<IReadOnlyList<Usuario>> GetByRolAsync(RolUsuario rol, CancellationToken ct = default);`
- [x] **CCC-T02** [A · Infra · EF] `src/GastroGestion.Infrastructure/Persistence/Repositories/UsuarioRepository.cs` — implement `GetByRolAsync`

### Phase A2: Application

- [x] **CCC-T03** [A · App] Create `GetCocinerosQuery.cs`
- [x] **CCC-T04** [A · App] Create `GetCocinerosHandler.cs`
- [x] **CCC-T05** [A · App · DI] `DependencyInjection.cs` — `AddScoped<GetCocinerosHandler>()`

### Phase A3: Contracts

- [x] **CCC-T06** [A · Contracts] Create `UsuarioResponses.cs` — `sealed record CocineroResponse(Guid Id, string NombreCompleto)`
- [x] **CCC-T07** [A · Contracts] Create `UsuarioMappings.cs` — `ToCocineroResponse(this Usuario u)`

### Phase A4: API

- [x] **CCC-T08** [A · API] Create `UsuarioEndpoints.cs` — `MapGet("/cocineros", ...)` with role gate
- [x] **CCC-T09** [A · API · Program] `Program.cs` — add `app.MapUsuarioEndpoints();`

### Phase A5: Tests

- [x] **CCC-T10** [A · Test · App] `GetCocinerosHandlerTests.cs`
- [x] **CCC-T11** [A · Test · Infra] `UsuarioGetByRolTests.cs`
- [x] **CCC-T12** [A · Test · API] `UsuarioEndpointTests.cs`

---

## PR B — Cliente CRUD (CCC-B01, CCC-B02, CCC-B03) — MERGED #20 @ 60bd611

### Phase B1: Domain

- [x] **CCC-T13** [B · Domain] `Cliente.cs` — add `ActualizarDatos(...)`

### Phase B2: Infrastructure

- [x] **CCC-T14** [B · Infra · Port] `IClienteRepository.cs` — add `SearchAsync` and `CuitExistsForOtherAsync`
- [x] **CCC-T15** [B · Infra · EF] `ClienteRepository.cs` — implement both methods

### Phase B3: Application

- [x] **CCC-T16** [B · App] Create `EditarClienteCommand.cs`
- [x] **CCC-T17** [B · App] Create `EditarClienteHandler.cs`
- [x] **CCC-T18** [B · App] Create `DesactivarClienteCommand.cs` + `DesactivarClienteHandler.cs`
- [x] **CCC-T19** [B · App] Create `BuscarClientesQuery.cs` + `BuscarClientesHandler.cs`
- [x] **CCC-T20** [B · App · DI] `DependencyInjection.cs` — `AddScoped` for 3 handlers

### Phase B4: Contracts

- [x] **CCC-T21** [B · Contracts] Add `EditarClienteRequest`
- [x] **CCC-T22** [B · Contracts] Add `EditarClienteValidator`
- [x] **CCC-T23** [B · Contracts] Add `ToCommand(this EditarClienteRequest r, Guid id)` mapping

### Phase B5: API

- [x] **CCC-T24** [B · API] `ClienteEndpoints.cs` — add `MapPut("/{id:guid}", ...)`
- [x] **CCC-T25** [B · API] `ClienteEndpoints.cs` — add `MapDelete("/{id:guid}", ...)`
- [x] **CCC-T26** [B · API] `ClienteEndpoints.cs` — rewire `MapGet("/", ...)` to `BuscarClientesHandler`

### Phase B6: Tests

- [x] **CCC-T27** [B · Test · Domain] `ClienteTests.cs` — extend with `ActualizarDatos` scenarios
- [x] **CCC-T28** [B · Test · App] `EditarClienteHandlerTests.cs`
- [x] **CCC-T29** [B · Test · App] `DesactivarClienteHandlerTests.cs`
- [x] **CCC-T30** [B · Test · App] `BuscarClientesHandlerTests.cs`
- [x] **CCC-T31** [B · Test · Infra] `ClienteSearchTests.cs`
- [x] **CCC-T32** [B · Test · API] `ClienteCrudEndpointTests.cs`

---

## PR C — Ingrediente CRUD (CCC-C01, CCC-C02, CCC-C03) — MERGED #21 @ b3af61e

### Phase C1: Domain

- [x] **CCC-T33** [C · Domain] `Ingrediente.cs` — add `ActualizarNombre(...)`

### Phase C2: Infrastructure

- [x] **CCC-T34** [C · Infra · Port] `IIngredienteRepository.cs` — add `SearchAsync` and `NombreExistsForOtherAsync`
- [x] **CCC-T35** [C · Infra · EF] `IngredienteRepository.cs` — implement both methods

### Phase C3: Application

- [x] **CCC-T36** [C · App] Create `EditarIngredienteCommand.cs`
- [x] **CCC-T37** [C · App] Create `EditarIngredienteHandler.cs`
- [x] **CCC-T38** [C · App] Create `DesactivarIngredienteCommand.cs` + `DesactivarIngredienteHandler.cs`
- [x] **CCC-T39** [C · App] Create `BuscarIngredientesQuery.cs` + `BuscarIngredientesHandler.cs`
- [x] **CCC-T40** [C · App · DI] `DependencyInjection.cs` — `AddScoped` for 3 handlers

### Phase C4: Contracts

- [x] **CCC-T41** [C · Contracts] Add `EditarIngredienteRequest(string Nombre)` — UnidadBase intentionally absent
- [x] **CCC-T42** [C · Contracts] Add `EditarIngredienteValidator`
- [x] **CCC-T43** [C · Contracts] Add `ToCommand(this EditarIngredienteRequest r, Guid id)` mapping

### Phase C5: API

- [x] **CCC-T44** [C · API] `IngredienteEndpoints.cs` — add `MapPut("/{id:guid}", ...)`
- [x] **CCC-T45** [C · API] `IngredienteEndpoints.cs` — add `MapDelete("/{id:guid}", ...)`
- [x] **CCC-T46** [C · API] `IngredienteEndpoints.cs` — rewire `MapGet("/", ...)` to `BuscarIngredientesHandler`

### Phase C6: Tests

- [x] **CCC-T47** [C · Test · Domain] `IngredienteTests.cs`
- [x] **CCC-T48** [C · Test · App] `EditarIngredienteHandlerTests.cs`
- [x] **CCC-T49** [C · Test · App] `DesactivarIngredienteHandlerTests.cs`
- [x] **CCC-T50** [C · Test · App] `BuscarIngredientesHandlerTests.cs`
- [x] **CCC-T51** [C · Test · Infra] `IngredienteSearchTests.cs`
- [x] **CCC-T52** [C · Test · API] `IngredienteCrudEndpointTests.cs`

---

## Build & Test Commands

```
dotnet build src\GastroGestion.sln
dotnet test tests\GastroGestion.Domain.Tests\GastroGestion.Domain.Tests.csproj
dotnet test tests\GastroGestion.Application.Tests\GastroGestion.Application.Tests.csproj
dotnet test tests\GastroGestion.Infrastructure.Tests\GastroGestion.Infrastructure.Tests.csproj
dotnet test tests\GastroGestion.Api.Tests\GastroGestion.Api.Tests.csproj
```

**Final test results (post-merge):** 413 / 413 passing (0 failures).
