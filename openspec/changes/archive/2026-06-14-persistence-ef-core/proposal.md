# Persist the GastroGestion domain with EF Core 8 (Infrastructure layer)

Give the completed Phase-2 domain a persistence skin: EF Core 8 mapping for **all** aggregates, repository/UnitOfWork ports, an in-process domain-event dispatcher, code-first migrations against a **local** SQL Server LocalDB, and a LocalDB-backed integration test suite. The Domain project stays at **zero outward dependencies** — every EF concern lives in `GastroGestion.Infrastructure` via `IEntityTypeConfiguration<T>` per aggregate. This phase also closes the one invariant the domain deferred to the application layer (REQ-13/13-G, multi-client Factura rejection).

## Why now

This is **phase 3 of 7** in the strangler roadmap (scaffold ✅ → domain port ✅ → **infrastructure/EF Core** → application → API+security → stock/OT hardening → Blazor). The Domain (8 aggregate roots, ~15 owned entities/VOs, 5 events, 147 tests) is complete and EF-ready structurally (private parameterless ctors, backing-field collections, plain `byte[]` rowversions — no framework attributes). The `Infrastructure` project is an empty placeholder with no EF packages. Nothing downstream — application use cases, the OT batch transaction, invoice persistence — can be built until aggregates can round-trip to a database. Full analysis: engram `sdd/persistence-ef-core/explore` (obs #47).

## What success looks like

- `dotnet ef migrations add` + `database update` build the full greenfield schema on `(localdb)\mssqllocaldb`.
- Every aggregate round-trips (save → reload) with all invariants intact, including the set-once price flag and append-only ledger guard.
- `GastroGestion.Domain.csproj` still has **zero** package/project references (Clean Architecture innermost-layer rule holds).
- REQ-13/13-G is enforced: a `CrearFactura` use case rejects mixing Pedidos from different clients with a `ConflictException`.
- Connection string is config-driven (`appsettings` + user-secrets), so Azure SQL becomes a config-only change later.

## Scope

### In scope

| Area | What lands |
|------|-----------|
| EF plumbing | `GastroGestionDbContext`, `DbSet` per aggregate, `IDesignTimeDbContextFactory`, EF Core 8 + SQL Server provider packages added to Infrastructure only |
| Mappings | One `IEntityTypeConfiguration<T>` per aggregate root, backing-field collections (`HasField`/`PropertyAccessMode.Field`), owned types, value converters, JSON columns, rowversion tokens |
| Aggregates | Cliente (+Direccion), Ingrediente, Plato (+LineaReceta), Menu (+MenuItem), Mesa, Pedido (+LineaPedido +OrdenTrabajo), MovimientoStock, Factura (+FacturaLinea +Pago) |
| Ports | `IUnitOfWork` + per-aggregate repositories in `Application`; implementations in `Infrastructure`. `IMovimientoStockRepository` exposes **AddAsync only** |
| Events | `IDomainEventDispatcher` (port in Application), in-process **post-SaveChanges** dispatcher (impl in Infrastructure) |
| Application | `CrearFactura` use case enforcing REQ-13/13-G; `ConflictException` |
| Migrations | EF code-first migrations in `Infrastructure/Persistence/Migrations/`, applied via `MigrateAsync` |
| Tests | Integration test project against LocalDB (round-trip, ledger guard, dispatcher, multi-client rejection) |
| Domain (minimal) | Expose `internal bool PrecioConfirmado { get; private set; }` on `LineaPedido` so the set-once flag persists |

### Out of scope (deferred)

- **Legacy data migration / ETL** — see Assumption 1 (greenfield).
- **Transactional outbox** — deferred to the AFIP phase; Phase 3 dispatches in-process post-commit (the seam exists, no AFIP HTTP call).
- **Materialized `StockBalance` read model** — balance stays a `SUM(Cantidad)` projection query (`CalcularBalance`); concurrency-hardened table deferred to Phase 5+.
- **Gap-free sequence generation** (`NumeroCliente`/`NumeroPlato`/etc.) — assigned by an infrastructure sequencer in a later slice/phase; not part of mapping.
- **Application use cases beyond `CrearFactura`** — full CQRS/use-case layer is Phase 4. `IEfectivoPrecioService`/`ICalculadorFactura` implementations may land minimally here only as far as Factura needs them, otherwise deferred to Phase 4.
- **Testcontainers / Docker / CI** — LocalDB only for now; Testcontainers is a future upgrade. **No Docker mandated.**
- **Azure SQL / cloud** — design stays cloud-ready (config-driven), but cloud is a later config-only change.

## Capabilities

> Contract with the spec phase. Mapped to slices; the spec phase decomposes.

### New capabilities

- `persistence-foundation`: DbContext, infra plumbing, value converters, catalogue + Cliente mappings, first migration, catalogue repositories (Slice A).
- `persistence-transactional`: Pedido mapping (owned LineaPedido/OrdenTrabajo, nullable DireccionEntrega, PrecioConfirmado, rowversion, LineaRecetaSnapshot JSON), MovimientoStock append-only repo, domain-event dispatcher (Slice B).
- `persistence-fiscal`: Factura mapping (flat table + discriminator column, PedidosFacturados JSON), Factura repository, `CrearFactura` use case enforcing REQ-13/13-G (Slice C).

### Modified capabilities

- `Domain` (REQ-08): `LineaPedido` exposes `internal bool PrecioConfirmado` so the set-once invariant survives reload. **No framework dependency added** — it is a plain CLR property backed by the existing `_precioConfirmado` field. This is the ONLY domain change.

## Approach

All EF concern lives in Infrastructure; the domain is mapped from the outside.

- **Per-aggregate fluent config.** One `IEntityTypeConfiguration<T>` per root under `Infrastructure/Persistence/Configurations/`. Backing-field collections (`_lineas`, `_ordenesTrabajo`, `_pagos`, `_items`, `_direcciones`) mapped with `HasField` + `PropertyAccessMode.Field`; aggregates expose `IReadOnlyList<T>`, EF populates the `List<T>`.
- **Value-object mapping by shape.** Multi-field VOs (`Dinero`, `Cantidad`, `DireccionEntrega`) → `OwnsOne` with explicit `HasColumnName` per path (avoids nested-VO column collisions). Single-field VOs (`Cuit`, `Email`, `LegajoId`, `PorcentajeIVA`) → value converters. `LineaRecetaSnapshot` (a `record` carrying a nested `Cantidad`) → `OwnsMany(...).ToJson()` audit column.
- **Factura = flat single table + discriminator column.** The domain is a single `Factura` class with a `TipoComprobante` enum — **not** an EF inheritance hierarchy. Map one `Facturas` table, `TipoComprobante` as an `int` column, nullable `CAE`/`VencimientoCAE`. No `HasDiscriminator`, no TPH/TPT. (Corrects the misleading "TPH" code comment.)
- **Append-only ledger.** `IMovimientoStockRepository` exposes only `AddAsync`. `SaveChangesAsync` override guards: any `Modified`/`Deleted` entry whose entity is a `MovimientoStock` throws. `PedidosFacturados` (`List<Guid>`) → JSON column (see Assumption 2). Balance = `SUM(Cantidad)` query.
- **In-process events post-commit.** `SaveChangesAsync` override: collect events from tracked roots → `SaveChangesAsync` → dispatch via injected `IDomainEventDispatcher` → clear. No HTTP in the transaction; the `FacturaNecesitaCAE` seam is satisfied without calling AFIP.
- **Specific repositories, no leaky base.** Per-aggregate interfaces in `Application` expose only needed operations; no generic `IRepository<T>` that leaks `Update`/`Delete` onto append-only roots. `IUnitOfWork` wraps the DbContext transaction.
- **REQ-13/13-G at the application boundary.** `CrearFactura` loads each Pedido via `IPedidoRepository`, asserts all `ClienteId` match, throws `ConflictException` on mismatch, then calls the domain factory. Closes the Phase-2 deferred item without crossing the aggregate boundary inside the domain.

## Affected areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/GastroGestion.Infrastructure/` | New | DbContext, configurations, repositories, dispatcher, design-time factory, migrations |
| `src/GastroGestion.Application/` | New (ports + 1 use case) | Repository/UoW/dispatcher interfaces, `ConflictException`, `CrearFactura` |
| `src/GastroGestion.Domain/LineaPedido` | Modified (minimal) | Expose `internal bool PrecioConfirmado` (no new dependency) |
| `tests/GastroGestion.Infrastructure.Tests/` | New | LocalDB integration tests |
| `appsettings.Development.json` + user-secrets | New/Modified | Config-driven connection string |
| `Dominio/` (legacy net48) | Untouched | Not modified, not migrated |

## Delivery strategy — 3 slices, chained/stacked PRs (stacked-to-main)

Matches the Phase-2 chained-PR pattern. Strict dependency order; each slice independently reviewable and testable on LocalDB.

- **Slice A — Foundation.** EF packages, `GastroGestionDbContext`, design-time factory, single-field value converters, catalogue + Cliente mappings (Cliente/Direccion, Ingrediente, Plato/LineaReceta, Menu/MenuItem, Mesa), first migration, catalogue repositories + `IUnitOfWork`. Structural, low risk.
- **Slice B — Transactional.** Pedido config (owned `LineaPedido`/`OrdenTrabajo`, nullable `DireccionEntrega`, `PrecioConfirmado` mapping, `RowVersion` token on Pedido/Mesa, `LineaRecetaSnapshot` JSON), `MovimientoStock` config + append-only repo + SaveChanges guard, `IDomainEventDispatcher` impl. Highest mapping complexity. Depends on A.
- **Slice C — Fiscal.** Factura config (flat table, discriminator column, nullable CAE, `PedidosFacturados` JSON), owned `FacturaLinea`/`Pago`, `IFacturaRepository`, `CrearFactura` use case + REQ-13/13-G enforcement, integration test project finalized. Depends on A–B. Highest product risk (fiscal); isolated.

### Review Workload Forecast

- **Estimated changed lines:** ~1,500–2,200 (configs + repos + dispatcher + migrations + integration tests). Well above the 400-line single-PR budget.
- **400-line budget risk:** High.
- **Chained PRs recommended:** Yes — three stacked slices (Foundation → Transactional → Fiscal), stacked-to-main.
- **Decision needed before apply:** Yes. `ask-on-risk`: confirm chained/stacked PRs and chain strategy before `sdd-apply`.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Nested `Dinero`/`PorcentajeIVA` owned types collide on column names (e.g. `FacturaLinea.PrecioUnitario.Monto`) | High | Explicit `HasColumnName` per owned path (`PrecioUnitario_Monto`, etc.); a round-trip integration test per nested-VO aggregate |
| Nullable `DireccionEntrega` owned type — EF owned types non-nullable by default | Med | `OwnsOne(..., n => n.Navigation(...).IsRequired(false))`; test save/reload of a non-delivery Pedido (null address) |
| `RowVersion`/`ConcurrencyToken` initialization — domain defaults to `[]`, EF must own generation | Med | `Property(...).IsRowVersion()` (SQL Server `rowversion`); test optimistic-concurrency conflict surfaces a `DbUpdateConcurrencyException` |
| `LineaRecetaSnapshot` record JSON with nested `Cantidad` VO serialization | Med | `OwnsMany(...).ToJson()`; round-trip test asserting snapshot survives later Plato recipe edits (REQ-10-E) |
| `_precioConfirmado` not persisted → reloaded line allows second `ConfirmarPrecio` | Med | Expose `internal PrecioConfirmado`, map it, integration test: reload → `ConfirmarPrecio` again → throws |
| Append-only guard misses an entry state | Low | SaveChanges guard unit/integration test: attempt Modify and Delete of a ledger row → both throw |
| Domain accidentally gains an EF dependency | Low | Gate: review `GastroGestion.Domain.csproj` for zero package refs before each slice merge |
| LocalDB unavailable on a dev/CI box | Low | Config-driven connection string; document LocalDB prerequisite; Testcontainers is the documented future upgrade |

## Flagged assumptions (user may veto)

1. **Greenfield schema — NO legacy data migration in Phase 3.** EF migrations build new empty tables. Rationale: this is an academic/strangler rebuild and the legacy SQL Server schema is structurally incompatible (removed multi-tenant `Id_Empresa`/`Id_Sucursal`, denormalized stored totals vs computed totals, no append-only ledger concept). A legacy-data ETL is a separate future work item only if live legacy data must be preserved. **Veto effect:** if real data must migrate, add an ETL work item alongside (not inside) the EF migration slices.
2. **`Factura.PedidosFacturados` (`List<Guid>`) → JSON column** (EF Core 8 primitive collection via `ToJson` on the private backing field), NOT a junction table. Rationale: it is an audit-style reference list never filtered in SQL. **Veto effect:** if "find all Facturas for a given Pedido" becomes a required query, switch to a `FacturaPedidos(FacturaId, PedidoId)` junction table.

## Rollback plan

Greenfield and no downstream consumer yet. Rollback = revert the slice's commits / close the slice PR and drop the LocalDB database (`dotnet ef database drop` or delete the `.mdf`). No production data, no API contract, no live consumer is affected. Each slice PR is independently revertible; reverting Slice C does not touch A–B. The single domain change (`PrecioConfirmado`) is additive and safe to keep or revert independently.

## Dependencies

- Phase 2 domain (`domain-port`) — complete, archived, on `main`.
- .NET 8 SDK + EF Core tooling (`dotnet-ef`).
- SQL Server LocalDB (`mssqllocaldb`) installed locally (ships with VS / SQL Server tools).

## Success criteria

- [ ] EF migrations create the full greenfield schema on `(localdb)\mssqllocaldb`.
- [ ] `GastroGestion.Domain.csproj` still has zero package/project references.
- [ ] Every aggregate round-trips (save → reload) preserving invariants, including `PrecioConfirmado` and the append-only ledger guard.
- [ ] `CrearFactura` rejects multi-client Pedido groups with `ConflictException` (REQ-13/13-G).
- [ ] In-process domain-event dispatch fires post-commit and clears events.
- [ ] Connection string is config-driven (no hardcoded server); Azure SQL would be config-only.
- [ ] Integration tests pass against LocalDB via `dotnet test`.

## Next step

Proceed to `sdd-spec` and `sdd-design` (can run in parallel). Spec captures Given/When/Then for round-trip persistence, the append-only guard, the dispatcher seam, and REQ-13/13-G. Design locks the configuration shapes (owned vs converter vs JSON per VO), the repository/UoW/dispatcher contracts, and the slice boundaries.
