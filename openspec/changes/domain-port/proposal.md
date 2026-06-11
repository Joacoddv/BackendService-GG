# Port the GastroGestion domain to a pure .NET 8 Domain layer

Replace the legacy anemic `Dominio/` (.NET Framework 4.8, data-bag classes with no invariants, no value objects, and several architectural defects) with a **pure** `GastroGestion.Domain` library targeting `net8.0`, `Nullable=enable`, C# 12. The domain encodes the business rules of the restaurant — order lifecycles, stock reservation, price snapshotting, polymorphic comprobantes, money/IVA — as rich aggregates with enforced invariants, value objects, domain services, domain events, and a data-driven state-machine registry. It has **zero outward dependencies**: no EF Core, no ASP.NET, no AutoMapper, no infrastructure. Persistence and mapping arrive in phase 3.

## Why now

This is **phase 2 of 7** in the agreed strangler roadmap (scaffold ✅ → **domain port** → infrastructure/EF Core → application → API+security → stock/OT hardening → Blazor). Phase 1 produced an empty-but-runnable Clean Architecture solution; `src/GastroGestion.Domain/` exists but is empty. Nothing downstream — repositories, the producible calculation, the OT batch transaction, invoicing totals — can be built until the domain model and its invariants exist. The exploration (engram `sdd/domain-port/explore`, obs #34) inventoried 15 legacy entities, mapped aggregate boundaries, and identified five hard modeling problems. All ten gating product decisions in `docs/functional-scope.md` are closed, and the eight exploration open-questions are now resolved (see Key decisions). The domain is unblocked.

## What success looks like

- `dotnet build src/GastroGestion.Domain` succeeds on .NET 8; `dotnet test` passes the invariant unit suites.
- The Domain project references **no** outward package or project — verified by inspecting its `.csproj` (Clean Architecture dependency rule holds at the innermost layer).
- Every aggregate enforces its invariants in constructors/factory methods; illegal states are unrepresentable (no public setters that bypass rules).
- Ubiquitous language is preserved: domain type names stay in canonical Spanish (`Pedido`, `Plato`, `OrdenTrabajo`, `Comprobante`, `MovimientoStock`, `Mesa`).
- Strict TDD activates from this change onward: invariants and pure calculations (price snapshot, IVA breakdown, producible-from-ledger projection, state-transition validation) are covered by xUnit tests.

## Scope

### In scope (the full domain model, sliced — see Delivery strategy)

| Area | What lands |
|------|-----------|
| Aggregates — Catalogue | `Cliente` (+ owned `Direccion[]`), `Ingrediente`, `Plato` (+ owned `LineaReceta[]`), `Menú` (+ owned `MenuItem[]`), `Mesa` |
| Aggregates — Transactional | `Pedido` (root) owning `LineaPedido[]` and `OrdenTrabajo[]` |
| Aggregates — Fiscal | `Factura`/`Comprobante` (polymorphic) owning `FacturaLinea[]` and `Pago[]`; `MovimientoStock` (append-only ledger root) |
| Value objects | `Dinero`, `Cuit`, `Email`, `Cantidad`, `PorcentajeIVA`, `LegajoId`, and `Direccion` as a frozen delivery snapshot VO on `Pedido` |
| Enums | `TipoPedido`, `EstadoPedido` (merged), `EstadoOT`, `EstadoFactura`, `EstadoFacturaPedido`, `CondicionIVA`, `AlicuotaIVA`, `UnidadDeMedida`, `TipoComprobante`, `TipoMovimientoStock`, `MetodoPago`, `EstadoMesa`, `Rol` |
| Domain services | `IEfectivoPrecioService` (effective-price resolution), `ICalculadorFactura` (IVA breakdown/totals), `PedidoTransicionRegistry` (data-driven state machines) |
| Domain events | `PedidoCreado`, `LineaPedidoAgregada`, `OrdenTrabajoCreada`, `PedidoEstadoCambiado`, `FacturaNecesitaCAE` (AFIP port seam) |
| Invariant tests | xUnit unit tests for factories, state transitions, price snapshot immutability, IVA calculation, ledger balance projection |

### Out of scope (deferred)

- **Persistence** — EF Core `DbContext`, entity configurations, TPH discriminator mapping, owned-type config, concurrency-token mapping, soft-delete query filters, migrations (phase 3).
- **Application layer** — producible calculation, the all-or-nothing OT batch transaction, cancellation restoration orchestration, invoice-grouping use cases, authorization policies (phase 4).
- **API + auth implementation** — controllers, DTO mapping, JWT issuing, password hashing, `[Authorize]` policies (phase 5).
- **AFIP/ARCA integration code** — the CAE request/response and Punto-de-Venta sequencing live in the integration layer; the domain only **holds** the assigned `CAE`/`VencimientoCAE`/comprobante number and raises `FacturaNecesitaCAE`.
- **Gap-free sequence generation** for `NumeroCliente`/`NumeroPlato`/etc. — assigned in infrastructure (phase 3); the domain treats them as immutable assigned values.
- **Near-expiry suggestion algorithm** — the ledger is lot/expiry-aware now, but the ranking/scoring function and "Sugerencias del día" surface are a later phase.
- **Combos / sub-recipes**, **table transfers/merges**, **third-party integrations** (PedidosYa/Rappi/MercadoPago) — explicitly not in v1; seams left, no implementation.
- Removing `Id_Empresa`/`Id_Sucursal`, legacy `Stock` and `Plato_Precio` classes — these are simply **not ported** (multi-tenancy is an infrastructure/tenant-context concern; price history was a snapshotting workaround).

## Capabilities

> Contract with the spec phase. These map to slices, not 1:1 to spec files — the spec phase will decompose.

### New capabilities

- `domain-catalogue`: `Cliente`/`Direccion`, `Ingrediente`, `Plato`/`LineaReceta`, `Menú`/`MenuItem`, `Mesa`, plus all value objects and enums (Slice 1).
- `domain-pedido`: `Pedido` aggregate with `LineaPedido` + `OrdenTrabajo`, price-snapshot domain service, transition registry, order/OT domain events (Slice 2).
- `domain-fiscal`: polymorphic `Comprobante`/`Factura`, `Pago`, `MovimientoStock` ledger, IVA-calculation domain service, AFIP event seam (Slice 3).

### Modified capabilities

- None. The Domain project is empty; this is net-new domain code, not a behavior change to an existing spec.

## Approach

Model rich aggregates that protect their invariants; keep all calculation pure and stateless.

- **Polymorphic Comprobante via TPH discriminator in the domain.** One `Factura` aggregate root with a `TipoComprobante` enum and factory methods `Factura.CrearTicket(...)`, `Factura.CrearFacturaConIVA(...)`, `Factura.CrearFacturaElectronica(...)`. `CAE`/`VencimientoCAE` are nullable, valid only for `FacturaElectronica`; the "CAE required when electronic" invariant is enforced in the factory. Persistence TPH is phase 3.
- **Append-only `MovimientoStock` ledger.** Signed `Cantidad`, `TipoMovimientoStock` enum (`Compra`, `Consumo`, `Ajuste`, `Reserva`, `LiberacionReserva`, `DevolucionCancelacion`), nullable `Lote` + `FechaVencimiento` (lot/expiry-aware from day one). Balance and producibility are **projection queries**, never stored mutable state. Reservation happens at order-line add; release/consume/return are further movements. The check-then-reserve atomicity is an infrastructure concern; the domain only defines the `Available >= 0` invariant.
- **Price snapshot via domain service at line-add.** `IEfectivoPrecioService` resolves `menu override for date → else Plato.PrecioBase`, then writes `PrecioUnitarioSnapshot` + `AlicuotaIVASnapshot` onto `LineaPedido` **once** (set-once `ConfirmarPrecio`). Catalogue prices are never re-read after snapshot; `Factura` totals compute from line snapshots.
- **Data-driven dual state machines.** `PedidoTransicionRegistry` holds `(TipoPedido, Desde, Hasta, RolesPermitidos)` rows; `Pedido.TransicionarEstado(nuevo, rol)` validates against the registry, enforces the role gate, applies the change, and raises `PedidoEstadoCambiado`. One merged `EstadoPedido` enum; reachable states constrained by the registry per tipo. Adding a transition = adding a row, not branching code.
- **Money/IVA stays pure.** `Dinero`/`PorcentajeIVA` value objects; `LineaPedido` exposes computed `SubtotalLinea`/`IVALinea`/`TotalLinea` over immutable snapshots; `ICalculadorFactura` returns the per-aliquot breakdown as a pure function. `TicketInterno` IVA is zero.
- **Aggregate ownership.** `Pedido` owns `OrdenTrabajo` for v1 (the "all OTs Listo → advance Pedido" invariant is natural inside the boundary; saga cost avoided). `MovimientoStock` is its own root (movements inserted independently). `Direccion` is an owned entity under `Cliente` but a **frozen VO snapshot** on the `Pedido` at delivery-order creation.

## Affected areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/GastroGestion.Domain/` | New | All aggregates, VOs, enums, domain services, events, transition registry |
| `tests/GastroGestion.Domain.Tests/` | New | Invariant + pure-calculation unit suites |
| `Dominio/` (legacy net48) | Untouched | Referenced as source-of-truth during port; not modified, not deleted (retired in a later cleanup change) |
| `src/GastroGestion.Application/` | Untouched | Will consume the domain in phase 4; no changes here |

## Delivery strategy — 3 slices, chained/stacked PRs

The domain has ~12 aggregate roots and five hard modeling problems. A single PR would be unreviewable. Recommended split (from exploration §5):

- **Slice 1 — Catalogue.** `Cliente`/`Direccion`, `Ingrediente`, `Plato`/`LineaReceta`, `Menú`/`MenuItem`, `Mesa`, all VOs and enums. Purely structural, no state machines. ~8–10 files.
- **Slice 2 — Transactional.** `Pedido` + `LineaPedido` + `OrdenTrabajo`, `IEfectivoPrecioService`, `PedidoTransicionRegistry`, order/OT events. The richest slice. ~10–15 files. Depends on Slice 1 (uses `Dinero`, `Cantidad`, enums).
- **Slice 3 — Fiscal.** Polymorphic `Comprobante`/`Factura`, `Pago`, `MovimientoStock` ledger, `ICalculadorFactura`, AFIP event seam. Highest product risk (fiscal/AFIP); isolating it protects the order model from fiscal churn. Depends on Slices 1–2.

### Review Workload Forecast

- **Estimated changed lines:** ~1,400–2,000 across the full domain (entities + VOs + enums + services + events + invariant tests). Far above the 400-line single-PR budget.
- **400-line budget risk:** High.
- **Chained PRs recommended:** Yes — three stacked slices (Catalogue → Transactional → Fiscal), each independently reviewable and testable.
- **Decision needed before apply:** Yes. The orchestrator uses `ask-on-risk`: confirm chained/stacked PRs and pick a chain strategy (`stacked-to-main` vs `feature-branch-chain`) before `sdd-apply`. Slice 1+2 *could* be one PR for a small team that knows the domain; Slice 3 should stay separate.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Domain accidentally takes an EF/framework dependency (e.g. `[Timestamp]`, `Required` attributes) | Med | Concurrency token modeled as a plain `byte[]` rowversion property — no EF attribute. `.csproj` has zero package refs; review the Domain `.csproj` as a gate before merge. |
| Over-modeling deferred features (combos, table transfers, AFIP) | Med | Leave seams (recipe-line could later reference a `Plato`; ports for integrations) but implement nothing; non-goals are explicit above. |
| `Pedido` aggregate grows unwieldy with owned `OrdenTrabajo` | Low | Accept for v1 (natural invariant boundary); documented extraction path to a child aggregate if it bloats. |
| Reservation/ledger sign convention errors in the balance projection | Med | Pure projection function with dedicated unit tests covering reserve → consume → liberate → return sequences. |
| Spanish/English mixing in identifiers | Low | Convention: domain nouns stay Spanish (ubiquitous language); structural/code keywords and comments English. |
| Slices merged out of order break compilation (Slice 2 needs Slice 1 VOs) | Med | Strict slice dependency order enforced in the chain; Slice 1 lands first. |

## Rollback plan

The Domain project is currently empty and nothing references it yet. Rollback = revert the slice's commits / close the slice PR. No data migration, no API contract, no downstream consumer is affected — phases 3+ have not started. Each slice PR is independently revertible; reverting Slice 3 does not touch Slices 1–2.

## Dependencies

- Phase 1 scaffold (`net8-clean-architecture-foundation`) — done, on `main`. Provides `src/GastroGestion.Domain/` and `tests/GastroGestion.Domain.Tests/`.
- All ten functional-scope decisions and the eight exploration open-questions — resolved (see Key decisions).

## Key decisions (resolved)

| Decision | Resolution |
|----------|-----------|
| Delivery address on a delivery order | **Frozen snapshot (VO)** onto the `Pedido` at creation; later client address edits do not alter past orders. |
| `NumeroCliente`/`NumeroPlato` (external, printed) | Gap-free sequence, **assigned in infrastructure** (phase 3); domain holds immutable assigned value. |
| AFIP comprobante numbering (Punto de Venta + número + CAE) | Assigned by the **AFIP integration layer**; the domain stores the assigned values and raises `FacturaNecesitaCAE`. |
| `Mesa` lifecycle | **Soft-deleted** via an active flag. |
| Cook assignment on `OrdenTrabajo` | Optional reference to the assigned cook (`Legajo`/`LegajoId`). |
| TakeAway | **Sub-type of Mostrador** — same state machine, channel label only (no extra registry rows). |
| `Factura` line granularity | One `FacturaLinea` maps to **one `LineaPedido`** — preserves the per-dish price snapshot. |
| Concurrency token on `Pedido` and `Mesa` | **`byte[]` rowversion** modeled as a plain domain property — no EF dependency. |
| Comprobante polymorphism | TPH via domain discriminator + factory methods; CAE invariants in factories. |
| Stock | Append-only `MovimientoStock` ledger; legacy mutable `Stock` dropped. |
| Pricing | Snapshot at line-add via domain service; legacy `Plato_Precio` history dropped. |
| Multi-tenancy fields | `Id_Empresa`/`Id_Sucursal` not ported — infrastructure/tenant-context concern. |

## Success criteria

- [ ] `dotnet build src/GastroGestion.Domain` succeeds on .NET 8 (nullable enabled, C# 12).
- [ ] `GastroGestion.Domain.csproj` has **zero** package references and **zero** project references.
- [ ] `dotnet test tests/GastroGestion.Domain.Tests` passes; invariants (factories, state transitions, set-once price, IVA breakdown, ledger balance) are covered.
- [ ] All aggregates enforce invariants in constructors/factory methods; no invariant-bypassing public setters.
- [ ] Ubiquitous language preserved — domain type names in canonical Spanish.
- [ ] All resolved decisions above are reflected in the model.

## Next step

Proceed to `sdd-spec` and `sdd-design` (these can run in parallel). Spec captures Given/When/Then acceptance for each aggregate's invariants and state transitions; design locks the aggregate diagrams, the TPH/ledger/registry shapes, and the slice boundaries.
