# Expose the GastroGestion domain over a Minimal-API Web API (Application use cases + dev seeder)

Give the persisted Phase-3 domain a usable HTTP surface: an ASP.NET Core **Minimal API** host (RouteGroupBuilder per aggregate, `TypedResults`), the **Application use cases** that are still missing (only `CrearFactura` exists today), RFC 7807 error translation via `IExceptionHandler` + `AddProblemDetails()`, FluentValidation on the way in, DTOs in the empty `GastroGestion.Contracts` project with manual static mapping, a wired JWT authentication pipeline (all endpoints `[AllowAnonymous]` for now), and a runtime **DevDataSeeder** that builds browsable sample data through the real domain factories. This phase also closes the one infrastructure follow-up the previous phase flagged as a Phase-4 blocker: **W-01** (the `IEfectivoPrecioService` sync-over-async deadlock risk).

## Why now

This is **phase 4 of 7** in the strangler roadmap (scaffold ✅ → domain port ✅ → infrastructure/EF Core ✅ → **application + Web API + dev seeder** → security/login → stock/OT hardening → Blazor). The domain (8 aggregate roots, full invariants, 147 tests) and the persistence skin (EF Core 8 on LocalDB, repositories, UoW, post-commit dispatcher, 175 tests) are complete and on `main`. But nothing is reachable: `Program.cs` registers no endpoints, the `Contracts` project is empty (zero `.cs` files), the JWT pipeline is configured but never wired (`AddAuthentication`/`UseAuthentication`/`UseAuthorization` are all absent), and the only Application use case is `CrearFacturaHandler`. There is no way to create a `Cliente`, take a `Pedido`, register a `Pago`, or even browse seeded data. Until the domain is exposed over HTTP, no UI (Phase 7) and no real auth story (Phase 5) can be built on top of it. Full analysis: engram `sdd/web-api/explore` (obs #60).

This phase is also the agreed point to fix **W-01**: `EfectivoPrecioService` blocks on async repositories via `.GetAwaiter().GetResult()`. It is harmless today (wired but never injected into a live call path), but the moment a `ConfirmarPrecio` endpoint is exposed on a saturated ASP.NET Core thread pool it becomes a **deadlock**. It MUST be fixed before any price-confirmation endpoint goes live.

## What success looks like

- The API host serves a documented surface: catalogue, transactional, fiscal, and stock endpoints, organised as one `RouteGroupBuilder` per aggregate with `TypedResults`.
- Every endpoint validates its request with FluentValidation and translates failures, domain errors, conflicts, and not-found into RFC 7807 ProblemDetails (`400` validation, `409` conflict, `422` domain, `404` not-found, `500` unhandled).
- Domain aggregates are **never** serialised over the wire; only `GastroGestion.Contracts` DTOs cross the boundary, mapped by hand-written static extensions (no AutoMapper).
- W-01 is gone: `IEfectivoPrecioService` is async end-to-end, the implementation is genuinely async (no `.GetAwaiter().GetResult()`), and the `ConfirmarPrecio` endpoint is safe under load.
- The JWT authentication pipeline is wired (`AddAuthentication(JwtBearer)` + `UseAuthentication` + `UseAuthorization`), with **all Phase-4 endpoints `[AllowAnonymous]`** — establishing the pipeline now so Phase 5 only removes the attribute and adds login.
- A developer running the API in `Development` lands on a database pre-populated with realistic, invariant-respecting sample data (built through domain factories, fired through repositories, idempotent), and every seeded aggregate is browsable via GET-all endpoints.
- `dotnet test tests/GastroGestion.Api.Tests/` exercises the real HTTP stack via `WebApplicationFactory<Program>` against LocalDB.

## Scope

### In scope

| Area | What lands |
|------|-----------|
| API host | Minimal API on the existing `GastroGestion.Api` project; one `RouteGroupBuilder` per aggregate; `TypedResults`; no MVC controllers; **no mediator** (plain handler injection, the `CrearFacturaHandler` precedent) |
| Error handling | `GastroGestionExceptionHandler : IExceptionHandler` + `AddProblemDetails()`; `ConflictException`→409, `DomainException`→422, explicit not-found→404, unhandled→500 (RFC 7807) |
| Validation | FluentValidation via a reusable endpoint filter; failures return `ValidationProblem` (400) |
| DTOs + mapping | Request/response DTOs in `GastroGestion.Contracts` (currently empty) + manual static mapping extensions; **no AutoMapper**; domain aggregates never exposed over the wire |
| OpenAPI | Keep Swashbuckle 6.6.2; **remove** the redundant `Microsoft.AspNetCore.OpenApi` package; Swagger dev-only |
| W-01 fix | Make `IEfectivoPrecioService` async (`Task<(Dinero, PorcentajeIVA)> ...Async`; `Task` is BCL so Domain stays zero-dependency); rewrite `EfectivoPrecioService` genuinely async; update call sites — **in PR 1, before any price endpoint is exposed** |
| List queries | Add `GetAllAsync` to the repository ports + EF implementations; expose GET-all endpoints so seeded data is browsable |
| Auth pipeline | `AddAuthentication(JwtBearer)` + `UseAuthentication` + `UseAuthorization`; **all Phase-4 endpoints `[AllowAnonymous]`** |
| Pedido role gate | `RolUsuario` taken from the request body for Pedido state-transition endpoints (no login yet) — a temporary hole closed in Phase 5 |
| Application use cases | All missing catalogue/transactional/fiscal/stock handlers (see capabilities + use-case catalogue below) |
| Dev seeder | `DevDataSeeder` in `Infrastructure.Persistence` via domain factories + repositories; gated on `IsDevelopment()`; idempotent (skip if `Clientes.AnyAsync()`); called from `Program.cs` after auto-migrate |
| Integration tests | `tests/GastroGestion.Api.Tests/` with `WebApplicationFactory<Program>` + LocalDB, `[Trait("Category","Integration")]` |

### Out of scope (non-goals)

- **Cloud deployment.** The host stays config-driven and local (LocalDB); no Azure/cloud provisioning, no container/CI deployment.
- **`Usuario`/login aggregate and real authentication.** No `Usuario` domain aggregate, no login endpoint, no token issuance. The JWT pipeline is wired but every endpoint is `[AllowAnonymous]`. **Phase 5** adds the `Usuario` aggregate + login endpoint, removes `[AllowAnonymous]`, and extracts `RolUsuario` from JWT claims.
- **Real AFIP/ARCA call.** The `FacturaNecesitaCAE` seam stays a domain event; no HTTP call to AFIP. CAE assignment remains a stored value, no external integration.
- **Mediator (MediatR).** Plain handler injection only, matching the established `CrearFacturaHandler` pattern. A mediator is a Phase 5+ decision if complexity grows.
- **Result pattern.** Handlers keep throwing domain/conflict exceptions; the `IExceptionHandler` translates them. No handler-signature rewrite to `Result<T>`.
- **Pagination / filtering / sorting on GET-all.** List endpoints return the full set (small, dev-oriented). Paged read models are a later concern.
- **Gap-free sequence generation, near-expiry ranking, combo/sub-recipe support** — all remain deferred per the domain/infrastructure non-goals.
- **Legacy `APIs/` MVC + AutoMapper architecture** — not ported; used only as domain-vocabulary reference for surface design.

## Capabilities

> Contract with the spec phase. Mapped to PRs; the spec phase decomposes into Given/When/Then.

### New capabilities

- `api-foundation` (PR 1): Minimal-API host wiring, JWT auth pipeline (`[AllowAnonymous]`), `IExceptionHandler` + ProblemDetails, FluentValidation endpoint-filter infrastructure, OpenAPI cleanup, `DevDataSeeder`, the W-01 async fix, and the `Api.Tests` project with a smoke test.
- `api-catalogue` (PR 2): catalogue use cases + endpoints (`Cliente`, `Ingrediente`, `Plato`, `Menu`, `Mesa`), the `GetAllAsync` repository additions, GET-all list endpoints, catalogue DTOs + validators.
- `api-transactional-fiscal-stock` (PR 3): `Pedido` lifecycle endpoints (create, add line, confirm price — exercises the W-01 fix, transition state from body role), the existing `CrearFactura` endpoint wiring plus `RegistrarPago`/`GetFactura`, and `RegistrarMovimientoStock`/`GetBalanceStock`.

### Modified capabilities

- `Domain` (`IEfectivoPrecioService`): the domain service interface becomes async — `Task<(Dinero, PorcentajeIVA)> ...Async(...)`. **No framework dependency added** (`Task` is BCL); Domain keeps zero `PackageReference`/`ProjectReference`. This is the W-01 Option-A fix.
- `Application` (`EfectivoPrecioService`): rewritten genuinely async (awaits `IPlatoRepository.GetByIdAsync` and `IMenuRepository.GetActivosByFechaAsync` directly; removes `.GetAwaiter().GetResult()`); call sites updated.
- `Application` (repository ports): each port gains `GetAllAsync` to back the list endpoints.
- `Infrastructure` (repository impls): each repository implements `GetAllAsync` with the full owned-entity graph.

## Use-case catalogue (to be WRITTEN — only `CrearFactura` exists)

> Names are indicative; the **spec/design phases reconcile exact CLR names** against `src/GastroGestion.Domain` and `src/GastroGestion.Application` (e.g. the actual factory signatures, `RolUsuario` vs `Rol`, `ResolverPrecioEfectivoAsync` naming).

**Catalogue:**
- `CrearCliente` — `Cliente.Crear(...)` + `IClienteRepository.AddAsync`
- `GetClienteById` — `IClienteRepository.GetByIdAsync`
- `GetAllClientes` — `IClienteRepository.GetAllAsync` (new port method)
- `CrearIngrediente`, `CrearPlato` (with recipe lines), `CrearMenu` (with items + override), `CrearMesa`
- Per-aggregate list queries for the aggregates exposed via GET-all

**Transactional:**
- `CrearPedido` — `Pedido.Crear(...)` (+ `Mesa.AsignarPedido` when `Salon`)
- `AgregarLinea` — `pedido.AgregarLinea(...)`
- `ConfirmarPrecioLinea` — resolves the now-async `IEfectivoPrecioService` (exercises the W-01 fix on a live HTTP path)
- `TransicionarEstadoPedido` — `pedido.TransicionarEstado(estado, rolUsuario)`; **`RolUsuario` from the request body** (no login yet)
- `GetPedidoById`

**Fiscal / Stock:**
- `CrearFactura` — handler EXISTS; only the endpoint + DTOs are new
- `RegistrarPago` — `factura.RegistrarPago(...)`
- `GetFacturaById`
- `RegistrarMovimientoStock` — `MovimientoStock` factory + append-only repo `AddAsync`
- `GetBalanceStock` — `IMovimientoStockRepository.CalcularBalanceAsync`

## Approach

The host owns HTTP concerns only; orchestration stays in Application handlers; the domain is never exposed.

- **Minimal APIs, one group per aggregate.** A `RouteGroupBuilder` per aggregate (`/clientes`, `/ingredientes`, `/platos`, `/menus`, `/mesas`, `/pedidos`, `/facturas`, `/stock`) registered from small static `Map*Endpoints` extension methods called in `Program.cs`. Handlers are injected directly into endpoint delegates (no mediator) — the `CrearFacturaHandler` precedent. Return values use `TypedResults` (`Created`, `Ok`, `NoContent`, `NotFound`) for explicit, OpenAPI-accurate signatures.
- **DTOs in `Contracts`, manual mapping.** Request DTOs map to commands; response DTOs are flat read models. Static mapping extension methods live in `GastroGestion.Contracts`. No AutoMapper. The domain aggregate types never appear in a request or response.
- **Validation as an endpoint filter.** `AbstractValidator<T>` validators registered via `AddValidatorsFromAssemblyContaining<T>()`; a reusable `WithValidation<T>()` endpoint filter runs the validator and short-circuits to `ValidationProblem` (400) on failure, before the handler executes.
- **One exception handler, RFC 7807.** `GastroGestionExceptionHandler : IExceptionHandler` + `AddProblemDetails()` maps `ConflictException`→409, `DomainException`→422, explicit not-found (null repo return / `KeyNotFoundException`)→404, anything else→500. Endpoints stay free of try/catch.
- **W-01 = Option A (async interface), in PR 1.** Change `IEfectivoPrecioService` to async (`Task<...>`), rewrite `EfectivoPrecioService` to await the repositories, update call sites. Domain stays zero-dependency (`Task` is BCL). Landing this in PR 1 guarantees no price-confirmation endpoint is ever exposed over a deadlock-prone sync-over-async path.
- **List endpoints, additive ports.** Add `GetAllAsync` to each repository port + EF implementation (full owned-graph load, consistent with the existing `GetByIdAsync` loading contract). GET-all endpoints make seeded data browsable.
- **JWT pipeline wired, endpoints anonymous.** `AddAuthentication(JwtBearerDefaults)` (config already present: Issuer/Audience/SigningKey + startup guard) + `UseAuthentication()` + `UseAuthorization()`. All Phase-4 endpoints carry `[AllowAnonymous]`. Phase 5 removes the attribute and adds the login endpoint + `Usuario` aggregate.
- **Pedido role from body (temporary).** State-transition endpoints accept `RolUsuario` in the request body and pass it to `pedido.TransicionarEstado(...)`. This is a **deliberate, documented security hole** for the no-login window — Phase 5 replaces it with JWT claim extraction (see Security note).
- **Runtime seeder via factories.** `DevDataSeeder` in `Infrastructure.Persistence` uses the domain factories + repositories (so invariants hold and domain events fire), gated on `IsDevelopment()`, called from `Program.cs` after auto-migrate. Idempotent: `if (await dbContext.Clientes.AnyAsync()) return;`. Because `Menu.Crear()` requires a **future** date, the seeder computes tomorrow dynamically (`DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)`) at run time.
- **Integration via WebApplicationFactory.** `tests/GastroGestion.Api.Tests/` spins up the real pipeline with `WebApplicationFactory<Program>` against LocalDB, tagged `[Trait("Category","Integration")]`. PR 1 ships a smoke test (health + one POST); endpoint coverage grows with PRs 2–3.

## Seeder sample data

Built through domain factories, fired through repositories, idempotent on `Clientes.AnyAsync()`:

- **3 Clientes** — one `ConsumidorFinal`, one `ResponsableInscripto` (with CUIT), one `Exento`.
- **5 Ingredientes** — varied `UnidadDeMedida`.
- **3 Platos** — each with recipe lines (`LineaReceta`) referencing the seeded ingredients.
- **1 Menu** — `FechaMenu = tomorrow` (computed dynamically), with menu items including one `PrecioOverride`.
- **4 Mesas** — varied capacities.
- **1 Salon Pedido** and **1 TakeAway (Mostrador) Pedido** — with lines and confirmed prices.
- **1 TicketInterno Factura** — produced from the TakeAway Pedido.

## Affected areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/GastroGestion.Api/Program.cs` | Modified | JWT pipeline, `AddProblemDetails`, exception handler registration, FluentValidation registration, endpoint group registration, seeder trigger |
| `src/GastroGestion.Api/` (endpoints) | New | `Map*Endpoints` extensions per aggregate, `WithValidation<T>` filter, `GastroGestionExceptionHandler` |
| `src/GastroGestion.Api/GastroGestion.Api.csproj` | Modified | Add FluentValidation (+ JwtBearer if not already in the Web SDK); **remove** `Microsoft.AspNetCore.OpenApi` |
| `src/GastroGestion.Contracts/` | New | Request/response DTOs + static mapping extensions + validators (currently empty) |
| `src/GastroGestion.Application/` | New + Modified | All missing handlers (catalogue/transactional/fiscal/stock); `GetAllAsync` on ports; async `EfectivoPrecioService` (W-01) |
| `src/GastroGestion.Domain/Services/IEfectivoPrecioService.cs` | Modified (minimal) | Async signature (`Task<...>`); no new dependency |
| `src/GastroGestion.Infrastructure/` | New + Modified | `DevDataSeeder`; `GetAllAsync` implementations on each repository |
| `tests/GastroGestion.Api.Tests/` | New | `WebApplicationFactory<Program>` integration tests on LocalDB |
| `Dominio/` + `APIs/` (legacy net48) | Untouched | Not modified, not ported |

## Delivery strategy — 3 slices, chained PRs (stacked-to-main)

Matches the Phase-2/Phase-3 chained-PR pattern. Strict dependency order; each slice independently reviewable and testable on LocalDB. Stacked-to-main: each PR merges to `main` in order.

- **PR 1 — API foundation.** Minimal-API host scaffolding; JWT auth pipeline (`[AllowAnonymous]`); `GastroGestionExceptionHandler : IExceptionHandler` + `AddProblemDetails()`; FluentValidation endpoint-filter infrastructure; `DevDataSeeder` (idempotent, dev-only); **W-01 async fix** (`IEfectivoPrecioService` + `EfectivoPrecioService` + call sites); remove `Microsoft.AspNetCore.OpenApi`; `GastroGestion.Api.Tests` project with a `WebApplicationFactory` smoke test. Foundational, must precede any endpoint. **~150–250 lines.**
- **PR 2 — Catalogue endpoints + use cases.** `CrearCliente`, `GetClienteById`, `GetAllClientes`, `CrearIngrediente`, `CrearPlato`, `CrearMenu`, `CrearMesa` handlers; catalogue DTOs + validators in `Contracts`; `GetAllAsync` added to catalogue repository ports + impls; GET-all + GET-by-id + POST endpoint groups. Depends on PR 1. **May exceed 400 lines** (chained delivery already covers this).
- **PR 3 — Transactional + fiscal + stock endpoints.** `CrearPedido`, `AgregarLinea`, `ConfirmarPrecioLinea` (exercises W-01), `TransicionarEstadoPedido` (role from body), `GetPedidoById`; `CrearFactura` endpoint wiring + `RegistrarPago` + `GetFacturaById`; `RegistrarMovimientoStock` + `GetBalanceStock`; their DTOs + validators. Depends on PR 1–2. **May exceed 400 lines** (chained delivery already covers this). Highest product risk (price/fiscal/stock paths); isolated last.

### Review Workload Forecast

- **Estimated changed lines:** ~1,200–1,800 across the three slices (host wiring + handlers + DTOs/validators + seeder + GetAll ports/impls + integration tests). Above the 400-line single-PR budget.
- **400-line budget risk:** High for PR 2 and PR 3.
- **Chained PRs recommended:** Yes — three stacked slices (Foundation → Catalogue → Transactional/Fiscal/Stock), stacked-to-main. **Already adopted** — PR 2 and PR 3 are expected to exceed 400 lines and that is accepted under the chained plan.
- **Decision needed before apply:** No additional decision — the chained/stacked plan and the locked decisions resolve the open questions the exploration raised.

## Security note (MUST close in Phase 5)

Two deliberate, time-boxed holes exist only because there is no login yet:

1. **All endpoints `[AllowAnonymous]`.** The JWT pipeline is wired but unprotected. Phase 5 removes `[AllowAnonymous]` once the login endpoint + `Usuario` aggregate exist.
2. **`RolUsuario` accepted from the request body** on Pedido state-transition endpoints. A client can claim any role and pass the domain role gate. This is a genuine authorization bypass and is acceptable ONLY for the no-login Phase-4 window. **Phase 5 MUST replace body-supplied `RolUsuario` with the role extracted from the authenticated JWT claim.**

Both are explicitly tracked as Phase-5 remediation, not accidental gaps.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| **W-01 deadlock** under ASP.NET Core's SynchronizationContext if the price path is exposed before the async fix | High (if unfixed) | W-01 async fix lands in **PR 1**, before any `ConfirmarPrecio` endpoint exists; integration test hits the price path on the real HTTP stack |
| **Menu seeder future-date idempotency edge case**: a previously-seeded menu's `FechaMenu` (tomorrow at seed time) goes stale; the idempotency check skips re-seed, so `GetActivosByFecha` for "today" returns nothing on later days | Med | Compute the date dynamically at seed time; document a **reseed/truncate path** (drop the dev DB or a `--reseed` dev switch) so a developer can refresh stale seed data; integration tests seed fresh per run |
| **`RolUsuario` from body** is an authorization bypass | High (by design, time-boxed) | Documented Security note; Phase 5 replaces it with JWT-claim extraction; only `[AllowAnonymous]` dev window is affected |
| **Redundant OpenAPI packages** (`Microsoft.AspNetCore.OpenApi` is the .NET-9 native package on a net8 project) cause subtle behaviour differences with Swashbuckle | Low | Remove `Microsoft.AspNetCore.OpenApi` in PR 1; keep Swashbuckle 6.6.2 only |
| **LocalDB required for `Api.Tests`** complicates CI | Low | `[Trait("Category","Integration")]` allows skipping in CI; document the LocalDB prerequisite; Testcontainers is the documented future upgrade |
| **Domain accidentally gains a framework dependency** via the W-01 change | Low | `Task` is BCL; gate: review `GastroGestion.Domain.csproj` for zero package/project refs before merge |
| **Exposing a domain aggregate over the wire** by accident | Low | DTO-only contract enforced; integration tests assert response shapes; review gate on endpoint signatures |
| **`GetAllAsync` over-fetch / N+1** when loading full owned graphs for lists | Low/Med | Mirror the existing `GetByIdAsync` eager-include strategy; dev-scale data only; pagination is a later concern (non-goal) |

## Rollback plan

Greenfield surface with no live consumer yet. Rollback = revert the slice's commits / close the slice PR. Each PR is independently revertible; reverting PR 3 does not touch PR 1–2. The `DevDataSeeder` runs only in `Development` and is idempotent — dropping the dev LocalDB (`dotnet ef database drop` or deleting the `.mdf`) fully resets seeded state. The W-01 async change is additive within the existing async chain and can be reverted independently if needed (though it should not be — it removes a real deadlock). No production data, no external consumer, no AFIP call is affected.

## Dependencies

- Phase 2 domain (`domain-port`) and Phase 3 persistence (`persistence-ef-core`) — complete, archived, on `main`.
- .NET 8 SDK + EF Core tooling.
- SQL Server LocalDB (`mssqllocaldb`) for runtime + `Api.Tests`.
- FluentValidation package; `Microsoft.AspNetCore.Authentication.JwtBearer` (typically resolved by the net8 Web SDK — confirm in design).

## Success criteria

- [ ] W-01 fixed: `IEfectivoPrecioService` is async, `EfectivoPrecioService` has no `.GetAwaiter().GetResult()`, Domain still has zero package/project refs.
- [ ] JWT pipeline wired (`AddAuthentication`/`UseAuthentication`/`UseAuthorization`); all Phase-4 endpoints `[AllowAnonymous]`.
- [ ] `IExceptionHandler` + ProblemDetails map Conflict→409, Domain→422, not-found→404, unhandled→500.
- [ ] FluentValidation endpoint filter returns `ValidationProblem` (400) on invalid requests.
- [ ] DTOs live only in `Contracts`; no domain aggregate is serialised over the wire.
- [ ] `Microsoft.AspNetCore.OpenApi` removed; Swashbuckle remains, dev-only.
- [ ] All catalogue/transactional/fiscal/stock use cases implemented and exposed; `GetAllAsync` added to ports + impls; GET-all endpoints return seeded data.
- [ ] `DevDataSeeder` runs dev-only, idempotently, through domain factories, with `Menu.FechaMenu = tomorrow` computed dynamically.
- [ ] `dotnet test tests/GastroGestion.Api.Tests/` passes against LocalDB (smoke + endpoint coverage).

## Next step

Proceed to `sdd-spec` and `sdd-design` (can run in parallel). Spec captures Given/When/Then for each endpoint group, the ProblemDetails status mapping, the validation-filter behaviour, the W-01 async contract, the `[AllowAnonymous]`/body-role security posture, the `GetAllAsync` loading contract, and the seeder idempotency + future-date behaviour. Design locks the endpoint-group layout, the handler-injection wiring, the DTO/mapping shapes, the exception-handler mapping table, the FluentValidation filter, the async `IEfectivoPrecioService` signature + call sites, and the seeder structure — and reconciles indicative use-case names against the actual CLR types.
