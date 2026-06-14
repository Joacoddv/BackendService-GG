# Persistence EF Core — Delta Spec

**Change:** `persistence-ef-core`
**Phase:** 3 of 7 — EF Core persistence layer
**Scope:** `GastroGestion.Infrastructure` (new), `GastroGestion.Application` (ports + 1 use case), `GastroGestion.Domain/LineaPedido` (minimal additive change), `tests/GastroGestion.Infrastructure.Tests` (new).
**Artifact store:** hybrid (openspec + engram).

This spec describes what MUST be true after Phase 3 is applied. It does NOT describe implementation mechanics.

**Locked decisions (not open questions):**
- Greenfield schema — no legacy data migration in Phase 3.
- `Factura.PedidosFacturados` → JSON column (EF Core 8 primitive collection).
- Local provider = SQL Server LocalDB; connection string is config-driven.
- Domain stays at zero outward dependencies — all EF config lives in Infrastructure.

---

## New Capabilities

### persistence-foundation (Slice A)

---

### REQ-01 — EF Core schema bootstraps on a fresh LocalDB

The system MUST produce a complete, correct greenfield schema by running EF Core code-first migrations against `(localdb)\mssqllocaldb`. The schema MUST represent all 8 aggregate roots. No legacy data migration is performed.

#### Scenario 01-A — Clean migration apply

- GIVEN a fresh `(localdb)\mssqllocaldb` instance with no GastroGestion database
- WHEN `dotnet ef database update` is executed against the Infrastructure project
- THEN the command exits with code 0
- AND the GastroGestion database contains tables for all 8 aggregate roots

#### Scenario 01-B — Idempotent re-apply

- GIVEN the GastroGestion database already exists at the current migration head
- WHEN `dotnet ef database update` is executed again
- THEN the command exits with code 0 and makes no structural changes

---

### REQ-02 — Domain project retains zero outward dependencies after Phase 3

`GastroGestion.Domain.csproj` MUST contain zero `<PackageReference>` and zero `<ProjectReference>` elements after all Phase 3 work is merged. All EF Core concern MUST reside exclusively in `GastroGestion.Infrastructure`.

#### Scenario 02-A — Domain .csproj has no package or project references

- GIVEN `GastroGestion.Domain.csproj` is inspected after Phase 3 is merged
- WHEN all `<PackageReference>` and `<ProjectReference>` elements are counted
- THEN the combined count is 0

#### Scenario 02-B — Domain compiles in isolation

- GIVEN the .NET 8 SDK is installed
- WHEN `dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj` is executed
- THEN the command exits with code 0 with no build errors

---

### REQ-03 — Catalogue aggregate round-trips (Cliente, Ingrediente, Plato, Menu, Mesa)

Each catalogue aggregate MUST persist and reload with all properties and owned-entity graphs intact. Value objects MUST survive the round-trip without losing their values or validation constraints.

#### Scenario 03-A — Cliente with Direcciones round-trip

- GIVEN a `Cliente` with `Nombre`, `Apellido`, `Email`, `Cuit`, `CondicionIVA`, and two `Direccion` entries
- WHEN the entity is saved via `IClienteRepository.AddAsync` + `IUnitOfWork.SaveChangesAsync`
- AND subsequently loaded by its `Id`
- THEN the reloaded `Cliente` has identical `Nombre`, `Apellido`, `Email.Value`, `Cuit.Value`, `CondicionIVA`
- AND `Direcciones` contains exactly 2 entries with the original values

#### Scenario 03-B — Plato with LineaReceta round-trip

- GIVEN a `Plato` with `PrecioBase` (Dinero), `AlicuotaIVA`, and 3 `LineaReceta` entries each with a `Cantidad`
- WHEN persisted and reloaded
- THEN `PrecioBase.Amount` and `PrecioBase.Currency` match the original
- AND `Receta` contains exactly 3 lines with correct `IngredienteId` and `Cantidad.Amount`

#### Scenario 03-C — Mesa round-trip

- GIVEN a `Mesa` with `NumeroMesa`, `Capacidad`, `Zona`, and `Activo = true`
- WHEN persisted and reloaded
- THEN all fields match and `ConcurrencyToken` (RowVersion) is non-empty

---

### REQ-04 — Value object converters round-trip correctly

Single-field value objects (`Cuit`, `Email`, `PorcentajeIVA`, `LegajoId`) MUST be stored via value converters and restore with the original normalized value. Multi-field value objects (`Dinero`, `Cantidad`, `DireccionEntrega`) MUST be stored as owned types with explicit column names and restore with all fields intact.

#### Scenario 04-A — Cuit and Email preserve normalized form

- GIVEN a `Cliente` with `Cuit = new Cuit("20-34567890-1")` and `Email = new Email("Test@EXAMPLE.com")`
- WHEN persisted and reloaded
- THEN `Cuit.Value` is `"20345678901"` and `Email.Value` is `"test@example.com"`

#### Scenario 04-B — Dinero OwnsOne survives nested paths

- GIVEN a `Factura` with a `FacturaLinea` carrying `PrecioUnitario = new Dinero(150m, "ARS")` and a nested `PorcentajeIVA`
- WHEN persisted and reloaded
- THEN `FacturaLinea.PrecioUnitario.Amount` is `150m` and currency is `"ARS"`
- AND no EF column name collision occurs (each nested VO path uses a distinct column name)

#### Scenario 04-C — Nullable DireccionEntrega owned type round-trips as null for non-Delivery Pedidos

- GIVEN a `Pedido` with `TipoPedido = Mostrador` (no delivery address)
- WHEN persisted and reloaded
- THEN `pedido.DireccionEntrega` is `null`

#### Scenario 04-D — DireccionEntrega round-trips for Delivery Pedidos

- GIVEN a `Pedido` with `TipoPedido = Delivery` and a non-null `DireccionEntrega` with all fields populated
- WHEN persisted and reloaded
- THEN `pedido.DireccionEntrega` is non-null and all address fields match the original

---

### persistence-transactional (Slice B)

---

### REQ-05 — LineaPedido set-once price invariant survives reload

`LineaPedido.PrecioConfirmado` (the `internal bool` flag backing the set-once price constraint) MUST be persisted and correctly restored. A reloaded line whose price was confirmed MUST still reject a second `ConfirmarPrecio` call.

#### Scenario 05-A — Confirmed line rejects re-confirmation after reload

- GIVEN a `LineaPedido` that has had `ConfirmarPrecio(precio, alicuota)` called
- AND the parent `Pedido` has been saved and reloaded from the database
- WHEN `lineaPedido.ConfirmarPrecio(...)` is called again on the reloaded instance
- THEN a `DomainException` is thrown (set-once invariant enforced post-reload)

#### Scenario 05-B — Unconfirmed line allows confirmation after reload

- GIVEN a `LineaPedido` with `PrecioConfirmado = false` that has been saved and reloaded
- WHEN `lineaPedido.ConfirmarPrecio(new Dinero(200m, "ARS"), new PorcentajeIVA(0.21m))` is called
- THEN no exception is thrown
- AND `PrecioUnitarioSnapshot` is `200m ARS` and `AlicuotaIVASnapshot` is `0.21`

---

### REQ-06 — Pedido with owned graphs round-trips completely

A `Pedido` with `LineaPedido` and `OrdenTrabajo` collections MUST persist and reload with all owned entities and their value objects intact.

#### Scenario 06-A — Pedido with lines and OTs round-trip

- GIVEN a `Pedido` with 2 `LineaPedido` entries and 1 `OrdenTrabajo` with a `LineaRecetaSnapshot` containing a nested `Cantidad` VO
- WHEN persisted and reloaded
- THEN `Lineas` contains 2 entries with correct `PlatoId` and `Cantidad`
- AND `OrdenesDeTrabajo` contains 1 entry whose `RecetaSnapshot` has the original ingredient amounts

#### Scenario 06-B — LineaRecetaSnapshot JSON survives later Plato recipe changes

- GIVEN an `OrdenTrabajo` created when a `Plato` had 2 recipe lines
- AND the `Plato` recipe is subsequently updated with a third line
- WHEN the `OrdenTrabajo` is reloaded from the database
- THEN `RecetaSnapshot` still contains exactly 2 entries (snapshot is frozen at OT creation time)

---

### REQ-07 — MovimientoStock is append-only at the persistence boundary

`IMovimientoStockRepository` MUST expose only `AddAsync`. The `SaveChangesAsync` override MUST reject any attempt to update or delete a persisted `MovimientoStock` row. Balance MUST be computed as `SUM(Cantidad)` for a given `IngredienteId`.

#### Scenario 07-A — Repository exposes no update or delete

- GIVEN the `IMovimientoStockRepository` interface definition
- WHEN its members are inspected
- THEN it exposes only `AddAsync` (and optionally read methods); no `Update`, `Remove`, or `Delete` members exist

#### Scenario 07-B — SaveChanges guard rejects modification of a ledger row

- GIVEN a `MovimientoStock` that has already been persisted
- WHEN application code retrieves it and attempts to modify any of its fields and calls `SaveChangesAsync`
- THEN `SaveChangesAsync` throws before committing (the guard fires on `EntityState.Modified` for `MovimientoStock`)

#### Scenario 07-C — SaveChanges guard rejects deletion of a ledger row

- GIVEN a persisted `MovimientoStock`
- WHEN application code marks it for deletion and calls `SaveChangesAsync`
- THEN `SaveChangesAsync` throws before committing (the guard fires on `EntityState.Deleted` for `MovimientoStock`)

#### Scenario 07-D — Balance projection is correct SUM

- GIVEN the following `MovimientoStock` entries for `IngredienteId = X`:
  - Compra: +20m
  - Reserva: -5m
  - Consumo: -5m
  - LiberacionReserva: +5m
- WHEN the repository's balance query for `IngredienteId = X` is executed
- THEN the result is `15m`

---

### REQ-08 — Domain events dispatch exactly once after SaveChanges and are cleared

The in-process `IDomainEventDispatcher` MUST dispatch all domain events from tracked aggregate roots exactly once after a successful `SaveChangesAsync` call. Events MUST be cleared from the root after dispatch. A failed `SaveChangesAsync` MUST NOT dispatch events.

#### Scenario 08-A — FacturaNecesitaCAE reaches its handler post-commit

- GIVEN a `Factura` created via `CrearFacturaElectronica` (which raises `FacturaNecesitaCAE`)
- WHEN `IUnitOfWork.SaveChangesAsync` completes successfully
- THEN the registered `FacturaNecesitaCAE` handler is invoked exactly once
- AND `factura.DomainEvents` is empty after dispatch

#### Scenario 08-B — Events not dispatched on SaveChanges failure

- GIVEN an operation that raises a domain event but causes a DB constraint violation during save
- WHEN `SaveChangesAsync` throws a database exception
- THEN no domain event handler is invoked

#### Scenario 08-C — Events cleared after successful dispatch

- GIVEN an aggregate root that raised events during an operation
- WHEN `SaveChangesAsync` completes successfully and events are dispatched
- THEN `aggregateRoot.DomainEvents` is empty (count = 0)

---

### REQ-09 — Optimistic concurrency via RowVersion on Pedido and Mesa

`Pedido` and `Mesa` MUST carry a `RowVersion` concurrency token. Concurrent updates to the same row MUST surface as `DbUpdateConcurrencyException`.

#### Scenario 09-A — Concurrent Pedido update throws DbUpdateConcurrencyException

- GIVEN a `Pedido` that is loaded into two separate EF Core tracking contexts (context A and context B)
- WHEN context A saves a state change first
- AND context B then attempts to save its own state change with the stale RowVersion
- THEN `SaveChangesAsync` on context B throws `DbUpdateConcurrencyException`

#### Scenario 09-B — RowVersion is non-empty after first save

- GIVEN a new `Pedido` or `Mesa` saved to the database
- WHEN the entity is reloaded
- THEN its `RowVersion` property is non-null and non-empty (`byte[]` with at least 1 byte)

---

### persistence-fiscal (Slice C)

---

### REQ-10 — Factura persists as a flat single table with discriminator column

`Factura` MUST be stored in a single `Facturas` table with a `TipoComprobante` discriminator column (int). `CAE` and `VencimientoCAE` MUST be nullable columns (null for non-electronic comprobantes). No EF Core inheritance hierarchy (`HasDiscriminator`) is used.

#### Scenario 10-A — TicketInterno persists with null CAE

- GIVEN a `Factura` created via `CrearTicket(...)`
- WHEN persisted and reloaded
- THEN `TipoComprobante` is `TicketInterno`, `CAE` is null, `VencimientoCAE` is null
- AND all `FacturaLinea` and `Pago` owned collections are present

#### Scenario 10-B — FacturaConIVA persists with null CAE

- GIVEN a `Factura` created via `CrearFacturaConIVA(...)`
- WHEN persisted and reloaded
- THEN `TipoComprobante` is `FacturaConIVA`, `CAE` is null, `VencimientoCAE` is null

#### Scenario 10-C — FacturaElectronica persists and accepts CAE assignment post-save

- GIVEN a `Factura` created via `CrearFacturaElectronica(...)` (CAE null at creation)
- WHEN persisted, reloaded, `factura.AsignarCAE("12345678901234", vencimiento)` is called, and saved again
- THEN the reloaded entity has `CAE = "12345678901234"` and `VencimientoCAE = vencimiento`

#### Scenario 10-D — PedidosFacturados JSON column round-trips

- GIVEN a `Factura` that references 3 `PedidoId` GUIDs in `PedidosFacturados`
- WHEN persisted and reloaded
- THEN `PedidosFacturados` contains exactly the same 3 GUIDs in the same order

---

### REQ-11 — CrearFactura use case enforces REQ-13/13-G multi-client rejection

The `CrearFactura` application use case MUST load each referenced `Pedido` and validate that all `ClienteId` values match the requested `clienteId`. A mismatch MUST cause the use case to throw `ConflictException` before calling any `Factura` factory method.

#### Scenario 11-A — Same-client Pedidos are accepted

- GIVEN two `Pedido` entities both with `ClienteId = A`
- WHEN `CrearFactura` is called with `clienteId = A` and both `PedidoId` values
- THEN no exception is thrown
- AND a `Factura` is created and persisted referencing both Pedidos

#### Scenario 11-B — Mixed-client Pedidos are rejected

- GIVEN `PedidoA` with `ClienteId = A` and `PedidoB` with `ClienteId = B` (different client)
- WHEN `CrearFactura` is called with `clienteId = A` and both `PedidoId` values
- THEN a `ConflictException` is thrown
- AND no `Factura` is created or persisted

#### Scenario 11-C — Non-existent Pedido is rejected

- GIVEN a `PedidoId` that does not exist in the database
- WHEN `CrearFactura` is called referencing that `PedidoId`
- THEN an appropriate not-found exception is thrown before any `Factura` factory is called

---

### REQ-12 — Repository and UnitOfWork contracts

Per-aggregate repository implementations MUST return fully tracked aggregate roots with their complete owned-entity graphs loaded. `IUnitOfWork.SaveChangesAsync` MUST commit all pending changes atomically within a single database transaction.

#### Scenario 12-A — Repository loads full owned graph

- GIVEN a `Pedido` with 3 `LineaPedido` entries and 2 `OrdenTrabajo` entries saved to the database
- WHEN `IPedidoRepository.GetByIdAsync(pedidoId)` is called
- THEN the returned `Pedido` is non-null
- AND `Lineas.Count` is 3 and `OrdenesDeTrabajo.Count` is 2 (no lazy-loading required from caller)

#### Scenario 12-B — UnitOfWork commits atomically

- GIVEN two repository operations (e.g. save a `Cliente` and update a `Pedido`) within the same `IUnitOfWork` scope
- WHEN `IUnitOfWork.SaveChangesAsync` is called
- THEN both changes are committed in the same database transaction
- AND if one operation causes a DB exception, neither change is persisted

---

### REQ-13 — Integration tests run against LocalDB

All Phase 3 integration tests MUST run against SQL Server LocalDB (`(localdb)\mssqllocaldb`) via `dotnet test`. No Docker or Testcontainers dependency is required. The test project MUST apply migrations before each test run.

#### Scenario 13-A — Integration test suite passes on dotnet test

- GIVEN SQL Server LocalDB is installed
- WHEN `dotnet test tests/GastroGestion.Infrastructure.Tests/` is executed
- THEN the command exits with code 0
- AND all integration tests are reported as passed

#### Scenario 13-B — Tests apply migrations before execution

- GIVEN the test project is configured to call `dbContext.Database.MigrateAsync()` before test execution
- WHEN a test run starts against a clean LocalDB
- THEN the schema is created automatically without requiring a manual `dotnet ef database update`

---

## Modified Capabilities

### REQ-08 in Domain spec — LineaPedido exposes `internal bool PrecioConfirmado`

**Modification to `Domain/spec.md` REQ-08:** `LineaPedido` exposes an `internal bool PrecioConfirmado { get; private set; }` property backed by the existing `_precioConfirmado` field. This is the ONLY domain change in Phase 3. No framework dependency is introduced; this is a plain CLR property used by Infrastructure's EF configuration to map the set-once flag as a persisted column.

(Previously: `_precioConfirmado` was a private field with no exposure; it was not persisted and would reset to `false` on reload, breaking the set-once invariant.)

#### Scenario 08-A — Price snapshot set-once (unchanged behavior)

- GIVEN a `LineaPedido` with `PrecioConfirmado = false`
- WHEN `lineaPedido.ConfirmarPrecio(new Dinero(200m, "ARS"), new PorcentajeIVA(0.21m))` is called
- THEN `PrecioUnitarioSnapshot` is `200m ARS` and `AlicuotaIVASnapshot` is `0.21`
- AND calling `ConfirmarPrecio` again throws a `DomainException`

#### Scenario 08-B — PrecioConfirmado is visible to Infrastructure without EF dependency on Domain

- GIVEN `LineaPedido.PrecioConfirmado` is `internal`
- WHEN `GastroGestion.Infrastructure` (a different assembly) accesses it via `[assembly: InternalsVisibleTo]` or mapping config
- THEN the property is accessible to the EF configuration without adding any package reference to the Domain project

---

## Non-goals (explicitly out of scope for Phase 3)

- Legacy data migration or ETL (deferred; greenfield schema only)
- Transactional outbox (deferred to AFIP phase)
- Materialized `StockBalance` read model (deferred to Phase 5+)
- Gap-free sequence generation
- Full Phase-4 CQRS/use-case layer (only `CrearFactura` lands here)
- Testcontainers / Docker / CI pipeline setup
- Azure SQL or cloud deployment (config-ready but not deployed)
- `IEfectivoPrecioService` and `ICalculadorFactura` implementations (deferred to Phase 4 unless Factura strictly requires them)

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
| Modified REQ-08 | B | LineaPedido PrecioConfirmado exposure | Domain REQ-08 |
