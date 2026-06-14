# Delta Spec — domain-port

**Scope:** Pure domain layer (`GastroGestion.Domain`, `net8.0`). Defines what MUST be true once this change is applied. Does not describe how to implement anything.

**Phase:** 2 of 7 in the strangler roadmap.
**Language covenant:** all identifiers, class names, and domain nouns follow the Spanish ubiquitous language established in `docs/functional-scope.md`.

---

## Non-goals (explicitly out of scope)

- No EF Core, no `DbContext`, no entity configurations, no migrations
- No ASP.NET Core, no HTTP concerns, no JSON serialisation attributes
- No AutoMapper, no FluentValidation, no MediatR
- No application-layer use-cases or CQRS handlers
- No AFIP/ARCA integration code (CAE *values* are stored; the HTTP call is a later phase)
- No gap-free sequence generation (NumeroCliente, NumeroPlato, etc. — assigned in phase 3 infrastructure)
- No near-expiry suggestion algorithm (ledger is lot/expiry-aware; ranking is a later phase)
- No combo/sub-recipe support (seam exists; implementation is not v1)
- No table transfers or mesa merges
- No third-party delivery platform adapters
- No legacy net48 classes (`Dominio/`) modified or deleted by this change
- `Id_Empresa` / `Id_Sucursal` are NOT fields on any domain entity

---

## Aggregate map (overview)

| Aggregate root | Owned entities | Value Objects used |
|---|---|---|
| `Cliente` | `Direccion[]` | `Cuit`, `Email`, `CondicionIVA` (enum) |
| `Ingrediente` | — | `UnidadDeMedida` (enum) |
| `Plato` | `LineaReceta[]` | `Dinero`, `AlicuotaIVA` (enum) |
| `Menu` | `MenuItem[]` | `Dinero` |
| `Mesa` | — | — |
| `Pedido` | `LineaPedido[]`, `OrdenTrabajo[]` | `Dinero`, `PorcentajeIVA`, `Cantidad`, `Direccion` (delivery snapshot VO) |
| `MovimientoStock` | — | `Cantidad` |
| `Factura` | `FacturaLinea[]`, `Pago[]` | `Dinero`, `PorcentajeIVA` |

---

## Non-goals dependency note

`GastroGestion.Domain.csproj` MUST list zero `<PackageReference>` and zero `<ProjectReference>` elements. Verified by REQ-ZERO-DEPS below.

---

## REQ-01 — Domain project has zero outward dependencies

**What must be true:**

- `GastroGestion.Domain.csproj` contains no `<PackageReference>` elements.
- `GastroGestion.Domain.csproj` contains no `<ProjectReference>` elements.
- The project compiles successfully with `dotnet build`.

### Scenario 01-A — No package or project references

```
Given  GastroGestion.Domain.csproj is inspected
When   all <PackageReference> and <ProjectReference> elements are counted
Then   the combined count is 0
```

### Scenario 01-B — Domain builds in isolation

```
Given  the .NET 8 SDK is installed
When   `dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj` is executed
Then   the command exits with code 0
And    no build errors are reported
```

---

## REQ-02 — Value Objects: construction validation rules

**What must be true:**

- Every value object is immutable (all properties `init` or `readonly`).
- Equality is value-based (record or explicit `Equals`/`GetHashCode`).
- Invalid construction throws `ArgumentException` or `DomainException` (never returns null).

### REQ-02-A — `Dinero`

- Has `decimal Amount` and `string Currency` (default `"ARS"`).
- `Amount` must be ≥ 0; negative values are rejected.
- Two `Dinero` values with different currencies cannot be added (throws).
- Arithmetic: `+`, `-` (result Amount ≥ 0 enforced on subtraction call site, not on VO), `*` by a positive decimal scalar.

#### Scenario 02-A-1 — Negative amount rejected

```
Given  a call to new Dinero(-0.01m, "ARS")
When   the constructor executes
Then   an ArgumentException is thrown
```

#### Scenario 02-A-2 — Currency mismatch on addition rejected

```
Given  d1 = new Dinero(100m, "ARS") and d2 = new Dinero(5m, "USD")
When   d1 + d2 is evaluated
Then   a DomainException is thrown indicating currency mismatch
```

#### Scenario 02-A-3 — Same-currency addition

```
Given  d1 = new Dinero(100m, "ARS") and d2 = new Dinero(50m, "ARS")
When   d1 + d2 is evaluated
Then   the result is new Dinero(150m, "ARS")
```

### REQ-02-B — `Cuit`

- Accepts 11-digit strings in raw (`20345678901`) or formatted (`20-34567890-1`) form.
- Normalises to raw 11-digit string internally.
- Rejects strings that are not 11 digits after stripping hyphens.
- Rejects null or empty.

#### Scenario 02-B-1 — Valid formatted CUIT accepted

```
Given  the string "20-34567890-1"
When   new Cuit("20-34567890-1") is called
Then   no exception is thrown
And    cuit.Value is "20345678901"
```

#### Scenario 02-B-2 — Non-11-digit string rejected

```
Given  the string "2034567890" (10 digits)
When   new Cuit("2034567890") is called
Then   an ArgumentException is thrown
```

### REQ-02-C — `Email`

- Accepts strings containing exactly one `@` with non-empty local and domain parts.
- Rejects null, empty, and strings without `@`.
- Stores the lowercased normalised form.

#### Scenario 02-C-1 — Valid email accepted

```
Given  the string "Cliente@Example.COM"
When   new Email("Cliente@Example.COM") is called
Then   no exception is thrown
And    email.Value is "cliente@example.com"
```

#### Scenario 02-C-2 — No-@ string rejected

```
Given  the string "notanemail"
When   new Email("notanemail") is called
Then   an ArgumentException is thrown
```

### REQ-02-D — `Cantidad`

- Has `decimal Amount` (> 0) and `UnidadDeMedida Unidad`.
- `Amount` must be > 0; zero and negative values are rejected.

#### Scenario 02-D-1 — Zero amount rejected

```
Given  a call to new Cantidad(0m, UnidadDeMedida.Kg)
When   the constructor executes
Then   an ArgumentException is thrown
```

#### Scenario 02-D-2 — Valid fractional amount accepted

```
Given  a call to new Cantidad(0.250m, UnidadDeMedida.Kg)
When   the constructor executes
Then   no exception is thrown
And    cantidad.Amount is 0.250m
```

### REQ-02-E — `PorcentajeIVA`

- Wraps a decimal representing the IVA rate (e.g. `0.21m` for 21 %, `0m` for 0 %).
- Accepted values: exactly `0m`, `0.105m`, `0.21m`, `0.27m` (closed Argentine set for v1).
- Any other decimal is rejected.

#### Scenario 02-E-1 — Valid aliquot accepted

```
Given  a call to new PorcentajeIVA(0.21m)
When   the constructor executes
Then   no exception is thrown
```

#### Scenario 02-E-2 — Arbitrary decimal rejected

```
Given  a call to new PorcentajeIVA(0.15m)
When   the constructor executes
Then   an ArgumentException is thrown
```

---

## REQ-03 — `Cliente` aggregate

**What must be true:**

- `Cliente` is an aggregate root with a `Guid Id` (surrogate), `int NumeroCliente` (immutable after assignment), `string Nombre` (required, non-empty), `string Apellido` (required, non-empty), `Email Email`, nullable `Cuit Cuit`, `CondicionIVA CondicionIVA`, nullable `string Telefono`, and an `bool Activo` soft-delete flag.
- `NumeroCliente` is assigned once; any attempt to re-assign throws.
- A `Cliente` owns a collection of `Direccion` (1-to-N). The collection is never null; it can be empty.
- CUIT uniqueness at domain level is expressed as an invariant intent: if `Cuit` is set, it must be unique within the client collection (enforced by infrastructure; domain exposes the rule via a static guard method or interface `IClienteUniquenessChecker`).

### Scenario 03-A — Blank Nombre rejected

```
Given  a CreateCliente command with Nombre = ""
When   the Cliente constructor or factory method is called
Then   an ArgumentException is thrown
```

### Scenario 03-B — NumeroCliente is immutable

```
Given  an existing Cliente with NumeroCliente = 42
When   code attempts to set NumeroCliente to 99
Then   a DomainException or InvalidOperationException is thrown
```

### Scenario 03-C — Soft-delete sets Activo = false

```
Given  an active Cliente
When   cliente.Desactivar() is called
Then   cliente.Activo is false
And    calling Desactivar() a second time does not throw (idempotent)
```

### Scenario 03-D — Address added to client

```
Given  a Cliente with no addresses
When   cliente.AgregarDireccion(nuevaDireccion) is called with a valid Direccion
Then   cliente.Direcciones contains the new Direccion
And    the collection count is 1
```

### Scenario 03-E — Direccion belongs to only one client

```
Given  two separate Cliente instances A and B
When   the same Direccion (by reference or equivalent value) is inspected
Then   a Direccion created for client A cannot be retrieved from client B's Direcciones
```

---

## REQ-04 — `Ingrediente` aggregate

**What must be true:**

- `Ingrediente` is an aggregate root with `Guid Id`, `int NumeroIngrediente` (immutable), `string NombreIngrediente` (required, non-empty), nullable `string Descripcion`, `UnidadDeMedida UnidadDeMedida`, and `bool Activo` soft-delete.
- Name uniqueness is an infrastructure concern; domain exposes the rule via an interface or static check convention (same pattern as CUIT).

### Scenario 04-A — Blank name rejected

```
Given  a CreateIngrediente command with NombreIngrediente = "   " (whitespace)
When   the Ingrediente constructor is called
Then   an ArgumentException is thrown
```

### Scenario 04-B — Soft-delete

```
Given  an active Ingrediente
When   ingrediente.Desactivar() is called
Then   ingrediente.Activo is false
```

---

## REQ-05 — `Plato` aggregate and `LineaReceta`

**What must be true:**

- `Plato` is an aggregate root with `Guid Id`, `int NumeroPlato` (immutable), `string NombrePlato` (required, non-empty), nullable `string Descripcion`, `Dinero PrecioBase`, `AlicuotaIVA AlicuotaIVA`, `bool Activo`, and `IReadOnlyList<LineaReceta> Receta`.
- `LineaReceta` is an owned entity with `Guid IngredienteId`, `Cantidad Cantidad` (Amount > 0, Unidad must match `Ingrediente.UnidadDeMedida` at application-layer validation; domain stores whatever is given but `Cantidad` itself enforces > 0).
- The recipe leaves a seam for future combo support: `LineaReceta` has an optional `Guid? PlatoReferenciadoId` (null in v1, reserved for sub-recipe use).
- Name uniqueness is an infrastructure concern.

### Scenario 05-A — Blank NombrePlato rejected

```
Given  a CreatePlato command with NombrePlato = ""
When   the Plato constructor or factory is called
Then   an ArgumentException is thrown
```

### Scenario 05-B — Negative PrecioBase rejected

```
Given  a CreatePlato command with PrecioBase.Amount = -1m
When   the Plato constructor is called
Then   an ArgumentException is thrown (propagated from Dinero)
```

### Scenario 05-C — Recipe line added

```
Given  an existing Plato with an empty recipe
When   plato.AgregarLineaReceta(ingredienteId, new Cantidad(0.5m, UnidadDeMedida.Kg)) is called
Then   plato.Receta contains one LineaReceta with IngredienteId and Amount = 0.5m
```

### Scenario 05-D — Soft-delete

```
Given  an active Plato
When   plato.Desactivar() is called
Then   plato.Activo is false
```

---

## REQ-06 — `Menu` aggregate and `MenuItem`

**What must be true:**

- `Menu` is an aggregate root with `Guid Id`, `DateOnly FechaMenu` (must be ≥ today at creation time; domain clock abstracted as `IDateTimeProvider`), `bool Activo`, and `IReadOnlyList<MenuItem> Items`.
- `MenuItem` is an owned entity with `Guid PlatoId`, nullable `Dinero PrecioOverride`, and `bool Activo`.
- A `PlatoId` can appear at most once per `Menu` (no duplicate plates on the same day's menu).
- `PrecioOverride`, if set, must have `Amount > 0`.
- Effective price resolution rule (pure, deterministic): if `MenuItem.PrecioOverride != null` → use it; else use `Plato.PrecioBase`. This resolution is the contract of `IEfectivoPrecioService` (domain service interface).

### Scenario 06-A — Past date rejected

```
Given  today is 2026-06-10
When   new Menu(fecha: DateOnly.FromDateTime(new DateTime(2026, 6, 9))) is called
Then   a DomainException is thrown indicating the date must be today or future
```

### Scenario 06-B — Duplicate plate on same menu rejected

```
Given  a Menu with MenuItem for PlatoId = X
When   menu.AgregarItem(platoId: X, precioOverride: null) is called again
Then   a DomainException is thrown indicating duplicate plate
```

### Scenario 06-C — Price override of 0 rejected

```
Given  a Menu and a call to AgregarItem with PrecioOverride = new Dinero(0m, "ARS")
When   the method executes
Then   a DomainException is thrown indicating override price must be positive
```

### Scenario 06-D — Effective price resolves to override when set

```
Given  a MenuItem with PrecioOverride = new Dinero(350m, "ARS")
And    Plato.PrecioBase = new Dinero(400m, "ARS")
When   IEfectivoPrecioService.ObtenerPrecioEfectivo(platoId, fecha) is called
Then   the returned Dinero is 350m ARS
```

### Scenario 06-E — Effective price falls back to PrecioBase when no override

```
Given  a MenuItem with PrecioOverride = null
And    Plato.PrecioBase = new Dinero(400m, "ARS")
When   IEfectivoPrecioService.ObtenerPrecioEfectivo(platoId, fecha) is called
Then   the returned Dinero is 400m ARS
```

---

## REQ-07 — `Pedido` aggregate — creation and structure

**What must be true:**

- `Pedido` is an aggregate root with `Guid Id`, `int NumeroPedido`, `TipoPedido TipoPedido` (Salon / Mostrador / Delivery — TakeAway is treated as Mostrador), `Guid ClienteId`, nullable `Guid MesaId` (required iff `TipoPedido == Salon`), `DireccionSnapshot? DireccionEntrega` (required iff `TipoPedido == Delivery`; frozen VO set at creation, never updated), `EstadoPedido Estado`, `EstadoFacturaPedido EstadoFacturacion` (NoFacturado / Facturado / Pagado), `byte[] ConcurrencyToken`, `IReadOnlyList<LineaPedido> Lineas`, `IReadOnlyList<OrdenTrabajo> OrdenesDeTrabajo`.
- `DireccionSnapshot` is a value object with the full address fields frozen at creation time.
- For `TipoPedido.Salon`, `MesaId` must be non-null at creation.
- For `TipoPedido.Delivery`, `DireccionEntrega` must be non-null at creation.

### Scenario 07-A — Salon pedido without MesaId rejected

```
Given  TipoPedido = Salon and MesaId = null
When   Pedido.Crear(...) factory is called
Then   a DomainException is thrown indicating MesaId is required for Salon
```

### Scenario 07-B — Delivery pedido without DireccionEntrega rejected

```
Given  TipoPedido = Delivery and DireccionEntrega = null
When   Pedido.Crear(...) factory is called
Then   a DomainException is thrown indicating DireccionEntrega is required for Delivery
```

### Scenario 07-C — Delivery address is frozen at creation

```
Given  a Delivery Pedido created with DireccionEntrega = { Calle = "Av. Corrientes", Altura = "1234" }
When   the original Direccion entity changes its Calle to "Av. Rivadavia"
Then   pedido.DireccionEntrega.Calle is still "Av. Corrientes"
```

---

## REQ-08 — `Pedido` — line management and price snapshot

**What must be true:**

- `LineaPedido` is an owned entity with `Guid PlatoId`, `int Cantidad` (> 0), nullable `string Observaciones`, `Dinero? PrecioUnitarioSnapshot`, `PorcentajeIVA? AlicuotaIVASnapshot`, and `bool StockReservado`.
- `PrecioUnitarioSnapshot` and `AlicuotaIVASnapshot` are set once via `lineaPedido.ConfirmarPrecio(Dinero, PorcentajeIVA)`. A second call throws.
- A line can be edited (quantity/observations changed) only while the `Pedido` has no `OrdenTrabajo`, OR all existing `OrdenTrabajo` for that line are in state `Creada`.
- Computed properties: `SubtotalLinea = PrecioUnitarioSnapshot.Amount * Cantidad`, `IVALinea = SubtotalLinea * AlicuotaIVASnapshot.Value`, `TotalLinea = SubtotalLinea + IVALinea`.

### Scenario 08-A — Price snapshot set-once

```
Given  a LineaPedido with PrecioUnitarioSnapshot = null
When   lineaPedido.ConfirmarPrecio(new Dinero(200m, "ARS"), new PorcentajeIVA(0.21m)) is called
Then   PrecioUnitarioSnapshot is 200m ARS and AlicuotaIVASnapshot is 0.21
And    calling ConfirmarPrecio again throws a DomainException
```

### Scenario 08-B — Line edit blocked when OT is in Preparandose

```
Given  a LineaPedido that has an associated OrdenTrabajo in state Preparandose
When   pedido.ModificarLinea(lineaId, nuevaCantidad) is called
Then   a DomainException is thrown indicating the line is locked
```

### Scenario 08-C — Line edit allowed when OT is in Creada

```
Given  a LineaPedido with an associated OrdenTrabajo in state Creada
When   pedido.ModificarLinea(lineaId, nuevaCantidad: 3) is called
Then   the LineaPedido.Cantidad is updated to 3
And    no exception is thrown
```

### Scenario 08-D — TotalLinea computed correctly

```
Given  PrecioUnitarioSnapshot = 100m ARS, AlicuotaIVASnapshot = 0.21, Cantidad = 2
When   lineaPedido.TotalLinea is read
Then   the value is 242m  (= 200 * 1.21)
```

---

## REQ-09 — `Pedido` — dual state machines via `PedidoTransicionRegistry`

**What must be true:**

- `PedidoTransicionRegistry` is a static or singleton domain service holding all valid `(TipoPedido, EstadoOrigen, EstadoDestino, IReadOnlyList<Rol> RolesPermitidos)` rows.
- `Pedido.TransicionarEstado(EstadoPedido nuevoEstado, Rol rolUsuario)` looks up the registry, rejects invalid transitions (throws `DomainException`), rejects unauthorized roles (throws `DomainException`), then applies the transition and raises `PedidoEstadoCambiado` domain event.
- **Salon valid transitions:** `Abierto→Cerrado` (Roles: ATC, Ventas, Gerente), `Abierto→Cancelado` (Roles: ATC, Ventas, Gerente).
- **Mostrador/Delivery valid transitions:** `Creado→Modificado`, `Creado→Preparandose`, `Modificado→Preparandose`, `Preparandose→ListoParaEntregar`, `ListoParaEntregar→Entregado`, and from any non-terminal state → `Cancelado`. Roles per transition are specified in the registry data.
- No transition is valid from a terminal state (`Cerrado`, `Entregado`, `Cancelado`).

### Scenario 09-A — Valid Salon transition accepted

```
Given  a Salon Pedido in state Abierto
And    the acting user has Rol = Ventas
When   pedido.TransicionarEstado(EstadoPedido.Cerrado, Rol.Ventas) is called
Then   pedido.Estado is Cerrado
And    a PedidoEstadoCambiado domain event is raised
```

### Scenario 09-B — Invalid transition rejected

```
Given  a Salon Pedido in state Abierto
When   pedido.TransicionarEstado(EstadoPedido.Entregado, Rol.Gerente) is called
Then   a DomainException is thrown indicating the transition is not valid for Salon
```

### Scenario 09-C — Unauthorized role rejected

```
Given  a Mostrador Pedido in state Creado
When   pedido.TransicionarEstado(EstadoPedido.Preparandose, Rol.Repartidor) is called
And    Repartidor is not in the allowed roles for (Mostrador, Creado→Preparandose)
Then   a DomainException is thrown indicating insufficient role
```

### Scenario 09-D — Transition from terminal state rejected

```
Given  a Pedido in state Cancelado
When   pedido.TransicionarEstado(EstadoPedido.Creado, Rol.Gerente) is called
Then   a DomainException is thrown indicating the state is terminal
```

---

## REQ-10 — `OrdenTrabajo` — generation and state machine

**What must be true:**

- `OrdenTrabajo` is an owned entity of `Pedido` with `Guid Id`, `int NumeroOrden`, `Guid LineaPedidoId`, `Guid PlatoId` (snapshot), `int Cantidad`, nullable `LegajoId CocinaLegajo`, `EstadoOT Estado` (Creada / Preparandose / Lista / Cancelada), and `IReadOnlyList<LineaRecetaSnapshot> RecetaSnapshot` (recipe as it was at OT creation).
- OT generation is all-or-nothing: `pedido.GenerarOrdenesDeTrabajo()` validates stock availability for ALL lines before creating any OT; if any line fails, zero OTs are created and a `DomainException` is thrown.
- Duplicate OT (same `LineaPedidoId` already has an OT in a non-cancelled state) is rejected.
- When **all** OTs of a Mostrador/Delivery Pedido reach state `Lista`, the Pedido auto-advances to `ListoParaEntregar` (domain invariant, enforced inside `pedido.MarcarOTLista(ordenId)`).
- Optional cook assignment: `ordt.AsignarCocinero(LegajoId legajo)` sets `CocinaLegajo` and transitions OT to `Preparandose`.
- OTs are cancelled only via `pedido.Cancelar(...)` (never directly).

### Scenario 10-A — All-or-nothing OT creation: one unavailable line blocks all

```
Given  a Pedido with two LineaPedido (Plato A and Plato B)
And    stock is available for Plato A but not for Plato B
When   pedido.GenerarOrdenesDeTrabajo(stockChecker) is called
Then   a DomainException is thrown
And    pedido.OrdenesDeTrabajo is empty (zero OTs created)
```

### Scenario 10-B — Duplicate OT rejected

```
Given  a Pedido with an existing OrdenTrabajo for LineaPedidoId = X in state Creada
When   pedido.GenerarOrdenesDeTrabajo() attempts to create another OT for X
Then   a DomainException is thrown indicating duplicate OT
```

### Scenario 10-C — All OTs Lista triggers auto-advance

```
Given  a Mostrador Pedido in state Preparandose with two OTs (both Creada)
When   pedido.MarcarOTLista(ot1Id) and pedido.MarcarOTLista(ot2Id) are called
Then   after both calls pedido.Estado is ListoParaEntregar
```

### Scenario 10-D — Cook assignment transitions OT to Preparandose

```
Given  an OrdenTrabajo in state Creada
When   pedido.AsignarCocineroAOT(ordenId, new LegajoId("L-42")) is called
Then   the OT's Estado is Preparandose
And    CocinaLegajo is LegajoId("L-42")
```

### Scenario 10-E — Recipe snapshot is independent of later Plato changes

```
Given  an OT created when Plato P had 2 recipe lines
When   plato.AgregarLineaReceta(...) adds a third line after OT creation
Then   the OT.RecetaSnapshot still contains exactly 2 lines
```

---

## REQ-11 — Pedido cancellation and state-conditional stock restoration

**What must be true:**

- `pedido.Cancelar(Rol rolUsuario)` is the sole cancellation entry point.
- Cancellation from a terminal state (`Cerrado`, `Entregado`, `Cancelado`) throws `DomainException`.
- On cancellation, for each OT:
  - OT in state `Creada` → OT transitions to `Cancelada`; a `MovimientoStockRequested` domain event (or equivalent) is raised to signal stock restoration.
  - OT in state `Preparandose` or `Lista` → OT transitions to `Cancelada`; NO stock restoration event is raised (ingredients already consumed).
- Cancelling a Pedido that has no OTs still succeeds and transitions state to `Cancelado`.

### Scenario 11-A — Cancel with Creada OTs raises restoration event

```
Given  a Mostrador Pedido in state Creado with one OT in state Creada
When   pedido.Cancelar(Rol.Gerente) is called
Then   pedido.Estado is Cancelado
And    the OT.Estado is Cancelada
And    exactly one domain event signalling stock restoration is present in pedido.DomainEvents
```

### Scenario 11-B — Cancel with Preparandose OT does not raise restoration event

```
Given  a Mostrador Pedido with one OT in state Preparandose
When   pedido.Cancelar(Rol.Gerente) is called
Then   pedido.Estado is Cancelado
And    no stock restoration domain event is raised for that OT
```

### Scenario 11-C — Cancel from terminal state throws

```
Given  a Pedido in state Entregado
When   pedido.Cancelar(Rol.Gerente) is called
Then   a DomainException is thrown
```

---

## REQ-12 — `MovimientoStock` aggregate (append-only ledger)

**What must be true:**

- `MovimientoStock` is an aggregate root (each movement is its own root — no parent ledger entity).
- Fields: `Guid Id`, `Guid IngredienteId`, `decimal Cantidad` (signed: positive = ingress, negative = egress/reservation), `TipoMovimientoStock TipoMovimiento`, `DateTime FechaMovimiento`, nullable `Guid OrdenTrabajoId`, nullable `Guid LineaPedidoId`, nullable `string Lote`, nullable `DateOnly FechaVencimiento`, `Guid UsuarioId` (for audit).
- Valid `TipoMovimientoStock` values: `Compra`, `Consumo`, `Ajuste`, `Reserva`, `LiberacionReserva`, `DevolucionCancelacion`.
- Sign convention enforced at construction:
  - `Compra`, `Ajuste` (positive), `LiberacionReserva`, `DevolucionCancelacion`: `Cantidad > 0`.
  - `Consumo`, `Reserva`: `Cantidad < 0`.
  - `Ajuste` may be positive or negative (domain expresses it as two separate factory methods: `AjusteIngreso` and `AjusteEgreso`).
- Once created, a `MovimientoStock` is immutable (no mutation methods).
- The domain invariant **Available ≥ 0** is stated as: `Balance(ingredienteId) = Σ(Cantidad for all movements of that ingrediente) ≥ 0`. Enforcement is an infrastructure concern (optimistic/pessimistic concurrency at repository layer). The domain exposes `Balance` as a pure static projection function for testability.
- `FechaVencimiento` is modelled from day one to support the future near-expiry feature; it is nullable (not required).

### Scenario 12-A — Compra movement construction

```
Given  a call to MovimientoStock.Compra(ingredienteId, cantidad: 10m, lote: "L01", vencimiento: new DateOnly(2027,1,1), usuarioId)
When   the factory executes
Then   the resulting MovimientoStock.Cantidad is +10m
And    TipoMovimiento is Compra
And    Lote is "L01" and FechaVencimiento is 2027-01-01
```

### Scenario 12-B — Reserva movement has negative Cantidad

```
Given  a call to MovimientoStock.Reserva(ingredienteId, cantidad: 2m, lineaPedidoId, usuarioId)
When   the factory executes
Then   the resulting MovimientoStock.Cantidad is -2m
And    TipoMovimiento is Reserva
```

### Scenario 12-C — Zero cantidad rejected

```
Given  a call to MovimientoStock.Compra(ingredienteId, cantidad: 0m, ...)
When   the factory executes
Then   an ArgumentException is thrown
```

### Scenario 12-D — Balance projection

```
Given  the following movements for IngredienteId = X:
  MovimientoStock.Compra(X, 20m)
  MovimientoStock.Reserva(X, 5m)    → stored as -5m
  MovimientoStock.Consumo(X, 5m)    → stored as -5m
  MovimientoStock.LiberacionReserva(X, 5m) → stored as +5m
When   MovimientoStock.CalcularBalance(movimientos) is called with the four movements
Then   the result is 15m  (= 20 - 5 - 5 + 5)
```

### Scenario 12-E — MovimientoStock is immutable

```
Given  an existing MovimientoStock with Cantidad = -5m
When   any property setter or mutation method is called
Then   a compilation error or NotSupportedException is raised
     (properties are init-only or absent of setters)
```

---

## REQ-13 — `Factura` aggregate (polymorphic `Comprobante`)

**What must be true:**

- `Factura` is an aggregate root with `Guid Id`, `TipoComprobante TipoComprobante`, `int NumeroFactura`, `DateTime FechaAlta`, `Guid ClienteId`, `EstadoFactura Estado` (Creada / Pagada / Cancelada), `IReadOnlyList<FacturaLinea> Lineas`, `IReadOnlyList<Pago> Pagos`, `IReadOnlyList<Guid> PedidosFacturados`, nullable `string CAE` (only for `FacturaElectronica`), nullable `DateTime VencimientoCAE` (only for `FacturaElectronica`).
- Three factory methods enforce creation rules:
  - `Factura.CrearTicket(...)`: `TipoComprobante = TicketInterno`; `CAE = null`; IVA rate applied = 0.
  - `Factura.CrearFacturaConIVA(...)`: `TipoComprobante = FacturaConIVA`; `CAE = null`.
  - `Factura.CrearFacturaElectronica(...)`: `TipoComprobante = FacturaElectronica`; `CAE` is nullable at creation (not yet assigned); a `FacturaNecesitaCAE` domain event is raised.
- `CAE` can only be set on `FacturaElectronica`; calling `factura.AsignarCAE(cae, vencimiento)` on any other type throws `DomainException`.
- Lines: each `FacturaLinea` references one `LineaPedidoId`, stores a price snapshot (`Dinero PrecioUnitario`, `PorcentajeIVA Alicuota`, `int Cantidad`). Lines are added at creation time only.
- Multi-payment: a `Factura` in state `Creada` can receive N `Pago` records via `factura.RegistrarPago(Dinero monto, MetodoPago metodo, DateTime fecha)`.
- `Factura` becomes `Pagada` when `Σ(Pago.Monto) >= Total`.
- Cancellation is only allowed from `Creada`; cancelling from `Pagada` or `Cancelada` throws.
- Same-client grouping: all `PedidosFacturados` must belong to the same `ClienteId` — enforced at creation.
- Totals are computed (never stored):
  - `SubTotal = Σ(PrecioUnitario.Amount * Cantidad)` for all lines.
  - `TotalIVA = Σ(PrecioUnitario.Amount * Cantidad * Alicuota.Value)` grouped by aliquot.
  - `Total = SubTotal + TotalIVA`.

### Scenario 13-A — CAE assignment blocked on TicketInterno

```
Given  a Factura created via CrearTicket(...)
When   factura.AsignarCAE("12345678901234", vencimiento) is called
Then   a DomainException is thrown
```

### Scenario 13-B — FacturaNecesitaCAE event raised on electronic invoice creation

```
Given  a call to Factura.CrearFacturaElectronica(...)
When   the factory completes
Then   factura.DomainEvents contains one FacturaNecesitaCAE event
```

### Scenario 13-C — Invoice becomes Pagada when payments cover total

```
Given  a Factura with Total = 1000m ARS and no payments
When   factura.RegistrarPago(new Dinero(600m, "ARS"), MetodoPago.Efectivo, now) is called
And    factura.RegistrarPago(new Dinero(400m, "ARS"), MetodoPago.TarjetaCredito, now) is called
Then   factura.Estado is Pagada
```

### Scenario 13-D — Partial payment does not mark as Pagada

```
Given  a Factura with Total = 1000m ARS
When   factura.RegistrarPago(new Dinero(500m, "ARS"), MetodoPago.Efectivo, now) is called
Then   factura.Estado is still Creada
```

### Scenario 13-E — Cancel from Pagada throws

```
Given  a Factura in state Pagada
When   factura.Cancelar() is called
Then   a DomainException is thrown
```

### Scenario 13-F — Totals computed from line snapshots

```
Given  a Factura with two lines:
  Line 1: PrecioUnitario = 100m ARS, Alicuota = 0.21, Cantidad = 2
  Line 2: PrecioUnitario = 200m ARS, Alicuota = 0.105, Cantidad = 1
When   factura.SubTotal, factura.TotalIVA, factura.Total are read
Then   SubTotal = 400m
And    TotalIVA = 200*0.21 + 200*0.105 = 42m + 21m = 63m    (approx — exact by decimal arithmetic)
And    Total = 463m
```

### Scenario 13-G — Multi-client grouping rejected

```
Given  two PedidoIds from different clients (ClienteId A and ClienteId B)
When   Factura.CrearTicket(clienteId: A, pedidoIds: [pedidoDeA, pedidoDeB], ...) is called
Then   a DomainException is thrown indicating mismatched client
```

---

## REQ-14 — `Mesa` aggregate

**What must be true:**

- `Mesa` is an aggregate root with `Guid Id`, `int NumeroMesa`, `int Capacidad` (> 0), nullable `string Zona`, `bool Activo`, nullable `Guid ActivePedidoId`, and `byte[] ConcurrencyToken`.
- `Capacidad` must be ≥ 1; zero or negative is rejected.
- A `Mesa` can only have one open (`Salon`, non-terminal) `Pedido` at a time; attempting to assign a second throws `DomainException`.
- Soft-delete via `mesa.Desactivar()`; a Mesa with an active Pedido cannot be deactivated.

### Scenario 14-A — Zero capacity rejected

```
Given  a CreateMesa command with Capacidad = 0
When   new Mesa(...) is called
Then   an ArgumentException is thrown
```

### Scenario 14-B — Second open Pedido assignment rejected

```
Given  a Mesa with ActivePedidoId = some Guid
When   mesa.AsignarPedido(nuevoId) is called
Then   a DomainException is thrown indicating the table is occupied
```

### Scenario 14-C — Deactivation blocked when Pedido is active

```
Given  a Mesa with ActivePedidoId != null
When   mesa.Desactivar() is called
Then   a DomainException is thrown
```

---

## REQ-15 — Domain events contract

**What must be true:**

- All domain events implement a common marker interface `IDomainEvent` (or base record).
- Domain events are raised by aggregate roots and stored in a `IReadOnlyList<IDomainEvent> DomainEvents` collection on the root.
- Events are cleared by the infrastructure dispatcher after publishing; the domain does NOT clear them.
- Minimum set of events defined in this change:

| Event | Raised by | Key fields |
|---|---|---|
| `PedidoCreado` | `Pedido.Crear` | PedidoId, TipoPedido, ClienteId |
| `LineaPedidoAgregada` | `pedido.AgregarLinea` | PedidoId, LineaPedidoId, PlatoId |
| `OrdenTrabajoCreada` | `pedido.GenerarOrdenesDeTrabajo` | PedidoId, OrdenTrabajoId, PlatoId |
| `PedidoEstadoCambiado` | `pedido.TransicionarEstado` | PedidoId, EstadoAnterior, EstadoNuevo, Rol |
| `FacturaNecesitaCAE` | `Factura.CrearFacturaElectronica` | FacturaId, ClienteId |

### Scenario 15-A — PedidoCreado raised at creation

```
Given  a valid call to Pedido.Crear(tipoPedido, clienteId, ...)
When   the factory completes
Then   pedido.DomainEvents contains exactly one PedidoCreado event
And    the event carries the correct PedidoId and ClienteId
```

### Scenario 15-B — PedidoEstadoCambiado raised on transition

```
Given  a valid state transition call
When   pedido.TransicionarEstado(destino, rol) succeeds
Then   pedido.DomainEvents contains a PedidoEstadoCambiado event
And    the event's EstadoAnterior matches the previous state
And    the event's EstadoNuevo matches the new state
```

---

## REQ-16 — Soft-delete semantics (cross-cutting)

**What must be true:**

- The following aggregates support soft-delete: `Cliente`, `Ingrediente`, `Plato`, `Menu`, `Mesa`.
- Soft-delete is represented by a boolean `Activo` flag set to `false`.
- A deleted entity is never physically removed from the domain model; it remains queryable.
- Domain layer does NOT filter soft-deleted entities from collections — that is a repository/query concern.
- Soft-delete is idempotent (calling `Desactivar()` on an already-inactive entity does not throw).

### Scenario 16-A — Desactivar is idempotent

```
Given  a Cliente with Activo = false (already deactivated)
When   cliente.Desactivar() is called again
Then   no exception is thrown
And    cliente.Activo remains false
```

---

## REQ-17 — Ubiquitous language enforcement

**What must be true:**

- All domain types (classes, records, enums, interfaces, methods, properties) use Spanish nouns matching the glossary in `docs/functional-scope.md`.
- No English domain nouns appear in `GastroGestion.Domain` (English is permitted for C# language keywords, framework type names, and technical concepts: `interface`, `record`, `Guid`, `DateTime`, etc.).
- The following Spanish enum value sets are defined:
  - `TipoPedido`: `Salon`, `Mostrador`, `Delivery`
  - `EstadoPedido`: `Abierto`, `Cerrado`, `Creado`, `Modificado`, `Preparandose`, `ListoParaEntregar`, `Entregado`, `Cancelado`
  - `EstadoOT`: `Creada`, `Preparandose`, `Lista`, `Cancelada`
  - `EstadoFactura`: `Creada`, `Pagada`, `Cancelada`
  - `EstadoFacturaPedido`: `NoFacturado`, `Facturado`, `Pagado`
  - `CondicionIVA`: `ResponsableInscripto`, `Monotributo`, `ConsumidorFinal`, `Exento`
  - `AlicuotaIVA`: `Cero`, `DiezYMedioPorciento`, `VeintiUnPorciento`, `VeintisietePorciento`
  - `UnidadDeMedida`: `Kg`, `Gr`, `Lt`, `Ml`, `Unidad`
  - `TipoMovimientoStock`: `Compra`, `Consumo`, `Ajuste`, `Reserva`, `LiberacionReserva`, `DevolucionCancelacion`
  - `TipoComprobante`: `TicketInterno`, `FacturaConIVA`, `FacturaElectronica`
  - `MetodoPago`: `Transferencia`, `Efectivo`, `TarjetaCredito`, `TarjetaDebito`, `ContraEntrega`
  - `EstadoMesa`: (represented by `Mesa.Activo` bool — no separate enum)
  - `Rol`: `Gerente`, `ATC`, `Ventas`, `Finanzas`, `Produccion`, `Almacenes`, `Repartidor`

### Scenario 17-A — No English domain nouns in Domain assembly

```
Given  all .cs files under src/GastroGestion.Domain/ are scanned
When   class, record, enum, and interface names are listed
Then   none of them use English domain nouns (e.g. "Order", "Invoice", "Product", "Customer")
     instead of the Spanish equivalents ("Pedido", "Factura", "Plato", "Cliente")
```

---

## REQ-18 — Domain layer test coverage baseline

**What must be true:**

- `GastroGestion.Domain.Tests` contains at minimum one xUnit test class per aggregate root.
- Each test class exercises the invariants defined in REQ-02 through REQ-16.
- All tests pass with `dotnet test`.
- Tests have zero dependencies on EF Core, ASP.NET Core, or any infrastructure package.

### Scenario 18-A — Domain tests pass in isolation

```
Given  the .NET 8 SDK is installed
When   `dotnet test tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj` is executed
Then   the command exits with code 0
And    all test cases are reported as passed
And    no test references infrastructure packages
```

---

## Dependency map (spec cross-reference)

```
REQ-01 (zero deps)
  └── REQ-02 (value objects)
        ├── REQ-03 (Cliente + Direccion)
        ├── REQ-04 (Ingrediente)
        ├── REQ-05 (Plato + LineaReceta)
        ├── REQ-06 (Menu + MenuItem)
        │     └── REQ-07 (Pedido creation)
        │           ├── REQ-08 (line management + price snapshot)
        │           ├── REQ-09 (state machines + registry)
        │           ├── REQ-10 (OT generation)
        │           └── REQ-11 (cancellation + stock restoration)
        ├── REQ-12 (MovimientoStock ledger)
        └── REQ-13 (Factura polymorphic)
REQ-14 (Mesa) ← depends on REQ-07 (Pedido reference)
REQ-15 (domain events) ← cross-cutting, raised by REQ-07/REQ-10/REQ-13
REQ-16 (soft-delete) ← cross-cutting
REQ-17 (ubiquitous language) ← cross-cutting
REQ-18 (test coverage) ← depends on all of the above
```
