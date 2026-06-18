# Infrastructure Specification — EF Core 8 Persistence Layer

**Scope:** `GastroGestion.Infrastructure` — EF Core 8 persistence skin for the completed Phase-2 domain.
Also covers `GastroGestion.Application` (repository/UoW/dispatcher ports + `CrearFactura` use case) and one minimal additive change to `GastroGestion.Domain/LineaPedido`.

**Phase:** 3 of 7 in the strangler roadmap (scaffold ✅ → domain port ✅ → **infrastructure/EF Core** ✅ → application → API+security → stock/OT hardening → Blazor).
**Archive date:** 2026-06-14
**Status:** COMPLETE — all 26 implementation tasks delivered across three slices (Foundation, Transactional, Fiscal). Merged to `main` via PRs #6 (Slice A), #7 (Slice B), #8 (Slice C). Main HEAD: de56348.
**Test coverage:** 175 tests green (148 domain + 27 infrastructure integration against LocalDB).
**Language covenant:** all identifiers, class names, and domain nouns follow the Spanish ubiquitous language established in the Domain layer. Infrastructure code uses English for framework concepts.

---

## Non-goals (explicitly out of scope)

- Legacy data migration or ETL (greenfield schema only)
- Transactional outbox (deferred to AFIP phase)
- Materialized `StockBalance` read model (deferred to Phase 5+)
- Gap-free sequence generation (`NumeroCliente`, `NumeroPlato`, etc.)
- Full Phase-4 CQRS/use-case layer (only `CrearFactura` lands here)
- Testcontainers / Docker / CI pipeline setup
- Azure SQL or cloud deployment (config-ready, not deployed)
- `IEfectivoPrecioService` and `ICalculadorFactura` full implementations (minimal stubs land here; full wiring is Phase 4)

---

## Architecture constraints (locked)

| Constraint | Status |
|---|---|
| `GastroGestion.Domain.csproj` — zero `PackageReference` and zero `ProjectReference` | ENFORCED — verified at every slice gate |
| All EF Core concern lives exclusively in `GastroGestion.Infrastructure` | ENFORCED — per-aggregate `IEntityTypeConfiguration<T>` |
| Local provider = SQL Server LocalDB; connection string is config-driven | ENFORCED |
| Greenfield schema — no legacy data migration | LOCKED (Assumption 1) |
| `Factura.PedidosFacturados` stored as JSON column, not junction table | LOCKED (Assumption 2) |

---

## REQ-01 — EF Core schema bootstraps on a fresh LocalDB

The system MUST produce a complete, correct greenfield schema by running EF Core code-first migrations against `(localdb)\mssqllocaldb`. The schema MUST represent all 8 aggregate roots. No legacy data migration is performed.

### Scenario 01-A — Clean migration apply

- GIVEN a fresh `(localdb)\mssqllocaldb` instance with no GastroGestion database
- WHEN `dotnet ef database update` is executed against the Infrastructure project
- THEN the command exits with code 0
- AND the GastroGestion database contains tables for all 8 aggregate roots

### Scenario 01-B — Idempotent re-apply

- GIVEN the GastroGestion database already exists at the current migration head
- WHEN `dotnet ef database update` is executed again
- THEN the command exits with code 0 and makes no structural changes

---

## REQ-02 — Domain project retains zero outward dependencies

`GastroGestion.Domain.csproj` MUST contain zero `<PackageReference>` and zero `<ProjectReference>` elements. All EF Core concern MUST reside exclusively in `GastroGestion.Infrastructure`.

### Scenario 02-A — Domain .csproj has no package or project references

- GIVEN `GastroGestion.Domain.csproj` is inspected after Phase 3 is merged
- WHEN all `<PackageReference>` and `<ProjectReference>` elements are counted
- THEN the combined count is 0

### Scenario 02-B — Domain compiles in isolation

- GIVEN the .NET 8 SDK is installed
- WHEN `dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj` is executed
- THEN the command exits with code 0 with no build errors

---

## REQ-03 — Catalogue aggregate round-trips (Cliente, Ingrediente, Plato, Menu, Mesa)

Each catalogue aggregate MUST persist and reload with all properties and owned-entity graphs intact. Value objects MUST survive the round-trip without losing their values or validation constraints.

### Scenario 03-A — Cliente with Direcciones round-trip

- GIVEN a `Cliente` with `Nombre`, `Email`, `Cuit`, `CondicionIVA`, and two `Direccion` entries
- WHEN the entity is saved via `IClienteRepository.AddAsync` + `IUnitOfWork.SaveChangesAsync`
- AND subsequently loaded by its `Id`
- THEN the reloaded `Cliente` has identical `Nombre`, `Email.Valor`, `Cuit.Valor`, `CondicionIVA`
- AND `Direcciones` contains exactly 2 entries with the original values

### Scenario 03-B — Plato with LineaReceta round-trip

- GIVEN a `Plato` with `PrecioBase` (Dinero), `AlicuotaIVA`, and 3 `LineaReceta` entries each with a `Cantidad`
- WHEN persisted and reloaded
- THEN `PrecioBase.Monto` and `PrecioBase.Moneda` match the original
- AND `LineasReceta` contains exactly 3 lines with correct `IngredienteId` and `Cantidad.Valor`

### Scenario 03-C — Mesa round-trip

- GIVEN a `Mesa` with `Numero`, `Capacidad`, `Estado`, and `Activa = true`
- WHEN persisted and reloaded
- THEN all fields match and `RowVersion` (rowversion token) is non-empty

---

## REQ-04 — Value object converters round-trip correctly

Single-field value objects (`Cuit`, `Email`, `PorcentajeIVA`, `LegajoId`) MUST be stored via value converters and restore with the original normalized value. Multi-field value objects (`Dinero`, `Cantidad`, `DireccionEntrega`) MUST be stored as owned types with explicit column names and restore with all fields intact.

### Scenario 04-A — Cuit and Email preserve normalized form

- GIVEN a `Cliente` with `Cuit = new Cuit("20-34567890-1")` and `Email = new Email("Test@EXAMPLE.com")`
- WHEN persisted and reloaded
- THEN `Cuit.Valor` is `"20345678901"` and `Email.Valor` is `"test@example.com"`

### Scenario 04-B — Dinero OwnsOne survives nested paths

- GIVEN a `Factura` with a `FacturaLinea` carrying `PrecioUnitario = new Dinero(150m, "ARS")` and a `PorcentajeIVA`
- WHEN persisted and reloaded
- THEN `FacturaLinea.PrecioUnitario.Monto` is `150m` and `Moneda` is `ARS`
- AND no EF column name collision occurs (each nested VO path uses a distinct column name)

### Scenario 04-C — Nullable DireccionEntrega round-trips as null for non-Delivery Pedidos

- GIVEN a `Pedido` with `Tipo = Mostrador` (no delivery address)
- WHEN persisted and reloaded
- THEN `pedido.DireccionEntrega` is `null`

### Scenario 04-D — DireccionEntrega round-trips for Delivery Pedidos

- GIVEN a `Pedido` with `Tipo = Delivery` and a non-null `DireccionEntrega` with all fields populated
- WHEN persisted and reloaded
- THEN `pedido.DireccionEntrega` is non-null and all address fields match the original

---

## REQ-05 — LineaPedido set-once price invariant survives reload

`LineaPedido.PrecioConfirmado` (the `internal bool` flag backing the set-once price constraint) MUST be persisted and correctly restored. A reloaded line whose price was confirmed MUST still reject a second `ConfirmarPrecio` call.

### Scenario 05-A — Confirmed line rejects re-confirmation after reload

- GIVEN a `LineaPedido` that has had `ConfirmarPrecio(precio, alicuota)` called
- AND the parent `Pedido` has been saved and reloaded from the database
- WHEN `lineaPedido.ConfirmarPrecio(...)` is called again on the reloaded instance
- THEN a `DomainException` is thrown (set-once invariant enforced post-reload)

### Scenario 05-B — Unconfirmed line allows confirmation after reload

- GIVEN a `LineaPedido` with `PrecioConfirmado = false` that has been saved and reloaded
- WHEN `lineaPedido.ConfirmarPrecio(new Dinero(200m, "ARS"), new PorcentajeIVA(0.21m))` is called
- THEN no exception is thrown
- AND `PrecioUnitario` is `200m ARS` and `IVA` is set

---

## REQ-06 — Pedido with owned graphs round-trips completely

A `Pedido` with `LineaPedido` and `OrdenTrabajo` collections MUST persist and reload with all owned entities and their value objects intact.

### Scenario 06-A — Pedido with lines and OTs round-trip

- GIVEN a `Pedido` with 2 `LineaPedido` entries and 1 `OrdenTrabajo` with a `RecetaSnapshot` containing a nested `Cantidad` VO
- WHEN persisted and reloaded
- THEN `Lineas` contains 2 entries with correct `PlatoId` and `Cantidad`
- AND `OrdenesTrabajo` contains 1 entry whose `RecetaSnapshot` has the original ingredient amounts

### Scenario 06-B — RecetaSnapshot JSON survives later Plato recipe changes

- GIVEN an `OrdenTrabajo` created when a `Plato` had 2 recipe lines
- AND the `Plato` recipe is subsequently updated with a third line
- WHEN the `OrdenTrabajo` is reloaded from the database
- THEN `RecetaSnapshot` still contains exactly 2 entries (snapshot is frozen at OT creation time)

---

## REQ-07 — MovimientoStock is append-only at the persistence boundary

`IMovimientoStockRepository` MUST expose only `AddAsync` (plus balance query). The `SaveChangesAsync` override MUST reject any attempt to update or delete a persisted `MovimientoStock` row. Balance MUST be computed as `SUM(Cantidad)` for a given `IngredienteId`.

### Scenario 07-A — Repository exposes no update or delete

- GIVEN the `IMovimientoStockRepository` interface definition
- WHEN its members are inspected
- THEN it exposes only `AddAsync` and `CalcularBalanceAsync`; no `Update`, `Remove`, or `Delete` members exist

### Scenario 07-B — SaveChanges guard rejects modification of a ledger row

- GIVEN a `MovimientoStock` that has already been persisted
- WHEN application code retrieves it and attempts to modify any of its fields and calls `SaveChangesAsync`
- THEN `SaveChangesAsync` throws before committing (guard fires on `EntityState.Modified` for `MovimientoStock`)

### Scenario 07-C — SaveChanges guard rejects deletion of a ledger row

- GIVEN a persisted `MovimientoStock`
- WHEN application code marks it for deletion and calls `SaveChangesAsync`
- THEN `SaveChangesAsync` throws before committing (guard fires on `EntityState.Deleted` for `MovimientoStock`)

### Scenario 07-D — Balance projection is correct SUM

- GIVEN the following `MovimientoStock` entries for `IngredienteId = X`:
  - Compra: +20m
  - Reserva: -5m
  - Consumo: -5m
  - LiberacionReserva: +5m
- WHEN the repository's balance query for `IngredienteId = X` is executed
- THEN the result is `15m`

---

## REQ-08 — Domain events dispatch exactly once after SaveChanges and are cleared

The in-process `IDomainEventDispatcher` MUST dispatch all domain events from tracked aggregate roots exactly once after a successful `SaveChangesAsync` call. Events MUST be cleared from the root after dispatch. A failed `SaveChangesAsync` MUST NOT dispatch events.

### Scenario 08-A — FacturaNecesitaCAE reaches its handler post-commit

- GIVEN a `Factura` created via `CrearFacturaElectronica` (which raises `FacturaNecesitaCAE`)
- WHEN `IUnitOfWork.SaveChangesAsync` completes successfully
- THEN the registered `FacturaNecesitaCAE` handler is invoked exactly once
- AND `factura.DomainEvents` is empty after dispatch

### Scenario 08-B — Events not dispatched on SaveChanges failure

- GIVEN an operation that raises a domain event but causes a DB constraint violation during save
- WHEN `SaveChangesAsync` throws a database exception
- THEN no domain event handler is invoked

### Scenario 08-C — Events cleared after successful dispatch

- GIVEN an aggregate root that raised events during an operation
- WHEN `SaveChangesAsync` completes successfully and events are dispatched
- THEN `aggregateRoot.DomainEvents` is empty (count = 0)

---

## REQ-09 — Optimistic concurrency via RowVersion on Pedido and Mesa

`Pedido` and `Mesa` MUST carry a `RowVersion` concurrency token. Concurrent updates to the same row MUST surface as `DbUpdateConcurrencyException`.

### Scenario 09-A — Concurrent Pedido update throws DbUpdateConcurrencyException

- GIVEN a `Pedido` that is loaded into two separate EF Core tracking contexts (context A and context B)
- WHEN context A saves a state change first
- AND context B then attempts to save its own state change with the stale RowVersion
- THEN `SaveChangesAsync` on context B throws `DbUpdateConcurrencyException`

### Scenario 09-B — RowVersion is non-empty after first save

- GIVEN a new `Pedido` or `Mesa` saved to the database
- WHEN the entity is reloaded
- THEN its `RowVersion` property is non-null and non-empty (`byte[]` with at least 1 byte)

---

## REQ-10 — Factura persists as a flat single table with discriminator column

`Factura` MUST be stored in a single `Facturas` table with a `TipoComprobante` discriminator column (int). `CAE` and `VencimientoCAE` MUST be nullable columns (null for non-electronic comprobantes). No EF Core inheritance hierarchy (`HasDiscriminator`) is used.

### Scenario 10-A — TicketInterno persists with null CAE

- GIVEN a `Factura` created via `CrearTicket(...)`
- WHEN persisted and reloaded
- THEN `TipoComprobante` is `TicketInterno`, `CAE` is null, `VencimientoCAE` is null
- AND all `FacturaLinea` and `Pago` owned collections are present

### Scenario 10-B — FacturaConIVA persists with null CAE

- GIVEN a `Factura` created via `CrearFacturaConIVA(...)`
- WHEN persisted and reloaded
- THEN `TipoComprobante` is `FacturaConIVA`, `CAE` is null, `VencimientoCAE` is null

### Scenario 10-C — FacturaElectronica persists and accepts CAE assignment post-save

- GIVEN a `Factura` created via `CrearFacturaElectronica(...)` (CAE null at creation)
- WHEN persisted, reloaded, `factura.AsignarCAE("12345678901234", vencimiento)` is called, and saved again
- THEN the reloaded entity has `CAE = "12345678901234"` and `VencimientoCAE = vencimiento`

### Scenario 10-D — PedidosFacturados JSON column round-trips

- GIVEN a `Factura` that references 3 `PedidoId` GUIDs in `PedidosFacturados`
- WHEN persisted and reloaded
- THEN `PedidosFacturados` contains exactly the same 3 GUIDs in the same order

---

## REQ-11 — CrearFactura use case enforces REQ-13/13-G multi-client rejection

The `CrearFactura` application use case MUST load each referenced `Pedido` and validate that all `ClienteId` values match the requested `clienteId`. A mismatch MUST cause the use case to throw `ConflictException` before calling any `Factura` factory method.

### Scenario 11-A — Same-client Pedidos are accepted

- GIVEN two `Pedido` entities both with `ClienteId = A`
- WHEN `CrearFactura` is called with `clienteId = A` and both `PedidoId` values
- THEN no exception is thrown
- AND a `Factura` is created and persisted referencing both Pedidos

### Scenario 11-B — Mixed-client Pedidos are rejected

- GIVEN `PedidoA` with `ClienteId = A` and `PedidoB` with `ClienteId = B` (different client)
- WHEN `CrearFactura` is called with `clienteId = A` and both `PedidoId` values
- THEN a `ConflictException` is thrown
- AND no `Factura` is created or persisted

### Scenario 11-C — Non-existent Pedido is rejected

- GIVEN a `PedidoId` that does not exist in the database
- WHEN `CrearFactura` is called referencing that `PedidoId`
- THEN a `ConflictException` is thrown before any `Factura` factory is called

---

## REQ-12 — Repository and UnitOfWork contracts

Per-aggregate repository implementations MUST return fully tracked aggregate roots with their complete owned-entity graphs loaded. `IUnitOfWork.SaveChangesAsync` MUST commit all pending changes atomically within a single database transaction.

### Scenario 12-A — Repository loads full owned graph

- GIVEN a `Pedido` with 3 `LineaPedido` entries and 2 `OrdenTrabajo` entries saved to the database
- WHEN `IPedidoRepository.GetByIdAsync(pedidoId)` is called
- THEN the returned `Pedido` is non-null
- AND `Lineas.Count` is 3 and `OrdenesTrabajo.Count` is 2 (no lazy-loading required from caller)

### Scenario 12-B — UnitOfWork commits atomically

- GIVEN two repository operations within the same `IUnitOfWork` scope
- WHEN `IUnitOfWork.SaveChangesAsync` is called
- THEN both changes are committed in the same database transaction
- AND if one operation causes a DB exception, neither change is persisted

---

## REQ-13 — Integration tests run against LocalDB

All Phase 3 integration tests MUST run against SQL Server LocalDB (`(localdb)\mssqllocaldb`) via `dotnet test`. No Docker or Testcontainers dependency is required. The test project MUST apply migrations before each test run.

### Scenario 13-A — Integration test suite passes on dotnet test

- GIVEN SQL Server LocalDB is installed
- WHEN `dotnet test tests/GastroGestion.Infrastructure.Tests/` is executed
- THEN the command exits with code 0
- AND all integration tests are reported as passed

### Scenario 13-B — Tests apply migrations before execution

- GIVEN the test project is configured to call `dbContext.Database.MigrateAsync()` before test execution
- WHEN a test run starts against a clean LocalDB
- THEN the schema is created automatically without requiring a manual `dotnet ef database update`

---

## Modified Domain Capability (Phase 3 only change to Domain)

### LineaPedido exposes `internal bool PrecioConfirmado`

**Modification to Domain layer:** `LineaPedido` exposes an `internal bool PrecioConfirmado { get; private set; }` property backed by the existing `_precioConfirmado` field. This is the ONLY domain change in Phase 3. No framework dependency is introduced; this is a plain CLR property used by Infrastructure's EF configuration to map the set-once flag as a persisted column.

(Previously: `_precioConfirmado` was a private field with no exposure; it was not persisted and would reset to `false` on reload, breaking the set-once invariant.)

**Implementation note (Verify-Slice-B finding):** In `PedidoConfiguration`, `PrecioConfirmado` is mapped via string-based `Property<bool>("PrecioConfirmado")`. EF Core 8 resolves this name to the real `internal bool PrecioConfirmado` CLR property via reflection — it does NOT create a shadow property. The `internal` visibility is accessible to EF's reflection at runtime. Confirmed by the round-trip test: reload → second `ConfirmarPrecio` throws `DomainException`.

---

## Requirement cross-reference

| REQ | Slice | Domain area | Closes |
|-----|-------|-------------|--------|
| REQ-01 | A | Migrations | — |
| REQ-02 | A | Domain zero-deps gate | Domain REQ-01 (gate maintained) |
| REQ-03 | A | Catalogue aggregates | Domain REQ-03/04/05/06/14 |
| REQ-04 | A | Value object converters + owned types | Domain REQ-02 |
| REQ-05 | B | PrecioConfirmado persistence | Domain REQ-08 (set-once) |
| REQ-06 | B | Pedido + owned graphs | Domain REQ-07/08/10 |
| REQ-07 | B | Append-only ledger guard | Domain REQ-12 |
| REQ-08 | B | Domain event dispatch | Domain REQ-15 |
| REQ-09 | B | RowVersion concurrency | Domain REQ-07/14 |
| REQ-10 | C | Factura flat table + discriminator | Domain REQ-13 |
| REQ-11 | C | CrearFactura multi-client guard | Domain REQ-13-G (closes deferred item) |
| REQ-12 | A/B/C | Repository + UoW contracts | All aggregates |
| REQ-13 | C | LocalDB integration tests | All above |

---

## Known Open Items / Phase-4 Follow-ups

### W-01 — EfectivoPrecioService sync-over-async (MUST FIX before Phase 4 wiring)

**Issue:** `EfectivoPrecioService` (in `GastroGestion.Application/Services/`) implements the synchronous `IEfectivoPrecioService` domain interface but calls async repository methods (`IMenuRepository.GetActivosByFechaAsync`, `IPlatoRepository.GetByIdAsync`) using `.GetAwaiter().GetResult()` — a sync-over-async pattern.

**Current risk (Phase 3):** LOW — `EfectivoPrecioService` is registered in DI but is NOT injected into `CrearFacturaHandler`. It is wired but unused in Phase 3. No deadlock risk exists in the current Phase 3 call paths.

**Future risk (Phase 4+):** HIGH — if called on a synchronous ASP.NET Core thread with a saturated thread pool, the blocking `.GetAwaiter().GetResult()` will deadlock. This MUST be resolved before Phase 4 connects it to a real call path.

**Resolution options (choose one in Phase 4):**
- Make `IEfectivoPrecioService` async: `Task<(Dinero Precio, PorcentajeIVA IVA)> ObtenerPrecioEfectivoAsync(...)`. Requires updating the domain interface (adds a dependency direction concern — evaluate if it belongs in Application instead) and all callers.
- Pre-load data in the async handler before calling a sync service: handler calls repos async, passes data into a now-pure-sync `EfectivoPrecioService` that receives pre-loaded objects.

---

### Accepted EF mechanism deviations (informational — no action required)

These are implementation deviations from the design doc's original API choice. Storage outcome is identical; they become a concern only if SQL-level querying of these columns is ever required.

| Item | Design intent | Actual implementation | Impact |
|---|---|---|---|
| `RecetaSnapshot` (on `OrdenTrabajo`) | `OwnsMany(...).ToJson()` (EF8 native JSON) | `ValueConverter<IReadOnlyList<LineaRecetaSnapshot>, string>` → `nvarchar(max)` | EF8 cannot bind positional `sealed record` constructors to owned navigation JSON. Storage outcome identical (JSON in nvarchar(max)). D6 intent preserved: audit snapshot, never SQL-filtered. |
| `Factura.PedidosFacturados` | `PrimitiveCollection<T>().ToJson()` | `ValueConverter<IReadOnlyList<Guid>, string>` → `nvarchar(max)` | `PrimitiveCollection<T>.ToJson()` does not exist as a valid EF8 fluent API chain. Storage outcome identical. D5 intent preserved: never SQL-filtered. |

### Additional informational notes (Verify-Slice-B/C findings)

- `LineaPedido.PrecioConfirmado` mapped via string-based `Property<bool>("PrecioConfirmado")` — binds to the real internal CLR property (not a shadow property). Verified.
- `IMenuRepository.GetActivosByFechaAsync` — added to the port interface to support `EfectivoPrecioService`. May belong on a separate read-side query interface in Phase 4 (Phase 4 concern).
- `InternalsVisibleTo` (`GastroGestion.Infrastructure` → `GastroGestion.Infrastructure.Tests`) — added to allow integration tests to instantiate internal repository classes directly. Standard .NET test pattern. May be moved to a `TestHelpers` project if the solution grows.
- `AddFactura` migration is empty — Factura tables were created in `InitialCatalogue` (Slice A speculative mapping). Schema is correct; verified by round-trip tests.

---

## Delivery summary

| PR | Slice | Tasks | Merged | Status |
|---|---|---|---|---|
| PR #6 | Slice A — Foundation | PE-01..PE-11 | main | Merged |
| PR #7 | Slice B — Transactional | PE-12..PE-19 | main | Merged |
| PR #8 | Slice C — Fiscal | PE-20..PE-26 | main | Merged |

**Chain strategy:** stacked-to-main (each slice merged to main in order).
**Main HEAD after merge:** de56348

---

## PHASE-6 Additions — OrdenTrabajo Workflow

### REQ-14 — `IPedidoRepository` flat board projection

`IPedidoRepository` exposes an additional method:

```csharp
Task<IReadOnlyList<OrdenTrabajoBoardItem>> GetAllOrdenesTrabajoAsync(
    EstadoOT? estado, CancellationToken ct = default);
```

The EF Core implementation uses a `SelectMany` projection off the owned `PedidoOrdenesTrabajo` set without loading full `Pedido` aggregates. When `estado` is non-null, only OTs with that `EstadoOT` are returned. When `estado` is null, all non-`Cancelada` OTs are returned.

The projection NEVER loads `RecetaSnapshot` (JSON column) — the board does not need it.

#### Scenario 14-A — Board projection returns flat items without loading Pedido aggregates

- GIVEN multiple `OrdenesTrabajo` across multiple `Pedidos`
- WHEN `GetAllOrdenesTrabajoAsync(EstadoOT.Creada, ct)` is called
- THEN the returned list contains only `OrdenTrabajoBoardItem` records with `Estado = Creada`
- AND no full `Pedido` aggregate is materialized in memory

#### Scenario 14-B — Null estado returns all non-Cancelada OTs

- GIVEN OTs in states `Creada`, `Preparandose`, `Lista`, and `Cancelada`
- WHEN `GetAllOrdenesTrabajoAsync(null, ct)` is called
- THEN the result excludes all `Cancelada` OTs and includes all others

---

### REQ-15 — `IPlatoRepository` batch load

`IPlatoRepository` exposes an additional method:

```csharp
Task<IReadOnlyList<Plato>> GetByIdsAsync(
    IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
```

The implementation uses a single `WHERE Id IN (...)` query. This method is used by `GenerarOrdenesTrabajoHandler` to resolve recipe snapshots for all distinct `PlatoId` values in a `Pedido` without N+1 queries.

---

---

## CATALOG-CRUD-AND-COCINEROS Additions — Repository Ports and EF Implementations

> **Change:** `catalog-crud-and-cocineros` — merged 2026-06-17 via 3 stacked PRs (#19, #20, #21).

### REQ-16 — `IUsuarioRepository.GetByRolAsync`

`IUsuarioRepository` (in `GastroGestion.Application/Abstractions/Persistence/`) exposes one additional method:

```csharp
Task<IReadOnlyList<Usuario>> GetByRolAsync(RolUsuario rol, CancellationToken ct = default);
```

The EF Core implementation (`UsuarioRepository`) filters `Where(u => u.Rol == rol && u.Activo)` at the database layer. Inactive users with the given role are excluded. The result is returned as a read-only list.

#### Scenario 16-A — GetByRolAsync returns only active cocineros

- GIVEN multiple `Usuario` rows with `Rol == Cocinero`, some `Activo == true`, some `Activo == false`
- WHEN `GetByRolAsync(RolUsuario.Cocinero)` is called
- THEN only rows with `Activo == true` are returned
- AND rows with other roles are excluded

---

### REQ-17 — `IClienteRepository` search and uniqueness methods

`IClienteRepository` exposes two additional methods:

```csharp
Task<IReadOnlyList<Cliente>> SearchAsync(string? nombre, bool incluirInactivos, CancellationToken ct = default);
Task<bool> CuitExistsForOtherAsync(string cuit, Guid excludeId, CancellationToken ct = default);
```

`GetAllAsync` is left intact and unchanged. Default-active behavior lives in the new `SearchAsync`; the new `GET /clientes` endpoint calls `SearchAsync`, not `GetAllAsync`.

`SearchAsync` implementation:
- When `!incluirInactivos`: filters `Where(c => c.Activo)`.
- When `nombre` is non-empty: filters `Where(c => EF.Functions.Like(c.Nombre, $"%{nombre}%"))` (case-insensitive on SQL Server default `CI_AS` collation).
- When both are null/false: returns all active clientes (equivalent to `GetAllAsync` for active data only — seeded-list tests remain green).

`CuitExistsForOtherAsync` uses `AnyAsync` with parameterized SQL, excluding the current record by id to allow a cliente to retain its own CUIT on update.

#### Scenario 17-A — SearchAsync hides inactive by default

- GIVEN active and inactive clientes exist
- WHEN `SearchAsync(null, false)` is called
- THEN only active clientes are returned

#### Scenario 17-B — SearchAsync applies case-insensitive partial nombre filter

- GIVEN clientes named "García SA" and "López SRL" (both active)
- WHEN `SearchAsync("garc", false)` is called
- THEN only "García SA" is returned

#### Scenario 17-C — CuitExistsForOtherAsync semantics

- GIVEN cliente A holds CUIT "20345678901" and cliente B holds a different CUIT
- WHEN `CuitExistsForOtherAsync("20345678901", clienteBId)` is called
- THEN returns `true` (another cliente holds that CUIT)
- WHEN `CuitExistsForOtherAsync("20345678901", clienteAId)` is called
- THEN returns `false` (same cliente — not a conflict)

---

### REQ-18 — `IIngredienteRepository` search and uniqueness methods

`IIngredienteRepository` exposes two additional methods:

```csharp
Task<IReadOnlyList<Ingrediente>> SearchAsync(string? nombre, bool incluirInactivos, CancellationToken ct = default);
Task<bool> NombreExistsForOtherAsync(string nombre, Guid excludeId, CancellationToken ct = default);
```

`GetAllAsync` is left intact. `SearchAsync` mirrors the shape of `ClienteRepository.SearchAsync`. `NombreExistsForOtherAsync` uses `AnyAsync(i => i.Id != excludeId && i.Nombre == nombre)` — plain string equality (SQL Server collation handles case-insensitivity for exact-match uniqueness check).

#### Scenario 18-A — SearchAsync default-active behavior

- GIVEN active and inactive ingredientes
- WHEN `SearchAsync(null, false)` is called
- THEN only active ingredientes are returned (seeded-list integration test remains green)

#### Scenario 18-B — NombreExistsForOtherAsync semantics

- GIVEN ingrediente A has `Nombre = "Harina"`
- WHEN `NombreExistsForOtherAsync("Harina", ingredienteBId)` is called
- THEN returns `true` (another ingrediente holds that name)
- WHEN `NombreExistsForOtherAsync("Harina", ingredienteAId)` is called
- THEN returns `false` (same ingrediente — not a conflict)

---

## Archive Info

**Archived:** 2026-06-14 (Phase 3 of 7 complete)
**Change folder archive:** `openspec/changes/archive/2026-06-14-persistence-ef-core/` (proposal, spec, design, tasks)
**Verify reports:** engram `sdd/persistence-ef-core/verify-report-slice-a` (#53), `-slice-b` (#55), `-slice-c` (#57)
**Phase 6 infrastructure additions:** REQ-14 and REQ-15 added — `GetAllOrdenesTrabajoAsync` flat projection on `IPedidoRepository`, `GetByIdsAsync` on `IPlatoRepository`. See `openspec/changes/archive/2026-06-17-ordentrabajo-workflow/` for full design.
**catalog-crud-and-cocineros infrastructure additions (2026-06-17):** REQ-16, REQ-17, REQ-18 added — `IUsuarioRepository.GetByRolAsync`, `IClienteRepository.SearchAsync + CuitExistsForOtherAsync`, `IIngredienteRepository.SearchAsync + NombreExistsForOtherAsync`. `GetAllAsync` left intact on all repos. See `openspec/changes/archive/2026-06-17-catalog-crud-and-cocineros/` for full spec, design, and tasks.
**Next phase:** Phase 7 — Blazor frontend
