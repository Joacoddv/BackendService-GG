# Technical Design — Domain Port (pure .NET 8 Domain layer)

This design locks the **exact domain model** for **phase 2 of 7** of the GastroGestion modernization: the contents of `src/GastroGestion.Domain/`. It decides the project layout, every aggregate boundary, the value-object set, the five hard modeling problems (polymorphic comprobante, stock ledger, price snapshot, dual state machines, money/IVA), the domain-event dispatch contract, the concurrency-token shape, and how all of it maps onto the three delivery slices. It implements zero persistence, zero application logic, zero framework dependency.

> Scope guard: this document is the **architecture lock** — the HOW at model level. The companion spec (`sdd/domain-port/spec`) holds the Given/When/Then acceptance for each invariant. EF Core mapping, the producible/OT-batch use cases, and API wiring are phases 3–5 and are explicitly NOT designed here. The single hardest line in the sand: **the Domain layer knows nothing about how it is stored or transported.**

---

## Quick path (what lands, in slice order)

1. **Slice 1 — Catalogue.** Value objects + enums + the five structural aggregates (`Cliente`, `Ingrediente`, `Plato`, `Menú`, `Mesa`). No state machines, no events. The foundation everything else compiles against.
2. **Slice 2 — Transactional.** `Pedido` aggregate (owning `LineaPedido` + `OrdenTrabajo`), `IEfectivoPrecioService`, `PedidoTransicionRegistry`, order/OT domain events. The richest slice. Depends on Slice 1.
3. **Slice 3 — Fiscal.** Polymorphic `Factura`/`Comprobante`, `Pago`, `MovimientoStock` ledger, `ICalculadorFactura`, the AFIP event seam. Highest product risk. Depends on Slices 1–2.
4. **Gate.** `GastroGestion.Domain.csproj` carries **zero** `PackageReference` and **zero** `ProjectReference` — reviewed on every slice PR.

---

## 1. Project layout inside `src/GastroGestion.Domain/`

**Decision — organize by building block first, then by aggregate.** A single flat namespace would not survive ~12 aggregates plus VOs, enums, services, and events. The two real candidates:

| Option | Shape | Verdict |
|--------|-------|---------|
| A — by building-block kind | `ValueObjects/`, `Aggregates/`, `Events/`, `Services/`, `Enums/`, `Common/` | Rejected as the *sole* axis — a flat `Aggregates/` with 12 roots plus their owned entities is a junk drawer; you cannot see the `Pedido` cluster at a glance. |
| B — by aggregate | `Cliente/`, `Pedido/`, `Factura/`, … each holding its root + owned entities + its own events | Rejected as the *sole* axis — shared kernel (VOs, enums, the event base, the result type) has no obvious home and gets duplicated or dumped in the first aggregate folder. |
| **C — hybrid (chosen)** | A shared **`Common/`** kernel + a **`ValueObjects/`** + **`Enums/`** band, then **one folder per aggregate** that co-locates the root, its owned entities, and its aggregate-specific events. Cross-cutting service contracts in **`Services/`**. | **Chosen.** Screaming-architecture: the folder tree names the business. Shared kernel is explicit; each aggregate is a self-contained, reviewable cluster that maps 1:1 to a slice. |

```text
src/GastroGestion.Domain/
├─ GastroGestion.Domain.csproj          (ZERO PackageReference, ZERO ProjectReference — the gate)
│
├─ Common/                              (shared kernel — Slice 1)
│  ├─ Entity.cs                         (abstract base: Id (Guid), equality by Id)
│  ├─ AggregateRoot.cs                  (Entity + domain-event buffer: RaiseEvent/DomainEvents/ClearEvents)
│  ├─ ValueObject.cs                    (abstract base: structural equality via GetEqualityComponents)
│  ├─ IDomainEvent.cs                   (marker: OccurredOnUtc)
│  └─ DomainException.cs                (thrown by invariant guards in ctors/factories)
│
├─ ValueObjects/                        (Slice 1)
│  ├─ Dinero.cs                         (decimal Monto + Moneda; arithmetic + IVA ops)
│  ├─ Moneda.cs                         (enum: ARS default — kept beside Dinero)
│  ├─ Cuit.cs                           (11-digit validated, check-digit)
│  ├─ Email.cs                          (format-validated)
│  ├─ Cantidad.cs                       (decimal Valor + UnidadDeMedida)
│  ├─ PorcentajeIVA.cs                  (closed-set aliquot wrapper over AlicuotaIVA)
│  └─ LegajoId.cs                       (typed employee file id)
│
├─ Enums/                               (Slice 1 — closed vocabularies, replace legacy lookup tables)
│  ├─ TipoPedido.cs  EstadoPedido.cs  EstadoOT.cs
│  ├─ EstadoFactura.cs  EstadoFacturaPedido.cs  TipoComprobante.cs
│  ├─ CondicionIVA.cs  AlicuotaIVA.cs  UnidadDeMedida.cs
│  ├─ TipoMovimientoStock.cs  MetodoPago.cs
│  └─ EstadoMesa.cs  Rol.cs
│
├─ Clientes/                            (Slice 1)
│  ├─ Cliente.cs                        (root)
│  └─ Direccion.cs                      (owned entity; also reused as a VO snapshot type — see §3, §4)
├─ Ingredientes/                        (Slice 1)
│  └─ Ingrediente.cs                    (root)
├─ Platos/                              (Slice 1)
│  ├─ Plato.cs                          (root)
│  └─ LineaReceta.cs                    (owned entity)
├─ Menus/                               (Slice 1)
│  ├─ Menu.cs                           (root)
│  └─ MenuItem.cs                       (owned entity)
├─ Mesas/                               (Slice 1)
│  └─ Mesa.cs                           (root)
│
├─ Pedidos/                             (Slice 2)
│  ├─ Pedido.cs                         (root — owns lines + OTs, holds the state machine entry point)
│  ├─ LineaPedido.cs                    (owned entity — set-once price snapshot)
│  ├─ OrdenTrabajo.cs                   (owned entity — kitchen state, recipe snapshot)
│  ├─ LineaRecetaSnapshot.cs            (frozen recipe line captured into an OT)
│  ├─ DireccionEntrega.cs              (frozen delivery-address VO on Pedido — see §3/§4)
│  ├─ PedidoTransicionRegistry.cs       (data-driven transition table — see §6)
│  └─ Events/
│     ├─ PedidoCreado.cs  LineaPedidoAgregada.cs
│     ├─ OrdenTrabajoCreada.cs  PedidoEstadoCambiado.cs
│
├─ Facturas/                            (Slice 3)
│  ├─ Factura.cs                        (polymorphic root via TipoComprobante + factories — see §5a)
│  ├─ FacturaLinea.cs                   (owned entity — 1:1 with a LineaPedido snapshot)
│  ├─ Pago.cs                           (owned entity — N per Factura)
│  └─ Events/
│     └─ FacturaNecesitaCAE.cs          (AFIP integration seam)
│
├─ Stock/                               (Slice 3)
│  └─ MovimientoStock.cs                (append-only ledger AGGREGATE ROOT — see §5b)
│
└─ Services/                            (domain service contracts — interfaces only)
   ├─ IEfectivoPrecioService.cs         (Slice 2 — effective price resolution)
   └─ ICalculadorFactura.cs            (Slice 3 — IVA breakdown / totals)
```

**Decision — `Common/` is a shared kernel, not a framework.** `Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, `DomainException` are ~5 tiny base types with no external dependency. They give every aggregate consistent identity-equality, a domain-event buffer, and a single guard-clause exception type. This is the only "infrastructure-shaped" code allowed in Domain, and it is pure C#.

**Decision — namespaces mirror folders** (`GastroGestion.Domain.Pedidos`, `GastroGestion.Domain.ValueObjects`, …), honoring the repo `.editorconfig` rule `dotnet_style_namespace_match_folder`.

---

## 2. Aggregate diagrams

Notation: **(R)** root · **(O)** owned entity (no identity outside the root, never loaded alone) · **→Id** referenced by identifier across an aggregate boundary (never a navigation that loads the other aggregate) · invariants are enforced in the ctor/factory or the named command method.

### Cliente (Slice 1)

```text
Cliente (R)
 ├─ Direcciones : Direccion[] (O)
 └─ refs: (none)
```
- **Fields:** `Id` (Guid), `NumeroCliente` (int, immutable, infra-assigned), `Nombre`, `Apellido`, `Email` (VO), `Cuit?` (VO), `CondicionIVA` (enum), `Estado` (active flag — soft delete), `FechaAlta`.
- **Invariants:** `Nombre` required; if `CondicionIVA` requires fiscal identity (Responsable Inscripto / Monotributo) then `Cuit` is required; `Cuit` uniqueness is an INFRA invariant (cannot be checked inside one aggregate), the domain only guarantees the VO is well-formed.
- **Commands:** `AgregarDireccion(Direccion)`, `QuitarDireccion(Guid direccionId)`, `Desactivar()`, `Modificar(...)` (every field except `NumeroCliente`).
- **Referenced by Id** from `Pedido` (`ClienteId`) and `Factura` (`ClienteId`).

### Ingrediente (Slice 1)

```text
Ingrediente (R)   — no owned entities
```
- **Fields:** `Id`, `NumeroIngrediente` (immutable), `Nombre`, `Descripcion?`, `Unidad` (UnidadDeMedida), `Estado`.
- **Invariants:** `Nombre` required; name uniqueness is INFRA.
- **Commands:** `Modificar(...)`, `Desactivar()`.
- **Referenced by Id** from `LineaReceta`, `MovimientoStock`, `LineaRecetaSnapshot`.

### Plato (Slice 1)

```text
Plato (R)
 ├─ Receta : LineaReceta[] (O)  ── each →Id Ingrediente
 └─ PrecioBase : Dinero, Alicuota : AlicuotaIVA
```
- **Fields:** `Id`, `NumeroPlato` (immutable), `Nombre`, `Descripcion?`, `PrecioBase` (Dinero), `Alicuota` (AlicuotaIVA), `Estado`.
- **`LineaReceta` (O):** `IngredienteId` (→Id), `Cantidad` (VO: decimal + UnidadDeMedida).
- **Invariants:** `Nombre` required; `PrecioBase >= 0`; every recipe line `Cantidad.Valor > 0`; a recipe line's `Unidad` must match the referenced `Ingrediente.Unidad` — but the domain cannot load the Ingrediente, so this is enforced at the application layer; the `LineaReceta` itself only guarantees `Cantidad > 0`. (Seam left: a future combo line could reference a `Plato` instead of an `Ingrediente`.)
- **Commands:** `AgregarLineaReceta(...)`, `QuitarLineaReceta(...)`, `CambiarPrecioBase(Dinero)`, `Desactivar()`.

### Menú (Slice 1)

```text
Menu (R)  (a date's offering)
 └─ Items : MenuItem[] (O)  ── each →Id Plato, optional PrecioOverride
```
- **Fields:** `Id`, `Fecha` (DateOnly), `Items`.
- **`MenuItem` (O):** `PlatoId` (→Id), `PrecioOverride?` (Dinero), `Estado`.
- **Invariants:** at most one active `MenuItem` per `PlatoId` within the menu; `PrecioOverride >= 0` when present.
- **Commands:** `AgregarItem(platoId, precioOverride?)`, `QuitarItem(...)`, `CambiarOverride(...)`.

### Mesa (Slice 1)

```text
Mesa (R)   — no owned entities
```
- **Fields:** `Id`, `NumeroMesa`, `Capacidad`, `Zona?` (string), `Estado` (EstadoMesa: Libre/Ocupada), `Activa` (soft-delete flag), `PedidoActivoId?` (→Id — enforces one open salón order per table), `RowVersion` (byte[] — see §9).
- **Invariants:** `Capacidad > 0`; cannot `Ocupar` a table that already has a `PedidoActivoId`; cannot create a salón order against an inactive table (checked by the app, the table itself rejects `Ocupar` when not `Libre`).
- **Commands:** `Ocupar(pedidoId)`, `Liberar()`, `Desactivar()`, `Modificar(...)`.

### Pedido (Slice 2 — the central boundary)

```text
Pedido (R)
 ├─ Lineas : LineaPedido[] (O)   ── each →Id Plato, set-once price snapshot
 ├─ OrdenesTrabajo : OrdenTrabajo[] (O)  ── each →Id Plato + recipe snapshot
 ├─ DireccionEntrega? : DireccionEntrega (frozen VO, delivery only)
 ├─ refs: ClienteId (→Id), MesaId? (→Id)
 └─ Estado (EstadoPedido), Tipo (TipoPedido), RowVersion (byte[])
```
- **Why OT is owned (not its own aggregate):** the invariant *"all OTs `Listo` ⇒ Pedido advances to `ListoParaEntregar`"* and the cascade *"cancel Pedido ⇒ cancel all OTs"* are natural single-boundary invariants. Owning OT inside `Pedido` enforces them transactionally with no saga. Documented extraction path if the aggregate bloats (exploration §2 Option B), but **not v1**.
- **`LineaPedido` (O):** `PlatoId` (→Id), `Cantidad` (decimal), `Observaciones?`, `PrecioUnitarioSnapshot?` (Dinero, set-once), `AlicuotaSnapshot?` (PorcentajeIVA, set-once), computed `SubtotalLinea`/`IVALinea`/`TotalLinea`. Command `ConfirmarPrecio(Dinero, PorcentajeIVA)` — throws if already confirmed (see §7).
- **`OrdenTrabajo` (O):** `Id`, `NumeroOrden`, `PlatoId` (→Id), `Cantidad`, `Estado` (EstadoOT: `Creada`/`Preparandose`/`Lista`/`Cancelada`), `CocinaId?` (LegajoId), `RecetaSnapshot` (`LineaRecetaSnapshot[]` — the recipe frozen at OT creation). Commands `AsignarCocina(LegajoId)→Preparandose`, `Finalizar()→Lista`, `Cancelar()`.
- **Invariants:** a line is editable only while it has no OT, or its OT is still `Creada`; OT generation is all-or-nothing (the batch lives in the app/§5b, the domain only validates each OT's own creation); one OT per `(Pedido, Plato)`; on `Pedido.Cancelar()`, every owned OT is cancelled.
- **Commands:** `AgregarLinea(...)`, `ModificarLinea(...)` (guarded), `GenerarOrdenTrabajo(...)`, `TransicionarEstado(EstadoPedido nuevo, Rol rol)` (see §6), `Cancelar(rol)`.
- **Referenced by Id** from `Factura` (`PedidoIds[]`) and `MovimientoStock` (`OrdenTrabajoId?`).

### Factura / Comprobante (Slice 3 — polymorphic root)

```text
Factura (R)  [TipoComprobante discriminator]
 ├─ Lineas : FacturaLinea[] (O)   ── each 1:1 a LineaPedido snapshot
 ├─ Pagos : Pago[] (O)            ── N per factura
 ├─ refs: ClienteId (→Id), PedidosFacturados : Guid[] (→Id)
 └─ Tipo (TipoComprobante), Estado (EstadoFactura), CAE?, VencimientoCAE?
```
- See §5a for the polymorphism and factory invariants.
- **`FacturaLinea` (O):** copies the `LineaPedido` snapshot (`PlatoId`, `Cantidad`, `PrecioUnitarioSnapshot`, `AlicuotaSnapshot`). Totals computed, never stored.
- **`Pago` (O):** `Monto` (Dinero), `Metodo` (MetodoPago), `FechaPago`.
- **Invariants:** all grouped pedidos belong to the same `ClienteId`; `Factura` becomes `Pagada` when `Σ Pagos.Monto >= Total`; cancel only from `Creada`.

### MovimientoStock (Slice 3 — append-only ledger root)

```text
MovimientoStock (R)   — append-only, never updated
 └─ refs: IngredienteId (→Id), OrdenTrabajoId? (→Id), LineaPedidoId? (→Id)
```
- See §5b. Each movement is its **own** aggregate root (inserted independently in separate transactions); the "ledger" is a repository query, not a parent aggregate.

---

## 3. Value objects

All extend `Common/ValueObject` (structural equality), are immutable (init-only / ctor-set), and validate in the constructor (throw `DomainException` on bad input).

| VO | Fields | Validation | Equality / behavior |
|----|--------|-----------|--------------------|
| **Dinero** | `Monto` (decimal), `Moneda` (enum, default `ARS`) | `Monto` finite; arithmetic across different `Moneda` throws | Equality by `(Monto, Moneda)`. Ops: `operator +`/`-`, `Multiplicar(decimal)`, `AplicarIVA(PorcentajeIVA) → Dinero` (the IVA amount), `ConIVA(PorcentajeIVA) → Dinero` (gross). Currency-mix guard on every binary op. |
| **Cuit** | `Valor` (string, 11 digits) | Strips formatting, validates length and **check digit**; throws otherwise | Equality by normalized digits. `ToString()` formats `XX-XXXXXXXX-X`. |
| **Email** | `Valor` (string, normalized lower) | Format-validated at construction | Equality by normalized value. |
| **Cantidad** | `Valor` (decimal), `Unidad` (UnidadDeMedida) | `Valor > 0`; arithmetic across different `Unidad` throws | Equality by `(Valor, Unidad)`. No silent unit conversion in the domain — conversion is an app concern. |
| **PorcentajeIVA** | `Alicuota` (AlicuotaIVA enum) → exposes `Tasa` (decimal: 0m / 0.105m / 0.21m / 0.27m) | Closed set; constructed only from a valid `AlicuotaIVA` | Equality by `Alicuota`. `Cero` static for `TicketInterno`. |
| **LegajoId** | `Valor` (string) | Non-empty | Equality by value; prevents confusing it with a `UserId`. |
| **DireccionEntrega** | `Calle`, `Altura`, `Piso?`, `Localidad`, `Zona` | All required except `Piso`; frozen at `Pedido` creation | Equality by all components — see §4. |

**Decision — `Moneda` lives beside `Dinero` and defaults to `ARS`.** The system is single-currency today, but typing money prevents the "raw decimal" class of IVA bugs and leaves the seam open. Every `Dinero` binary op guards currency equality.

**Decision — `PorcentajeIVA` wraps the `AlicuotaIVA` enum rather than a free decimal.** Argentine IVA is a closed set; modeling it as an enum-backed VO makes `TicketInterno` IVA-zero unrepresentable as anything else and keeps the breakdown grouping (§8) trivial.

---

## 4. Direccion — dual nature (owned entity vs frozen VO)

This is the one type with two roles, and the design keeps them as **two distinct types** to avoid a leaky shared class:

| Role | Type | Where | Identity? |
|------|------|-------|-----------|
| A client's address book entry | `Direccion` (entity) | `Clientes/Direccion.cs`, owned by `Cliente` | Has `Id`; lifecycle tied to its `Cliente`. |
| The delivery address printed on a delivery order | `DireccionEntrega` (VO) | `Pedidos/DireccionEntrega.cs`, frozen on `Pedido` | No identity; equality by value; **immutable snapshot** taken at order creation. |

**Decision (resolved):** a delivery `Pedido` captures a **frozen `DireccionEntrega` VO** at creation. Editing the client's `Direccion` later never alters past orders. The app maps the chosen `Direccion` entity → `DireccionEntrega` VO when creating the order; the domain stores only the VO.

---

## 5. Hard parts

### 5a. Polymorphic Comprobante — TPH via a domain discriminator (Slice 3)

**Decision (locked): single `Factura` aggregate root + `TipoComprobante` discriminator + factory methods.** Rejected: TPT (forces persistence hierarchy knowledge into the domain, JOIN cost) and three separate aggregates (duplicated state machine + grouping logic). TPH keeps one consistency boundary and one "list all comprobantes for a client" query.

```text
enum TipoComprobante { TicketInterno, FacturaConIVA, FacturaElectronica }

Factura.CrearTicket(clienteId, pedidoIds, lineas)
    → Tipo = TicketInterno;  CAE = null;  VencimientoCAE = null;  IVA per line = 0
Factura.CrearFacturaConIVA(clienteId, pedidoIds, lineas)
    → Tipo = FacturaConIVA;  CAE = null;  VencimientoCAE = null;  IVA from line snapshots
Factura.CrearFacturaElectronica(clienteId, pedidoIds, lineas)
    → Tipo = FacturaElectronica;  CAE/VencimientoCAE START NULL (awaiting AFIP),
      then assigned once via AsignarCae(cae, vencimiento); raises FacturaNecesitaCAE on creation
```

- **CAE invariants guarded in the factories:** `TicketInterno` and `FacturaConIVA` **must** have `CAE == null`; `FacturaElectronica` is created with `CAE == null` and *requires* a later `AsignarCae(...)` before it can be considered fiscally complete. `AsignarCae` is set-once and rejects non-electronic types. The "CAE present ⇔ electronic and assigned" rule is therefore unrepresentable to violate.
- **Persistence-agnostic guarantee:** the domain class carries **no** EF inheritance attribute, no `[Discriminator]`, no `HasDiscriminator` — `TipoComprobante` is a plain enum property. The TPH mapping (discriminator column, nullable `CAE`/`VencimientoCAE` columns) is configured entirely in phase 3's `IEntityTypeConfiguration<Factura>`. The domain only knows "I am of type X and my CAE is null-or-set."
- **AFIP seam:** creating a `FacturaElectronica` raises `FacturaNecesitaCAE`; the AFIP adapter (later phase) handles the event, calls AFIP, and calls back `AsignarCae`. The domain never imports an AFIP SDK.

### 5b. Stock ledger with reservations + lot/expiry (Slice 3)

**Decision (locked): `MovimientoStock` is its own append-only aggregate root.** Insert-only, never updated. The balance is a **projection**, never stored.

```text
enum TipoMovimientoStock {
    Compra,                 // +  ingress, carries Lote + FechaVencimiento
    Consumo,                // -  actual kitchen consumption at OT creation
    Ajuste,                 // ±  manual correction
    Reserva,                // -  stock held when a LineaPedido is added
    LiberacionReserva,      // +  cancels a prior Reserva when it becomes Consumo
    DevolucionCancelacion   // +  restores stock when a Creada OT is cancelled
}

MovimientoStock (R):
    IngredienteId (→Id), Cantidad (signed decimal), Tipo, FechaTransaccion,
    OrdenTrabajoId? (→Id), LineaPedidoId? (→Id),
    Lote? (string), FechaVencimiento? (DateOnly), UsuarioId? (LegajoId)
```

- **Balance as projection (pure function, no stored field):** `Disponible(ingredienteId) = Σ Cantidad` over all movements for that ingredient, given the sign convention (`Reserva`/`Consumo` are negative, the rest positive). This pure projection is the same function the producible calculation (phase 4) reuses. Unit-tested across the full reserve→consume→liberate→return sequence.
- **Reservation lifecycle (the movement choreography):**
  - `LineaPedido` added → `MovimientoStock(Reserva, -qty, LineaPedidoId=ref)`.
  - OT created → `MovimientoStock(Consumo, -qty, OrdenTrabajoId=ref)` **and** `MovimientoStock(LiberacionReserva, +qty)` to retire the matching reservation.
  - Cancel while OT `Creada` → `MovimientoStock(DevolucionCancelacion, +qty)`.
  - Cancel while OT `Preparandose`/`Lista` → no movement (already consumed).
- **Lot/expiry-aware from day one:** `Compra` movements set `Lote` + `FechaVencimiento`; the near-expiry suggestion feature (later phase) is a query over these fields, no schema change. The ranking algorithm itself is **out of scope**.
- **Atomicity is an INFRA invariant, stated explicitly.** The domain defines only `Disponible >= 0` as the rule a reservation must not break. The *check-then-reserve* atomicity (so two cashiers cannot both reserve the last unit) is enforced in phase 3/4 via row-level locking or a serializable transaction on the `Ingrediente` row. **The domain layer contains no locking logic** — this is the line that keeps the ledger pure.

### 5c. Price snapshotting (Slice 2)

**Decision (locked): resolve via `IEfectivoPrecioService`, write once via `LineaPedido.ConfirmarPrecio`.**

```text
interface IEfectivoPrecioService {
    (Dinero precio, PorcentajeIVA iva) ResolverPrecioEfectivo(Guid platoId, DateOnly fecha);
}
```
- Resolution rule (pure): `menu override for (platoId, fecha)` → else `Plato.PrecioBase`; the aliquot is `Plato.Alicuota`. The *implementation* (which needs to read the Menú/Plato repositories) lives in the application layer; the **contract** lives in Domain so the aggregate depends on an abstraction, not a concretion.
- `LineaPedido.ConfirmarPrecio(Dinero precio, PorcentajeIVA iva)` is **set-once**: it throws `DomainException` if `PrecioUnitarioSnapshot` is already set. After confirmation the line's price is immutable; catalogue prices are never re-read.
- `Factura` totals compute from the line snapshots, never from `Plato.PrecioBase`.

### 5d. Dual Pedido state machines as data (Slice 2)

**Decision (locked): one merged `EstadoPedido` enum + a static `PedidoTransicionRegistry` of `(Tipo, Desde, Hasta, RolesPermitidos)` rows.** Reachable states are constrained by the registry per `TipoPedido`, not by branching code. Adding a transition = adding a row.

```text
record PedidoTransicion(
    TipoPedido Tipo,
    EstadoPedido Desde,
    EstadoPedido Hasta,
    IReadOnlyList<Rol> RolesPermitidos);

enum EstadoPedido {           // merged — registry gates which are reachable per tipo
    Abierto, Cerrado,                 // Salón
    Creado, Modificado, Preparandose, // Mostrador/Delivery
    ListoParaEntregar, Entregado,
    Cancelado                         // shared terminal
}
```

**Transition table — Salón:**

| Desde | Hasta | RolesPermitidos |
|-------|-------|-----------------|
| Abierto | Cerrado | ATC, Ventas, Gerente |
| Abierto | Cancelado | ATC, Ventas, Gerente |

**Transition table — Mostrador / Delivery (TakeAway = same table, channel label only):**

| Desde | Hasta | RolesPermitidos |
|-------|-------|-----------------|
| Creado | Modificado | ATC, Ventas, Gerente |
| Creado | Preparandose | ATC, Produccion, Gerente |
| Modificado | Preparandose | ATC, Produccion, Gerente |
| Preparandose | ListoParaEntregar | Produccion, Gerente *(also auto-raised when all OTs `Lista`)* |
| ListoParaEntregar | Entregado | Repartidor, ATC, Gerente |
| Creado | Cancelado | ATC, Ventas, Gerente |
| Modificado | Cancelado | ATC, Ventas, Gerente |
| Preparandose | Cancelado | Produccion, Gerente |

- **`Pedido.TransicionarEstado(EstadoPedido nuevo, Rol rol)`** queries the registry for a row matching `(this.Tipo, this.Estado, nuevo)`; rejects if no row (`DomainException` — illegal transition); rejects if `rol ∉ row.RolesPermitidos` (role gate); applies the new state; raises `PedidoEstadoCambiado(pedidoId, desde, hasta, rol)`.
- The "all OTs `Lista` ⇒ advance to `ListoParaEntregar`" rule runs **inside** the aggregate after an OT finishes (natural owned-entity invariant), going through the same `TransicionarEstado` path.

### 5e. Money / IVA (Slice 3 service, Slice 2 line computeds)

**Decision (locked): totals are computed, never stored; `ICalculadorFactura` returns the per-aliquot breakdown as a pure function.**

```text
interface ICalculadorFactura {
    ResultadoFactura Calcular(IReadOnlyList<FacturaLinea> lineas, TipoComprobante tipo);
}
record ResultadoFactura(
    Dinero SubTotal,
    IReadOnlyList<IVAPorAlicuota> DesgloseIVA,   // grouped by aliquot
    Dinero TotalIVA,
    Dinero Total);
record IVAPorAlicuota(PorcentajeIVA Alicuota, Dinero BaseImponible, Dinero Importe);
```
- Per line: `IVALinea = PrecioUnitarioSnapshot.Multiplicar(Cantidad).AplicarIVA(AlicuotaSnapshot)`. For `TicketInterno`, the calculator forces IVA to `Dinero.Cero` regardless of line aliquots.
- The breakdown groups lines **by aliquot** so the invoice shows `IVA 21%`, `IVA 10.5%`, etc. separately — required for a real Argentine factura.
- `Factura.SubTotal`/`TotalIVA`/`Total` are computed properties delegating to the same logic; phase 3 may denormalize for query perf, but that is an infrastructure choice, never a domain field.

---

## 6. Pedido transition registry & method (recap of the contract)

Already detailed in §5d. The reviewable surface: `PedidoTransicion` record shape, the two transition tables above, `TransicionarEstado(estado, rol)` validating against the registry + role gate + raising `PedidoEstadoCambiado`. **No `switch` on `Estado` anywhere in `Pedido`.**

---

## 7. Set-once price confirmation (recap)

`LineaPedido.ConfirmarPrecio(Dinero, PorcentajeIVA)` — callable exactly once; second call throws. Snapshots are init-only afterwards. This is the mechanical guarantee behind "invoices reproduce byte-for-byte regardless of later catalogue changes."

---

## 8. Domain events & dispatch contract

| Event | Raised by | When | Carries |
|-------|-----------|------|---------|
| `PedidoCreado` | `Pedido` factory | order created | pedidoId, clienteId, tipo |
| `LineaPedidoAgregada` | `Pedido.AgregarLinea` | line added | pedidoId, platoId, cantidad |
| `OrdenTrabajoCreada` | `Pedido.GenerarOrdenTrabajo` | OT created | pedidoId, ordenTrabajoId, platoId, recetaSnapshot |
| `PedidoEstadoCambiado` | `Pedido.TransicionarEstado` | any valid transition | pedidoId, desde, hasta, rol |
| `FacturaNecesitaCAE` | `Factura.CrearFacturaElectronica` | electronic invoice created | facturaId, total, clienteId |

**Decision — domain raises, infra/app dispatches.** Every event implements `Common/IDomainEvent`. `AggregateRoot` buffers events in a private list exposed as `IReadOnlyList<IDomainEvent> DomainEvents` with a `ClearDomainEvents()`. The Domain layer **only buffers** — it has no dispatcher, no `MediatR`, no event bus. Phase 3/4 reads `DomainEvents` after `SaveChanges` (or in a `SaveChangesInterceptor`) and dispatches to handlers. This keeps the dispatch mechanism (in-process now, possibly outbox later) entirely outside the domain.

---

## 9. Concurrency tokens

**Decision (locked): `byte[] RowVersion` as a plain domain property on `Pedido` and `Mesa`.**
- Type is `byte[]` (matches SQL Server `rowversion`/`timestamp`), exposed as an ordinary auto-property — **no `[Timestamp]`, no `[ConcurrencyCheck]` attribute** in the domain (those arrive in phase 3's EF configuration).
- Only the two aggregates with genuine concurrent-edit hazard carry it: `Pedido` (concurrent line edits / state changes) and `Mesa` (the one-open-order race). Catalogue aggregates and the append-only ledger do not need it (the ledger never updates a row).

---

## 10. Slice boundaries (the apply/PR map)

The design maps 1:1 to the three delivery slices; each slice is an independently reviewable, independently revertible PR, in strict dependency order.

| Slice | Types (exact) | Depends on | Risk |
|-------|---------------|-----------|------|
| **1 — Catalogue** | `Common/*` (Entity, AggregateRoot, ValueObject, IDomainEvent, DomainException); VOs `Dinero`, `Moneda`, `Cuit`, `Email`, `Cantidad`, `PorcentajeIVA`, `LegajoId`; enums `TipoPedido, EstadoPedido, EstadoOT, EstadoFactura, EstadoFacturaPedido, TipoComprobante, CondicionIVA, AlicuotaIVA, UnidadDeMedida, TipoMovimientoStock, MetodoPago, EstadoMesa, Rol`; aggregates `Cliente`+`Direccion`, `Ingrediente`, `Plato`+`LineaReceta`, `Menu`+`MenuItem`, `Mesa` | — (only Phase-1 scaffold) | Low — purely structural |
| **2 — Transactional** | `Pedido`, `LineaPedido`, `OrdenTrabajo`, `LineaRecetaSnapshot`, `DireccionEntrega`, `PedidoTransicionRegistry` (+ `PedidoTransicion` record); `Services/IEfectivoPrecioService`; events `PedidoCreado`, `LineaPedidoAgregada`, `OrdenTrabajoCreada`, `PedidoEstadoCambiado` | Slice 1 (Dinero, Cantidad, PorcentajeIVA, enums, AggregateRoot) | Med — state machines, set-once price |
| **3 — Fiscal** | `Factura`+`FacturaLinea`+`Pago`, `MovimientoStock`; `Services/ICalculadorFactura` (+ `ResultadoFactura`, `IVAPorAlicuota`); event `FacturaNecesitaCAE` | Slices 1–2 (Dinero, PorcentajeIVA, LineaPedido snapshot shape, AggregateRoot) | High — fiscal/AFIP, ledger sign convention |

**Dependency order is non-negotiable:** Slice 1 lands first (everything compiles against its VOs/enums); Slice 2 second (consumes them, defines the line snapshot shape Slice 3 copies); Slice 3 last. Slices 1+2 *could* combine into one PR for a domain-fluent reviewer; Slice 3 stays separate to protect the order model from fiscal churn.

---

## 11. Zero-dependency gate (acceptance, every slice)

`src/GastroGestion.Domain/GastroGestion.Domain.csproj` MUST contain:
- **Zero `<PackageReference>`** — no EF Core, no `System.Text.Json`-beyond-BCL, no AutoMapper, no MediatR, no FluentValidation, nothing.
- **Zero `<ProjectReference>`** — the innermost layer depends on nothing.

This is a **reviewable gate on every slice PR**: open the `.csproj`, confirm it has only the inherited `Directory.Build.props` settings (`net8.0`, nullable, implicit usings) and no reference elements. Any added reference is an automatic review block — it means a framework or infrastructure concern leaked into the domain.

---

## 12. ADR-style decision log

| # | Decision | Alternatives rejected | Rationale |
|---|----------|-----------------------|-----------|
| D1 | Hybrid layout: `Common/` + `ValueObjects/` + `Enums/` kernel, then one folder per aggregate | (a) by building-block only; (b) by aggregate only | Screaming architecture; shared kernel has an explicit home; each aggregate folder = a reviewable slice cluster. |
| D2 | Polymorphic `Factura` via TPH **domain discriminator** + factory methods | TPT base/subtypes; three separate aggregates | One consistency boundary, one client-comprobante query, no persistence hierarchy leaking into domain; CAE invariants enforced in factories. |
| D3 | `MovimientoStock` as its own append-only aggregate root; balance is a projection | Mutable `Stock.Cantidad`; `StockLedger` parent owning movements | Movements insert independently in different transactions; no "lock the whole ledger"; balance/producible is a pure query. Kills the legacy race-condition root. |
| D4 | Check-then-reserve atomicity is an **infra** invariant; domain only defines `Disponible >= 0` | Locking logic inside the aggregate | Domain stays pure and persistence-agnostic; concurrency belongs to phase 3/4 (row locks / serializable tx). |
| D5 | Price snapshot via `IEfectivoPrecioService` contract + set-once `ConfirmarPrecio` | Re-read `Plato.PrecioBase` at invoice time; store a price-history table in domain | Invoices reproduce exactly; the price-resolution implementation (needs repos) stays in the app, the contract stays in domain. |
| D6 | One merged `EstadoPedido` enum + data-driven `PedidoTransicionRegistry`; `TransicionarEstado(estado, rol)` | Two enums; `switch`-based transition code; per-tipo subclasses | New transition = new row; illegal transitions/role violations rejected in one place; no branching swamp. TakeAway reuses the Mostrador rows. |
| D7 | Money/IVA computed (never stored); `ICalculadorFactura` pure breakdown grouped by aliquot | Stored `Total`/`TotalIVA` mutable fields | Stored totals diverge from line data; computed totals are always correct; real factura needs per-aliquot grouping. `TicketInterno` IVA forced to zero. |
| D8 | Domain **raises/buffers** events; infra/app **dispatches** | In-domain MediatR/event bus; immediate dispatch in the aggregate | Keeps dispatch mechanism (in-process / outbox) out of the domain; `AggregateRoot` exposes a buffer + clear. |
| D9 | `byte[] RowVersion` plain property on `Pedido` + `Mesa` only | `[Timestamp]` attribute; `Guid`/`int` version; token on every aggregate | No EF attribute keeps domain pure; `byte[]` matches SQL `rowversion`; only the two contended aggregates need it. |
| D10 | `Direccion` (entity, owned by `Cliente`) and `DireccionEntrega` (frozen VO on `Pedido`) are **two distinct types** | One shared `Direccion` class used both ways | Distinct semantics (identity vs frozen snapshot); avoids a leaky dual-purpose class; resolves the delivery-snapshot decision. |
| D11 | Domain `.csproj` zero package + zero project references — gated on every PR | "Just one helper package" | The Clean Architecture innermost-layer rule; the single guard that keeps the domain portable and testable. |

---

## 13. Checklist (reviewer can confirm)

- [ ] Layout matches §1: `Common/`, `ValueObjects/`, `Enums/`, one folder per aggregate, `Services/`.
- [ ] Every VO in §3 is immutable, validates in its ctor, and extends `ValueObject` with structural equality.
- [ ] `Factura` is one class with `TipoComprobante` + the three factories; CAE null/set invariants enforced; **no EF discriminator attribute**.
- [ ] `MovimientoStock` is append-only, its own root; balance is a projection function, not a stored field; lot/expiry fields present; no locking code.
- [ ] `LineaPedido.ConfirmarPrecio` is set-once (second call throws); `IEfectivoPrecioService` contract lives in `Services/`.
- [ ] `PedidoTransicionRegistry` holds the §5d rows; `TransicionarEstado(estado, rol)` validates + role-gates + raises `PedidoEstadoCambiado`; no `switch` on state.
- [ ] `ICalculadorFactura` returns per-aliquot breakdown; totals computed, never stored; `TicketInterno` IVA zero.
- [ ] All five events implement `IDomainEvent`; `AggregateRoot` buffers + exposes + clears; no in-domain dispatcher.
- [ ] `RowVersion` is a plain `byte[]` property on `Pedido` and `Mesa` only; no EF attribute.
- [ ] Slice boundaries match §10; dependency order Slice 1 → 2 → 3 holds.
- [ ] **`GastroGestion.Domain.csproj` has zero `PackageReference` and zero `ProjectReference`.**

## Next step

Proceed to `sdd-tasks` (requires both the spec and this design). Tasks will sequence the three slices: Slice 1 kernel + VOs + enums + catalogue aggregates + their tests → Slice 2 `Pedido` cluster + registry + price service + events + tests → Slice 3 `Factura`/`Pago`/`MovimientoStock` + `ICalculadorFactura` + AFIP event + tests, each with the zero-dependency `.csproj` gate verified per PR.
