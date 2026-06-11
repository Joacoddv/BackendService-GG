# GastroGestion — Functional Scope

> **Status:** Living product scope for the .NET 8 modernization.
> **Source of truth:** Refined from *"Sistema de Gestión Gastronómica"* (Seminario de Aplicación Profesional, J. Díaz de Vivar, 2022), Section 9. Business-plan filler from the original is intentionally dropped.
> **Relationship to the roadmap:** This document defines *what the system does*. The 7-phase strangler roadmap (see `openspec/`) defines *the order in which we build it*.

## How to read this document

Each module is described in plain product language, then annotated:

| Mark | Meaning |
|------|---------|
| ✓ | Behavior carried over from the original spec (the baseline contract). |
| ★ | Improvement proposed with senior judgment — why the original was weak and what to do instead. |
| ⚠ | Genuine product/design decision still **open** — needs a human call before it can be built. |

Domain terms stay in their canonical Spanish (the ubiquitous language the code will use): `Plato`, `Pedido`, `Orden de Trabajo` (OT), `Mostrador`, `Bitácora`, etc.

---

## Ubiquitous language (glossary)

| Term | Meaning |
|------|---------|
| **Plato** | A sellable dish. Has a recipe (`PlatoIngrediente` lines) and a base price. |
| **Ingrediente** | A raw material / stock item with a unit of measure (`Medida`). |
| **Menú** | A date-bound assignment of one or more `Plato`s to a day, optionally with an overridden price. |
| **Pedido** | A customer order. Two flavors: `Mostrador`/`Delivery` (counter) and `Salón` (dine-in). |
| **Orden de Trabajo (OT)** | A kitchen production order, one per dish line of a `Pedido`. Drives stock deduction. |
| **Factura** | An invoice grouping one or more delivered/closed `Pedido`s for a single client. |
| **Stock** | Current quantity per `Ingrediente`, plus the ledger of movements. |
| **Mesa** | A dine-in table. Holds at most one open `Salón` order. |
| **Bitácora** | The audit log: who did what, when. |
| **Legajo** | Employee file number — the user identity used across the system. |

---

## Roles & permissions

Seven roles. The original scattered permissions across every use case; consolidated here as one matrix (★ — single source beats per-screen prose).

| Role | Responsibility |
|------|----------------|
| **Gerente** | Full access. The only role that reads the Bitácora. |
| **ATC** (Atención al Cliente) | Clients, orders, order-state changes. |
| **Ventas** | Clients, dishes, menu, orders. |
| **Finanzas** | Invoicing, payments; read access to clients/orders. |
| **Producción** | Dishes (create/modify), OT state, stock capacity. |
| **Almacenes** | Stock movements and capacity. |
| **Repartidor** | Order-state updates relevant to delivery (e.g. `Entregado`). |

★ **Improvement — model permissions as policies, not role string checks.** The original hard-codes role names inside each use case ("solo Gerente y Ventas"). In the rebuild, express authorization as named policies (e.g. `CanManageDishes`, `CanInvoice`) mapped to roles in one place. Role membership changes then never touch business code. This also makes the future multi-tenant/branch story tractable.

⚠ **Open decision — login identity.** The original keys users by `Legajo`. Confirm whether modern auth uses `Legajo` as the login, or email (the user's address is `@disbyte.com`), with `Legajo` as an internal attribute. Affects the JWT `sub` claim and the users table.

---

## 1. Clients (`Cliente`) & Companies (`Empresa`)

✓ **Baseline.** Register customers (individuals, optionally tied to a company). Fields: auto `ClientNumber`, Name (required), Surname, DNI type + number, Email, Phone (required), Address, Zone. Soft-delete (deactivate, never physically remove). Modify any field except the auto number. List (default descending) and search by company / DNI / name / surname / address / zone.

✓ A `Cliente` may belong to an `Empresa` (RazónSocial, CUIL, contact data).

★ **Improvement — a client owns many addresses, not one.** Delivery validation (module 5) needs an address with a zone; a real customer has several (home, office). Model `Cliente 1—N Direccion`, each with its own `Zona`, and let the order pick one. The original conflated "the client's address" with "the delivery address."

★ **Improvement — validate contact data at the contract boundary.** Email format and phone normalization belong in the `Contracts`/Application validation layer (FluentValidation), not as free text. Cheap, prevents garbage that later breaks delivery and invoicing.

⚠ **Open decision — DNI vs CUIT uniqueness.** Should DNI (or CUIT for companies) be unique? The original allows duplicates. For invoicing in Argentina you likely want tax-id uniqueness per client type. Decide before the persistence phase.

---

## 2. Ingredients (`Ingrediente`)

✓ **Baseline.** Create with Name + `Medida` (unit of measure), unique by name. Soft-delete. Modify (not the auto number). List/search by name or unit.

★ **Improvement — `Medida` is an enum/lookup, not free text.** "kg", "kilo", "Kg" must not be three units. Make `UnidadDeMedida` a controlled vocabulary. Recipe math (module 7) depends on consistent units.

⚠ **Open decision — does an ingredient carry expiry/lot data?** The original mentions "near-expiry menu suggestions" (see §11) but never models expiry. If that feature is ever in scope, an `Ingrediente` alone is not enough — you need stock *lots* with expiry dates. Decide now whether to design the stock ledger lot-aware from day one (cheap to design early, expensive to retrofit).

---

## 3. Dishes (`Plato`) & Recipes (`PlatoIngrediente`)

✓ **Baseline.** Create with Name, base Price, and N ingredient lines each with a quantity; unique by name; every referenced ingredient must exist. Soft-delete. Modify (not the auto number). List/search by name / ingredients / price.

★ **Improvement — version the recipe, snapshot the price.** Two distinct concerns the original blurs:
- **Recipe changes over time.** If a `Plato`'s recipe changes, past stock deductions must still reflect the recipe *as it was* when the OT was created. Either keep recipe history or snapshot the recipe into the OT at creation.
- **Price is a snapshot, always.** A `Pedido` line must capture the effective unit price *at order time*, never read the live `Plato.Price` later. This is what makes invoicing correct months later. (Also see §4 menu-price override and §6 invoicing.)

⚠ **Open decision — sub-recipes / combos.** Does a `Plato` ever contain another `Plato` (a combo/menu ejecutivo)? The original says no. If "yes" is even plausible, the recipe model should allow a line to reference either an `Ingrediente` or a `Plato`. Decide before locking the domain.

---

## 4. Menu (`Menú`)

✓ **Baseline.** A menu entry binds a future date to one or more `Plato`s. A plate on the daily menu may carry a **different price** than its base. Modify/disable entries. List/search by date or plate name.

★ **Improvement — the menu is a price-and-availability rule, not a copy of the dish.** Model a `MenuItem` as `{ Date, PlatoId, OverridePrice? }`. When an order is taken for that date, the effective price resolution is: *menu override for the date → else base `Plato` price*. Resolve once, snapshot onto the order line. This kills the price-propagation bug class entirely.

⚠ **Open decision — what does "off-menu" mean?** Can a customer order a `Plato` that is **not** on today's menu? The original is silent. Two products:
- **Strict menu:** only today's menu plates are orderable. Simpler, matches a fixed daily-menu restaurant.
- **Catalogue + menu:** the full catalogue is always orderable; the menu just sets daily specials/prices.

This single decision changes order entry (module 5) materially. **Needs an answer early.**

---

## 5. Orders (`Pedido`)

The richest module. Two order types with **divergent state machines** sharing one concept.

✓ **Mostrador / Delivery states:** `Creado → Modificado → Preparándose → Listo para Entregar → Entregado | Cancelado`.
✓ **Salón states:** `Abierto → Cerrado | Cancelado`.

✓ **Baseline behavior.**
- Counter/Delivery: pick (or create) a client; choose `Mostrador` or `Delivery`; for delivery, validate the address against the delivery zone; add plates + observations. Only **producible** plates are shown, quantities capped by stock.
- Salón: pick a free `Mesa` from the floor view; add plates + observations.
- Modify a line only while it has **no OT**, or its OT is still `Creada`.
- Cancel cascades to all OTs; stock is restored only for `Creada` OTs (see §10).
- Closing/delivering (`Entregado` / `Cerrado`) makes the order invoiceable.
- A comprobante (receipt) is generated for the client on creation.

★ **Improvement — model the two types as one aggregate with a strategy, not two parallel `if`s.** A `Pedido` has a `Tipo` and a state that is validated by a type-specific transition table. Implement the transition rules as data (a `(Tipo, FromState, ToState, AllowedRoles)` table) rather than branching code. New transitions become data edits; illegal transitions are rejected in one place. This is the difference between maintainable and a swamp.

★ **Improvement — the order is an aggregate; protect its invariants.** Stock reservation, line edit-locking, and state transitions all touch the same order. Treat `Pedido` (with its lines and OTs) as a consistency boundary so concurrent edits can't half-apply. This directly addresses the concurrency hazard the original ignored.

★ **Improvement — "only producible plates" must account for in-flight reservations, not just on-hand stock.** Two cashiers taking orders at once will both see the same on-hand quantity and oversell. The producible calculation must subtract stock already reserved by un-cancelled OTs. See §10 for the locking model.

⚠ **Open decision — does adding a plate to an order reserve stock immediately, or only at OT creation?** The original deducts stock at **OT creation**, which means two orders can both *promise* a dish that only one can produce, and the conflict only surfaces in the kitchen. Options:
- **Reserve at order line** (safer UX, no kitchen surprises, more locking).
- **Reserve at OT** (original behavior, simpler, oversell risk).

This is the central tradeoff of the whole order/stock subsystem. **Decide explicitly.**

---

## 6. Invoicing (`Factura`)

✓ **States:** `Creada → Pagada | Cancelada`.

✓ **Baseline.** Pick a client → system lists that client's `No Facturado` orders → select one or more → preview → confirm. One invoice can group several orders **of the same client**. Register payment (transfer / cash-on-delivery / card / cash) → invoice `Pagada`, all linked orders `Pagado`. Cancel only from `Creada` → orders revert to `No Facturado`. Each line: Plato, Unit Price (snapshot), Quantity, Amount. List/search by order / client / invoice number / status.

★ **Improvement — totals are computed from snapshotted line prices, never recomputed from the catalogue.** This is the payoff of §3/§4 price snapshotting. The invoice is a financial record; it must be reproducible byte-for-byte regardless of later catalogue changes.

⚠ **Open decision — taxes (IVA) and fiscal compliance.** The original has **no IVA, no CAE, no AFIP/ARCA integration** — it is an internal sales record, not a fiscal invoice. For a real Argentine restaurant this is a hard requirement. Decide the scope:
- Internal "comprobante" only (original), or
- Fiscal invoicing with IVA breakdown and AFIP/ARCA electronic invoicing.

If fiscal: it needs its own phase, tax categories on clients, and IVA rates on plates. **This is the single biggest scope question in the document.**

⚠ **Open decision — partial payments / multiple payment methods on one invoice.** The original assumes one payment, one method. Real hospitality splits bills. Decide whether `Factura` has many `Pago` records.

---

## 7. Work Orders / Kitchen (`Orden de Trabajo`, OT)

✓ **States:** `Creada → Preparándose → Listo | Cancelada`.

✓ **Baseline.**
- Generate OTs from a `Pedido`: one OT per dish line. Validate stock for **all** lines first; if any line fails, **no** OT is created (all-or-nothing). Reject duplicate OT for the same dish/order.
- On OT creation, decrement stock for every ingredient in the recipe.
- Assign to a cook → OT `Preparándose`; counter order → `Preparándose`.
- Cook finishes → OT `Listo`.
- When **all** OTs of a counter order are `Listo` → order auto-advances to `Listo para Entregar`.
- OTs are cancelled only via the parent order's cancel action.

★ **Improvement — the all-or-nothing OT batch is a transaction with stock reservation, full stop.** "Check all, then deduct all" must run inside one transaction that **locks** the affected stock rows, or two concurrent batches will both pass the check and oversell. The original describes the intent but not the mechanism. Specify: pessimistic row locks (or an atomic conditional decrement) on the involved `Stock` rows for the duration of the batch.

★ **Improvement — snapshot the recipe into the OT.** Tie this to §3: the OT records the exact ingredients/quantities it consumed, so cancellation/restoration and auditing are correct even if the `Plato` recipe later changes.

---

## 8. Stock

✓ **Baseline.** Manual ingress/egress per ingredient, never below zero. Automatic deduction on OT creation. Production-capacity calculation: on demand (how many of each plate can we make now) and live during order entry (cap quantities at producible).

★ **Improvement — stock is an append-only ledger, current quantity is a projection.** Don't store a mutable `CurrentQuantity` you overwrite. Store `StockMovement` rows (`+ purchase / − OT consumption / ± adjustment`, with reason, user, timestamp) and derive the balance. This gives you a free audit trail, correct concurrency (movements never lost), and makes restoration (§10) just another movement. The original's single mutable number is the root of every race condition in the spec.

★ **Improvement — the producible calculation belongs in one place and is the same for both callers.** "How many `Plato`s can I make?" is asked by the capacity report and by order entry. One pure function: `min over recipe ingredients of (available ÷ required)`, where *available = balance − reserved*. Reuse it; don't reimplement per screen.

⚠ **Open decision — units and conversions.** If recipes use grams but purchases are in kilos, the ledger needs unit normalization (tie to §2's `Medida` enum). Decide the canonical storage unit per ingredient.

---

## 9. Salon / Tables (`Mesa`)

✓ **Baseline.** Visual floor view; a table with an open order is blocked from new orders. Entry point for creating salón orders and advancing their state. No CRUD on tables in the original (tables appear pre-configured).

★ **Improvement — add minimal table administration.** A real venue rearranges its floor. A simple `Mesa` CRUD (number, capacity, active flag) and a notion of zones/sections costs little and avoids a DB-edit-by-hand operational pain. Keep it small; this is not table-reservation software.

⚠ **Open decision — table transfers / merges.** Move a party to another table, or merge two tables onto one bill? Common in hospitality, absent in the original. In or out?

---

## 10. The hard parts (cross-cutting concurrency & money)

These are the genuinely non-CRUD problems. Calling them out so they get the design attention they need *before* code.

1. **Stock reservation & overselling.** Resolved by: append-only ledger (§8) + reserve-at-OT-or-order decision (§5) + transactional all-or-nothing batch with row locks (§7). This is one coherent subsystem, not three features. **Design it as a unit.**
2. **State-conditional stock restoration on cancel (§5).** On cancel, walk each OT: `Creada` → post a compensating `+` movement; `Preparándose`/`Listo` → consumed, no restore. With a ledger, restoration is just another movement — clean and auditable.
3. **Price correctness over time (§3/§4/§6).** Snapshot effective price at order line creation. Invoices never recompute from the live catalogue.
4. **Dual order state machines (§5).** Transition rules as data, validated in one place, role-gated.
5. **Concurrency on `Mesa` and on `Pedido` (§5/§9).** One open order per table; concurrent edits to one order must not half-apply. Aggregate boundary + optimistic concurrency token.

---

## 11. Underspecified features in the original (specced properly here)

### 11.1 ★ Near-expiry → menu suggestion

The original sells this as a **differentiator** (§1.2, §1.7, §6.1.1) but writes **zero requirements**. To exist, it needs:
- **Data:** stock movements carry lot + expiry (ties to §2/§8). Without expiry on stock, this feature is impossible — that's why it must be decided at ledger-design time.
- **Algorithm:** rank `Plato`s by how much near-expiry ingredient mass they consume, weighted by producible quantity. A pure, testable scoring function.
- **Surface:** a "Sugerencias del día" view for Gerente/Producción when building the menu.

⚠ **Decision:** is this in scope at all? If yes, the stock ledger must be lot/expiry-aware from phase one. If no, drop expiry from the model and note it as future work. **Do not leave it ambiguous** — it silently dictates the stock schema.

### 11.2 ★ Third-party integrations (PedidosYa, Rappi, Glovo, MercadoPago)

Named as an "Ultra tier" feature (§1.2, §6.1.1) with no spec. Realistic shape:
- **Delivery platforms** push orders *in* → they map to `Pedido` (type `Delivery`) via an anti-corruption layer; each platform is one adapter behind a port. Keeps their messy payloads out of the domain.
- **MercadoPago** is a payment provider behind a `Factura` payment port.

⚠ **Decision:** out of scope for the core rebuild; revisit as a dedicated phase after the domain is stable. Design `Pedido` and `Factura` with ports so integrations attach later without surgery.

---

## 12. Cross-cutting requirements

| Area | Requirement | Note |
|------|-------------|------|
| **Soft-delete** | Clients, Ingredients, Plates, Menus are deactivated, never deleted. | ★ Use an `Active` flag + global query filter, not scattered `WHERE` clauses. |
| **Immutable IDs** | Auto-assigned numbers (client/ingredient/plate) never change. | Surrogate keys; never user-editable. |
| **Bitácora (audit)** | Every action records `Legajo`, timestamp, description. Logins (success + failure) logged. Only Gerente reads it. | ★ Implement as a cross-cutting concern (interceptor/middleware), not manual calls in every handler. |
| **Auth** | No action without an authenticated session. Passwords encrypted in transit (SSL) and hashed at rest. | ★ The legacy stored plaintext + a hardcoded JWT secret — both already remediated in the scaffold. Hash with a modern KDF (e.g. PBKDF2/Argon2). |
| **Exception handling** | All exceptions caught; system stays stable. | ★ Centralized exception middleware → consistent error contracts. |
| **Availability** | 99% uptime target. | Non-functional; informs hosting, not the domain. |
| **i18n** | At least two languages. | ★ Resource-based localization; design strings as keys from the start in the frontend. |
| **Backup** | Periodic DB backup. | Operational; out of app scope but note it. |

---

## 13. Mapping to the 7-phase roadmap

| Phase | Modules / concerns this scope feeds |
|-------|-------------------------------------|
| 1. Scaffold ✅ | (done) |
| 2. Domain port | Entities + invariants: `Cliente`/`Empresa`/`Direccion` (§1), `Ingrediente` (§2), `Plato`/`PlatoIngrediente` (§3), `Menú` (§4), `Pedido`/line + state machines (§5), `OT` (§7), `Factura` (§6), `Stock` ledger (§8), `Mesa` (§9). **Lock the open decisions in §4, §5, §6 before/within this phase.** |
| 3. Infrastructure / EF Core 8 | Stock ledger persistence + concurrency tokens (§8/§10), soft-delete filters (§12), price snapshots (§3). |
| 4. Application | Producible calculation (§8), OT batch transaction (§7/§10), cancellation restoration (§10), invoice grouping + totals (§6), authorization policies (§roles). |
| 5. API + Security | Auth, hashing, Bitácora middleware, exception middleware, role policies (§12). |
| 6. Stock / Orden de Trabajo | The hard concurrency subsystem hardened (§10); capacity reporting; (optional) near-expiry suggestion if §11.1 is in. |
| 7. Blazor frontend | Floor view (§9), stock-gated order entry (§5), all CRUD surfaces, i18n (§12). |

---

## Decision log — RESOLVED (2026-06-10)

All ten gating decisions are closed. These are now binding inputs to the phase-2 domain design.

- [x] **§6 Invoicing — polymorphic `Comprobante`.** The system emits **three** document types from a `Pedido`: internal ticket (no IVA), `Factura` with IVA (no AFIP), and electronic `Factura` with IVA + AFIP/ARCA (CAE). Full fiscal capability is **in scope**; internal tickets coexist. AFIP electronic integration is its own later phase, but the domain models the comprobante types now.
- [x] **§5 Stock reservation — at the order line.** Stock is reserved when a plate is added to a `Pedido`, not at OT creation. Reservation is released on cancel. Implemented via the append-only ledger (reservation movements).
- [x] **§4 Menu — catalogue + menu.** The full carta is always orderable; a `Menú` entry only sets daily specials and price overrides. Effective price = menu override for the date → else base `Plato` price, snapshotted at order-line time.
- [x] **§1 Client fiscal data — yes.** `Cliente` carries **Condición frente al IVA** (Responsable Inscripto / Monotributo / Consumidor Final / Exento) and **CUIT/CUIL**, with **CUIT unique** for fiscal clients. The client's condition drives which comprobante type they can receive.
- [x] **§6 Payments — multiple per invoice.** A `Factura` has N `Pago` (different amounts and methods; split-the-bill supported). The invoice becomes `Pagada` when the sum of payments covers the total.
- [x] **Auth — email login, `Legajo` internal.** Users log in with email; `Legajo` is an internal attribute for the Bitácora and HR. JWT `sub` = user id.
- [x] **§11.1 Near-expiry — ledger is lot/expiry-aware now; suggestion feature later.** The stock ledger models lot + expiry date from day one (cheap now, expensive to retrofit). The ranking algorithm and "Sugerencias del día" UI are a later phase.
- [x] **§3 Combos / sub-recipes — not in v1.** A recipe line references an `Ingrediente`. The model leaves a seam so a line *could* reference a `Plato` later, but it is not implemented now.
- [x] **§9 Table transfers / merges — not in v1.** Added later if needed.
- [x] **§11.2 Third-party integrations — out of core.** Design `Pedido` and `Factura` with ports (anti-corruption adapters) so PedidosYa/Rappi/Glovo/MercadoPago attach later without domain surgery.
