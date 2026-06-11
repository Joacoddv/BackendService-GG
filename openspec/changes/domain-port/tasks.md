# Tasks — domain-port

**Generated:** 2026-06-10  
**Artifact store:** openspec + engram  
**Change:** domain-port  
**Phase:** 2 of 7, GastroGestion .NET 8 strangler roadmap  
**Delivery strategy:** ask-on-risk  

---

## Dependency order

```
SLICE 1 — Catalogue
  DP-01 (zero-dep .csproj gate — Slice 1)   ← independent; must be verified first
    └── DP-02 (Common kernel)
          └── DP-03 (ValueObjects + Enums)
                └── DP-04 (Cliente + Direccion aggregates)
                      ├── DP-05 (Ingrediente aggregate)
                      ├── DP-06 (Plato + LineaReceta aggregates)
                      │     └── DP-07 (Menu + MenuItem aggregates)
                      └── DP-08 (Mesa aggregate)
                            └── DP-09 (Slice 1 domain tests — one xUnit class per aggregate)
                                  └── DP-10 (Slice 1 build + test verification)

SLICE 2 — Transactional  (depends on Slice 1 being green)
  DP-11 (zero-dep .csproj gate — Slice 2 checkpoint)
    └── DP-12 (DireccionEntrega VO + PedidoTransicionRegistry)
          └── DP-13 (Pedido + LineaPedido aggregates)
                └── DP-14 (OrdenTrabajo + LineaRecetaSnapshot)
                      └── DP-15 (IEfectivoPrecioService contract + domain events S2)
                            └── DP-16 (Slice 2 domain tests)
                                  └── DP-17 (Slice 2 build + test verification)

SLICE 3 — Fiscal  (depends on Slice 1 + 2 being green)
  DP-18 (zero-dep .csproj gate — Slice 3 checkpoint)
    └── DP-19 (MovimientoStock ledger aggregate)
          └── DP-20 (Factura TPH aggregate + FacturaLinea + Pago)
                └── DP-21 (ICalculadorFactura contract + FacturaNecesitaCAE event)
                      └── DP-22 (Slice 3 domain tests)
                            └── DP-23 (Slice 3 build + test verification)
```

Within each slice, DP-05/DP-06 and DP-08 can be developed in parallel after DP-04 lands. All other tasks within a slice are strictly sequential. Slices 1→2→3 are strictly ordered: a later slice's first task MUST NOT start until the previous slice's verification task passes.

---

## SLICE 1 — Catalogue

Covers: REQ-01, REQ-02, REQ-03, REQ-04, REQ-05, REQ-06 (partial), REQ-14, REQ-16, REQ-17, REQ-18 (partial)  
Design sections: §1 (project layout), §2 (aggregates), §3 (VOs), §4 (Direccion dual nature), §9 (RowVersion on Mesa)

---

### DP-01 — Zero-dependency .csproj gate (Slice 1) [ ]

**Work unit:** Verification-only — no new source code.  
**Conventional commit:** N/A — gate check before committing any source.

#### What to verify

```powershell
# GastroGestion.Domain.csproj must have ZERO PackageReference AND ZERO ProjectReference
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" -Pattern "PackageReference|ProjectReference"
# Expected: no matches

# TFM must be net8.0
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" -Pattern "net8.0"
# Expected: one match
```

If any reference is found, stop immediately — do NOT proceed to DP-02 until the reference is removed. This gate is re-checked after EVERY slice.

#### Spec requirements satisfied

- **REQ-01** (Scenario 01-A) — zero outward dependencies on domain project.

---

### DP-02 — Common kernel (Entity, AggregateRoot, ValueObject, IDomainEvent, DomainException) [ ]

**Work unit:** One commit — foundational abstractions only; no concrete types yet.  
**Conventional commit:** `feat(domain): add Common kernel abstractions`

#### What to do

Create `src/GastroGestion.Domain/Common/`:

1. **`Entity.cs`** — abstract base with `Guid Id` (set in ctor); equality by Id (`Equals`/`GetHashCode` override); protected ctor guards `Id != Guid.Empty`.

2. **`AggregateRoot.cs`** — extends `Entity`; private `List<IDomainEvent> _domainEvents`; protected `AddDomainEvent(IDomainEvent)`; public `IReadOnlyList<IDomainEvent> DomainEvents` property; public `ClearDomainEvents()` method.

3. **`ValueObject.cs`** — abstract base; abstract `IEnumerable<object> GetEqualityComponents()`; structural `Equals`/`GetHashCode` derived from components; no `Id`.

4. **`IDomainEvent.cs`** — marker interface; `DateTime OccurredOn { get; }`.

5. **`DomainException.cs`** — sealed class extending `Exception`; single `string message` ctor; no inner exception or error codes in v1.

Namespace for all: `GastroGestion.Domain.Common`.

#### Must NOT do

- Do not add any NuGet packages.
- Do not add any domain-specific types (aggregates, VOs, enums) in this task.
- Do not add MediatR, bus, or dispatch concerns.

#### Verification

```powershell
# All 5 files exist
Get-ChildItem "src/GastroGestion.Domain/Common/" | Select-Object Name
# Expected: Entity.cs, AggregateRoot.cs, ValueObject.cs, IDomainEvent.cs, DomainException.cs

# Project still has zero outward dependencies
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" -Pattern "PackageReference|ProjectReference"
# Expected: no matches

# Compiles
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0, no errors
```

#### Spec requirements satisfied

- **REQ-15** (Scenario 15-A) — `IDomainEvent` marker interface present.
- **REQ-18** (Scenario 18-A partial) — domain project compiles without infra deps.
- Design §1 — Common/ kernel established.

---

### DP-03 — ValueObjects and Enums [ ]

**Work unit:** One commit — all value objects and enums; no aggregate code yet.  
**Conventional commit:** `feat(domain): add value objects and domain enums`

#### What to do

Create `src/GastroGestion.Domain/ValueObjects/`:

1. **`Dinero.cs`** — extends `ValueObject`; `decimal Monto`; `Moneda Moneda` (default `ARS`); validate `Monto >= 0` in ctor (throw `DomainException`); methods: `Sumar(Dinero)`, `Restar(Dinero)`, `Multiplicar(decimal)`, `AplicarIVA(PorcentajeIVA) → Dinero` (returns IVA amount only), `ConIVA(PorcentajeIVA) → Dinero` (returns Monto + IVA); currency-mix guard in Sumar/Restar (throw if `Moneda` differs).

2. **`Cuit.cs`** — extends `ValueObject`; `string Valor`; validate 11 digits + CUIT check-digit algorithm in ctor; `ToString()` formats as `##-########-#`.

3. **`Email.cs`** — extends `ValueObject`; `string Valor`; validate format (must contain `@` and a domain part) in ctor; normalize to lowercase.

4. **`Cantidad.cs`** — extends `ValueObject`; `decimal Valor`; `UnidadDeMedida Unidad`; validate `Valor > 0` in ctor; NO silent unit conversion between different `UnidadDeMedida` values (throw `DomainException` on arithmetic with mismatched units).

5. **`PorcentajeIVA.cs`** — extends `ValueObject`; wraps `AlicuotaIVA` enum; readonly property `decimal Tasa` derived from enum; closed set `{0m, 0.105m, 0.21m, 0.27m}`; static factory `Cero` returning `AlicuotaIVA.Exento`.

6. **`LegajoId.cs`** — extends `ValueObject`; `Guid Valor`; validate not empty.

Create `src/GastroGestion.Domain/Enums/`:

7. **`Moneda.cs`** — `enum Moneda { ARS }` (ARS only for v1; seam for future extensions).

8. **`AlicuotaIVA.cs`** — `enum AlicuotaIVA { Exento = 0, ReducidoA = 1, General = 2, Diferencial = 3 }` (maps to Tasa 0/0.105/0.21/0.27 in PorcentajeIVA).

9. **`UnidadDeMedida.cs`** — `enum UnidadDeMedida { Gramo, Kilogramo, Mililitro, Litro, Unidad, Porcion }`.

10. **`CondicionIVA.cs`** — `enum CondicionIVA { ResponsableInscripto, Monotributista, ConsumidorFinal, ExentoIVA }`.

11. **`TipoPedido.cs`** — `enum TipoPedido { Salon, TakeAway, Delivery }`.

12. **`EstadoPedido.cs`** — `enum EstadoPedido { Abierto, Creado, Modificado, Preparandose, ListoParaEntregar, Entregado, Cerrado, Cancelado }`.

13. **`EstadoMesa.cs`** — `enum EstadoMesa { Libre, Ocupada, Reservada }`.

14. **`TipoMovimientoStock.cs`** — `enum TipoMovimientoStock { Compra, Consumo, Ajuste, Reserva, LiberacionReserva, DevolucionCancelacion }`.

15. **`TipoComprobante.cs`** — `enum TipoComprobante { TicketInterno, FacturaConIVA, FacturaElectronica }`.

16. **`RolUsuario.cs`** — `enum RolUsuario { Administrador, Cajero, Mozo, Cocinero }`.

#### Must NOT do

- Do not add any aggregate or entity types.
- Do not reference any namespace outside `GastroGestion.Domain`.

#### Verification

```powershell
# All VO files exist
Get-ChildItem "src/GastroGestion.Domain/ValueObjects/" | Measure-Object | Select-Object -ExpandProperty Count
# Expected: 6

# All Enum files exist
Get-ChildItem "src/GastroGestion.Domain/Enums/" | Measure-Object | Select-Object -ExpandProperty Count
# Expected: 10

# Project compiles with zero dependencies
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0

# Zero outward dependencies still holds
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" -Pattern "PackageReference|ProjectReference"
# Expected: no matches
```

#### Spec requirements satisfied

- **REQ-02** (Scenarios 02-A through 02-E) — Dinero, Cuit, Email, Cantidad, PorcentajeIVA validation rules.
- **REQ-17** (Scenario 17-A) — ubiquitous language enum values in Spanish as per spec.
- Design §3 — all VOs specified; §2 enum dependencies resolved.

---

### DP-04 — Cliente aggregate + Direccion owned entity [ ]

**Work unit:** One commit.  
**Conventional commit:** `feat(domain): add Cliente aggregate with Direccion owned entity`

#### What to do

Create `src/GastroGestion.Domain/Clientes/`:

1. **`Direccion.cs`** — extends `Entity`; owned by Cliente; properties: `string Calle`, `string Numero`, `string? Piso`, `string? Departamento`, `string Ciudad`, `string Provincia`, `string CodigoPostal`; no business invariants beyond non-empty Calle/Ciudad.

2. **`Cliente.cs`** — extends `AggregateRoot`; factory method `static Cliente Crear(string nombre, CondicionIVA condicionIVA, Cuit? cuit, Email? email)` — validates: `nombre` not null/empty (throw `DomainException`); if `CondicionIVA` is `ResponsableInscripto`, `cuit` must be non-null (throw `DomainException`); assigns immutable `NumeroCliente` (use `Guid.NewGuid()` as surrogate in v1 — uniqueness is infra); `bool Activo = true`; private `List<Direccion> _direcciones`; public `IReadOnlyList<Direccion> Direcciones` property; methods: `AgregarDireccion(Direccion)`, `EliminarDireccion(Guid direccionId)`, `Desactivar()` — idempotent (no throw if already inactive), sets `Activo = false`.

#### Must NOT do

- Do not add domain-level filtering by `Activo` flag (REQ-16 — filtering is infrastructure concern).
- Do not enforce CUIT uniqueness (infra concern per design §2).
- Do not add navigation properties or EF attributes.

#### Verification

```powershell
# Files exist
Test-Path "src/GastroGestion.Domain/Clientes/Cliente.cs"
Test-Path "src/GastroGestion.Domain/Clientes/Direccion.cs"

# Compiles
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0

# Zero deps gate
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" -Pattern "PackageReference|ProjectReference"
# Expected: no matches
```

#### Spec requirements satisfied

- **REQ-03** (Scenarios 03-A through 03-F) — Cliente creation rules, CUIT/CondicionIVA validation, Direccion[], soft-delete, NumeroCliente immutability.
- **REQ-16** (Scenarios 16-A, 16-B) — Activo flag; idempotent Desactivar.
- Design §2 (Cliente), §4 (Direccion dual nature — entity form established here).

---

### DP-05 — Ingrediente aggregate [ ]

**Work unit:** One commit — can be developed in parallel with DP-06 once DP-04 is committed.  
**Conventional commit:** `feat(domain): add Ingrediente aggregate`

#### What to do

Create `src/GastroGestion.Domain/Ingredientes/`:

1. **`Ingrediente.cs`** — extends `AggregateRoot`; factory `static Ingrediente Crear(string nombre, UnidadDeMedida unidadBase)` — validates `nombre` not null/empty; `bool Activo = true`; method `Desactivar()` — idempotent. No owned entities. Uniqueness of name is an infrastructure concern.

#### Must NOT do

- Do not add stock tracking or quantity fields — stock lives in MovimientoStock (Slice 3).

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Ingredientes/Ingrediente.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-04** (Scenarios 04-A through 04-D) — creation rules, UnidadDeMedida, soft-delete, unique-name intent (infra).
- **REQ-16** (Scenario 16-A) — Activo/Desactivar pattern.

---

### DP-06 — Plato aggregate + LineaReceta owned entity [ ]

**Work unit:** One commit — can be developed in parallel with DP-05 once DP-04 is committed.  
**Conventional commit:** `feat(domain): add Plato aggregate with LineaReceta recipe lines`

#### What to do

Create `src/GastroGestion.Domain/Platos/`:

1. **`LineaReceta.cs`** — extends `Entity`; owned by Plato; properties: `Guid IngredienteId` (cross-boundary ref), `Cantidad Cantidad`, `Guid? PlatoReferenciadoId` (nullable, null in v1 — sub-recipe seam per design §2).

2. **`Plato.cs`** — extends `AggregateRoot`; factory `static Plato Crear(string nombre, Dinero precioBase, AlicuotaIVA alicuotaIVA)` — validates `nombre` not null/empty, `precioBase.Monto >= 0`; `bool Activo = true`; private `List<LineaReceta> _lineasReceta`; public `IReadOnlyList<LineaReceta> LineasReceta`; methods: `AgregarLineaReceta(Guid ingredienteId, Cantidad cantidad)`, `EliminarLineaReceta(Guid lineaRecetaId)`, `ActualizarPrecio(Dinero nuevoPrecio)`, `Desactivar()` — idempotent.

#### Must NOT do

- Do not validate unit compatibility between LineaReceta and Ingrediente (needs Ingrediente load — app-level per design §2).

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Platos/Plato.cs"
Test-Path "src/GastroGestion.Domain/Platos/LineaReceta.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-05** (Scenarios 05-A through 05-E) — PrecioBase Dinero, AlicuotaIVA, LineaReceta Cantidad, sub-recipe seam (nullable PlatoReferenciadoId).
- **REQ-16** — Desactivar pattern.
- Design §2 (Plato/LineaReceta).

---

### DP-07 — Menu aggregate + MenuItem owned entity [ ]

**Work unit:** One commit — depends on DP-06 (Plato exists for cross-ref).  
**Conventional commit:** `feat(domain): add Menu aggregate with MenuItem owned entity`

#### What to do

Create `src/GastroGestion.Domain/Menus/`:

1. **`MenuItem.cs`** — extends `Entity`; owned by Menu; properties: `Guid PlatoId` (cross-boundary ref), `Dinero? PrecioOverride` (nullable — null means use PrecioBase).

2. **`Menu.cs`** — extends `AggregateRoot`; factory `static Menu Crear(string nombre, DateOnly fechaVigencia)` — validates: `nombre` not null/empty; `fechaVigencia` must be in the future relative to `DateOnly.FromDateTime(DateTime.UtcNow)` (throw `DomainException` with message "FechaVigencia must be a future date"); `bool Activo = true`; private `List<MenuItem> _items`; public `IReadOnlyList<MenuItem> Items`; methods: `AgregarItem(Guid platoId, Dinero? precioOverride)` — if `precioOverride` is non-null, validate `precioOverride.Monto >= 0`; `EliminarItem(Guid menuItemId)`.

Note: `IEfectivoPrecioService` contract is defined in Slice 2 (DP-15) because it crosses into Pedido/LineaPedido logic. Menu only stores the override value; effective price resolution is application-layer responsibility.

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Menus/Menu.cs"
Test-Path "src/GastroGestion.Domain/Menus/MenuItem.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-06** (Scenarios 06-A through 06-D) — future-date rule, price override validation, IEfectivoPrecioService seam established (contract deferred to DP-15).
- Design §2 (Menu/MenuItem), §5c (price snapshot contract).

---

### DP-08 — Mesa aggregate [ ]

**Work unit:** One commit — can be developed in parallel with DP-05/DP-06 once DP-04 is committed.  
**Conventional commit:** `feat(domain): add Mesa aggregate`

#### What to do

Create `src/GastroGestion.Domain/Mesas/`:

1. **`Mesa.cs`** — extends `AggregateRoot`; factory `static Mesa Crear(int numero, int capacidad)` — validates `capacidad > 0` (throw `DomainException`), `numero > 0`; `EstadoMesa Estado = EstadoMesa.Libre`; `bool Activa = true`; `Guid? PedidoActivoId = null`; `byte[] RowVersion = Array.Empty<byte>()` (plain property — EF config in phase 3, per design §9); methods:
   - `AsignarPedido(Guid pedidoId)` — throws if `PedidoActivoId != null` (one open Pedido invariant); sets `PedidoActivoId`, `Estado = EstadoMesa.Ocupada`.
   - `LiberarPedido()` — sets `PedidoActivoId = null`, `Estado = EstadoMesa.Libre`.
   - `Desactivar()` — throws `DomainException` if `PedidoActivoId != null` (deactivation guard); idempotent if already inactive; sets `Activa = false`.

#### Must NOT do

- Do not add `[Timestamp]` or `[ConcurrencyCheck]` attributes (EF config is phase 3, per design §9).

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Mesas/Mesa.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-14** (Scenarios 14-A through 14-D) — Capacidad > 0, one-open-Pedido invariant, deactivation guard, EstadoMesa transitions.
- Design §2 (Mesa), §9 (RowVersion plain property).

---

### DP-09 — Slice 1 domain tests (one xUnit class per aggregate) [ ]

**Work unit:** One commit — tests ship with the code they verify (work-unit-commits rule).  
**Conventional commit:** `test(domain): add Slice 1 catalogue aggregate tests`

#### What to do

Target project: `tests/GastroGestion.Domain.Tests/`

Create one test class per aggregate/VO group:

1. **`ClienteTests.cs`** — covers REQ-03 scenarios:
   - `Crear_WithValidData_CreatesActiveCliente`
   - `Crear_WithEmptyNombre_ThrowsDomainException`
   - `Crear_ResponsableInscripto_WithoutCuit_ThrowsDomainException`
   - `Desactivar_ActiveCliente_SetsActivoFalse`
   - `Desactivar_AlreadyInactive_IsIdempotent`
   - `AgregarDireccion_AddsToList`

2. **`IngredienteTests.cs`** — covers REQ-04:
   - `Crear_WithValidData_CreatesActiveIngrediente`
   - `Crear_WithEmptyNombre_ThrowsDomainException`
   - `Desactivar_SetsActivoFalse`
   - `Desactivar_AlreadyInactive_IsIdempotent`

3. **`PlatoTests.cs`** — covers REQ-05:
   - `Crear_WithValidData_CreatesPlato`
   - `AgregarLineaReceta_AddsToRecipe`
   - `EliminarLineaReceta_RemovesFromRecipe`
   - `ActualizarPrecio_UpdatesPrecioBase`
   - `LineaReceta_PlatoReferenciadoId_IsNullByDefault` (sub-recipe seam)

4. **`MenuTests.cs`** — covers REQ-06:
   - `Crear_WithFutureDate_CreatesMenu`
   - `Crear_WithPastDate_ThrowsDomainException`
   - `AgregarItem_WithPriceOverride_ValidatesNonNegative`
   - `AgregarItem_WithNullOverride_IsPermitted`

5. **`MesaTests.cs`** — covers REQ-14:
   - `Crear_WithPositiveCapacidad_CreatesMesa`
   - `Crear_WithZeroCapacidad_ThrowsDomainException`
   - `AsignarPedido_WhenLibre_AssignsPedidoAndSetsOcupada`
   - `AsignarPedido_WhenAlreadyOcupada_ThrowsDomainException`
   - `Desactivar_WhenPedidoActivo_ThrowsDomainException`
   - `Desactivar_WhenLibre_SetsActivaFalse`

6. **`ValueObjectTests.cs`** — covers REQ-02:
   - `Dinero_WithNegativeMonto_ThrowsDomainException`
   - `Dinero_Sumar_DifferentMoneda_ThrowsDomainException`
   - `Dinero_ConIVA_AppliesCorrectly` (spot-check 21%)
   - `Cuit_InvalidFormat_ThrowsDomainException`
   - `Cuit_ValidCuit_FormatsCorrectly`
   - `Email_InvalidFormat_ThrowsDomainException`
   - `Cantidad_ZeroValor_ThrowsDomainException`
   - `PorcentajeIVA_Cero_ReturnsZeroTasa`

All test classes must use only `GastroGestion.Domain.*` and `xunit` namespaces — zero infrastructure dependencies.

#### Verification

```powershell
dotnet test tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj
# Expected: all tests pass, 0 failed, 0 skipped

# Confirm zero infra deps in test project (REQ-18)
Select-String -Path "tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj" -Pattern "EntityFramework|Infrastructure|Moq"
# Expected: no matches
```

#### Spec requirements satisfied

- **REQ-18** (Scenarios 18-A through 18-G) — one xUnit class per aggregate, zero infra deps.
- **REQ-02, REQ-03, REQ-04, REQ-05, REQ-06, REQ-14** — acceptance scenario coverage for Slice 1 types.

---

### DP-10 — Slice 1 build + test verification (no code changes) [ ]

**Work unit:** Verification-only — no commits. Confirms Slice 1 is shippable.

#### Verification commands

```powershell
# Full domain project build
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0, no warnings treated as errors

# All Slice 1 tests green
dotnet test tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj --logger "console;verbosity=detailed"
# Expected: all pass

# Zero-dependency gate (final Slice 1 check)
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" -Pattern "PackageReference|ProjectReference"
# Expected: no matches

# Namespace-to-folder alignment spot-check
Select-String -Path "src/GastroGestion.Domain/Clientes/Cliente.cs" -Pattern "namespace GastroGestion.Domain.Clientes"
# Expected: one match
```

---

## SLICE 2 — Transactional

Covers: REQ-07, REQ-08, REQ-09, REQ-10, REQ-11, REQ-15, REQ-18 (continued)  
Design sections: §2 (Pedido/OT aggregates), §4 (DireccionEntrega VO), §5c (IEfectivoPrecioService), §5d (PedidoTransicionRegistry), §8 (domain events), §9 (RowVersion on Pedido)  
**Prerequisite:** DP-10 must pass before DP-11 begins.

---

### DP-11 — Zero-dependency .csproj gate (Slice 2 checkpoint) [ ]

**Work unit:** Verification-only gate — re-run DP-01 verification commands.  
**Conventional commit:** N/A

Repeat the same check from DP-01. Any new dependency introduced since Slice 1 is a blocking issue.

#### Spec requirements satisfied

- **REQ-01** (Scenario 01-A) — zero deps maintained after Slice 1 additions.

---

### DP-12 — DireccionEntrega VO + PedidoTransicionRegistry [ ]

**Work unit:** One commit — foundational Slice 2 types that Pedido depends on.  
**Conventional commit:** `feat(domain): add DireccionEntrega VO and PedidoTransicionRegistry`

#### What to do

1. **`DireccionEntrega.cs`** (in `src/GastroGestion.Domain/ValueObjects/`) — extends `ValueObject`; frozen snapshot VO (no identity); properties: `string Calle`, `string Numero`, `string? Piso`, `string? Departamento`, `string Ciudad`, `string Provincia`, `string CodigoPostal`; validate `Calle` and `Ciudad` not null/empty in ctor. This is distinct from `Direccion` entity (design §4 — dual nature resolved).

2. **`PedidoTransicion.cs`** (in `src/GastroGestion.Domain/Pedidos/`) — record or immutable class: `TipoPedido Tipo`, `EstadoPedido Desde`, `EstadoPedido Hasta`, `RolUsuario[] RolesPermitidos`.

3. **`PedidoTransicionRegistry.cs`** (in `src/GastroGestion.Domain/Pedidos/`) — static class with `static IReadOnlyList<PedidoTransicion> Transiciones` property populated at static init; includes ALL valid transitions per design §5d:
   - Salon: `Abierto→Cerrado` (Administrador, Mozo), `Abierto→Cancelado` (Administrador, Mozo)
   - Mostrador/Delivery (applies to TakeAway and Delivery via matching TipoPedido): `Creado→Modificado`, `Creado→Preparandose`, `Creado→Cancelado`, `Modificado→Preparandose`, `Modificado→Cancelado`, `Preparandose→ListoParaEntregar`, `Preparandose→Cancelado`, `ListoParaEntregar→Entregado`
   - Each row has explicit `RolesPermitidos[]` per design §5d.
   - Static method `bool EsValida(TipoPedido, EstadoPedido desde, EstadoPedido hasta, RolUsuario rol)`.

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/ValueObjects/DireccionEntrega.cs"
Test-Path "src/GastroGestion.Domain/Pedidos/PedidoTransicionRegistry.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-07** (Scenario 07-D) — frozen DireccionEntrega VO (delivery address snapshot).
- **REQ-09** (Scenarios 09-A through 09-D) — role-gated registry; terminal-state guard data ready; NO switch on state.
- Design §4, §5d.

---

### DP-13 — Pedido aggregate + LineaPedido owned entity [ ]

**Work unit:** One commit.  
**Conventional commit:** `feat(domain): add Pedido aggregate with LineaPedido`

#### What to do

Create `src/GastroGestion.Domain/Pedidos/`:

1. **`LineaPedido.cs`** — extends `Entity`; owned by Pedido; properties: `Guid PlatoId`, `int Cantidad` (units ordered, validate > 0), `Dinero? PrecioSnapshot` (null until confirmed), `PorcentajeIVA? IVASnapshot` (null until confirmed), `bool PrecioConfirmado = false`; method: `ConfirmarPrecio(Dinero precio, PorcentajeIVA iva)` — throws `DomainException` if `PrecioConfirmado` is already true (set-once invariant); sets `PrecioSnapshot`, `IVASnapshot`, `PrecioConfirmado = true`; computed property `Dinero? Subtotal` — returns `PrecioSnapshot?.Multiplicar(Cantidad)` (null if not confirmed); computed `Dinero? SubtotalConIVA` — returns `Subtotal?.ConIVA(IVASnapshot!)`.

2. **`Pedido.cs`** — extends `AggregateRoot`; factory `static Pedido Crear(TipoPedido tipo, Guid clienteId, Guid? mesaId, DireccionEntrega? direccionEntrega)`:
   - Validate `tipo == TipoPedido.Salon` → `mesaId` must be non-null (throw `DomainException`).
   - Validate `tipo == TipoPedido.Delivery` → `direccionEntrega` must be non-null (throw `DomainException`).
   - `TakeAway` (Mostrador): neither `mesaId` nor `direccionEntrega` required.
   - Initial `EstadoPedido`: `Salon → Abierto`, `TakeAway/Delivery → Creado`.
   - `byte[] RowVersion = Array.Empty<byte>()` (plain property, per design §9).
   - Raise `PedidoCreado` domain event.
   - Private `List<LineaPedido> _lineas`; `IReadOnlyList<LineaPedido> Lineas`.
   - Private `List<OrdenTrabajo> _ordenesTrabajo`; `IReadOnlyList<OrdenTrabajo> OrdenesTrabajo` (populated in DP-14).
   - Methods:
     - `AgregarLinea(Guid platoId, int cantidad)` — throws if `PrecioConfirmado` on ANY existing line (edit-lock rule: once any price is confirmed, the order is locked for line edits); raises `LineaPedidoAgregada` event.
     - `TransicionarEstado(EstadoPedido nuevoEstado, RolUsuario rol)` — calls `PedidoTransicionRegistry.EsValida`; throws `DomainException` on invalid transition or unauthorized role; raises `PedidoEstadoCambiado` event.
     - `Cancelar(RolUsuario rol)` — calls `TransicionarEstado(Cancelado, rol)`; if previous state was `Creado` or `Abierto`, raises stock-restoration events for each confirmed LineaPedido (see REQ-11); if `Preparandose` or `ListoParaEntregar`, no restoration events (per REQ-11 design decision).

#### Must NOT do

- Do not implement OrdenTrabajo generation logic yet (DP-14).
- Do not add EF attributes or navigation properties.

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Pedidos/Pedido.cs"
Test-Path "src/GastroGestion.Domain/Pedidos/LineaPedido.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-07** (Scenarios 07-A through 07-D) — TipoPedido creation rules, MesaId/DireccionEntrega conditions.
- **REQ-08** (Scenarios 08-A through 08-D) — ConfirmarPrecio set-once, line edit-lock, computed totals.
- **REQ-11** (Scenarios 11-A, 11-B) — cancellation state-conditional stock events.
- Design §2 (Pedido), §5d (TransicionarEstado via registry), §8 (PedidoCreado, LineaPedidoAgregada, PedidoEstadoCambiado events raised).

---

### DP-14 — OrdenTrabajo aggregate + LineaRecetaSnapshot owned entity [ ]

**Work unit:** One commit.  
**Conventional commit:** `feat(domain): add OrdenTrabajo aggregate with recipe snapshot`

#### What to do

Create `src/GastroGestion.Domain/Pedidos/OrdenesTrabajo/` (nested under Pedidos — owned aggregate, design §2):

1. **`LineaRecetaSnapshot.cs`** — extends `Entity`; owned by OrdenTrabajo; properties: `Guid IngredienteId`, `Cantidad Cantidad` (snapshot of the recipe at OT creation time — immutable after creation).

2. **`OrdenTrabajo.cs`** — extends `AggregateRoot`; factory `static OrdenTrabajo Crear(Guid pedidoId, Guid platoId, IReadOnlyList<LineaRecetaSnapshot> recetaSnapshot)` — validates: `platoId != Guid.Empty`; `recetaSnapshot` non-empty; `bool Completada = false`; `Guid? CocineroAsignadoId = null`; `IReadOnlyList<LineaRecetaSnapshot> RecetaSnapshot` (immutable — set in ctor, no mutation methods); methods:
   - `AsignarCocinero(Guid cocineroId)` — sets `CocineroAsignadoId`; idempotent (overwrite allowed).
   - `MarcarCompletada()` — throws if already `Completada`; sets `Completada = true`.

3. **Back in `Pedido.cs`** — add method `GenerarOrdenesTrabajo(Func<Guid, IReadOnlyList<LineaRecetaSnapshot>> obtenerReceta)`:
   - Throws `DomainException` if `OrdenesTrabajo.Any()` (duplicate OT block per REQ-10).
   - For each `LineaPedido`, call `obtenerReceta(platoId)` to get the snapshot, create one `OrdenTrabajo` per line, add to `_ordenesTrabajo`.
   - Raises `OrdenTrabajoCreada` event per OT generated.
   - Checks if ALL OTs are `Completada` after any `MarcarCompletada()` call → auto-advance logic: add method `TryAutoAvanzarEstado(RolUsuario rolSistema)` — if all OTs completed AND `Estado == Preparandose`, call `TransicionarEstado(ListoParaEntregar, rolSistema)`.

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Pedidos/OrdenesTrabajo/OrdenTrabajo.cs"
Test-Path "src/GastroGestion.Domain/Pedidos/OrdenesTrabajo/LineaRecetaSnapshot.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-10** (Scenarios 10-A through 10-E) — all-or-nothing OT generation, duplicate OT block, auto-advance on all Completada, cocinero assignment, recipe snapshot.
- Design §2 (OT owned by Pedido, saga avoided, extraction path documented in design ADR).

---

### DP-15 — IEfectivoPrecioService contract + Slice 2 domain events [ ]

**Work unit:** One commit.  
**Conventional commit:** `feat(domain): add IEfectivoPrecioService contract and domain events`

#### What to do

1. **`IEfectivoPrecioService.cs`** (in `src/GastroGestion.Domain/Services/`) — interface; method: `(Dinero Precio, PorcentajeIVA IVA) ResolverPrecioEfectivo(Guid platoId, DateOnly fecha)`. Rule in doc-comment: menu override → else PrecioBase. Implementation lives in Application layer (phase 3). Contract only.

2. **Domain events** (in `src/GastroGestion.Domain/Pedidos/Events/`):
   - **`PedidoCreado.cs`** — `Guid PedidoId`, `TipoPedido Tipo`, `DateTime OccurredOn`.
   - **`LineaPedidoAgregada.cs`** — `Guid PedidoId`, `Guid LineaId`, `Guid PlatoId`, `DateTime OccurredOn`.
   - **`OrdenTrabajoCreada.cs`** — `Guid PedidoId`, `Guid OrdenTrabajoId`, `Guid PlatoId`, `DateTime OccurredOn`.
   - **`PedidoEstadoCambiado.cs`** — `Guid PedidoId`, `EstadoPedido EstadoAnterior`, `EstadoPedido EstadoNuevo`, `RolUsuario Rol`, `DateTime OccurredOn`.

   All implement `IDomainEvent`. All are records (immutable). Note: `FacturaNecesitaCAE` is a Slice 3 event (DP-21).

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Services/IEfectivoPrecioService.cs"
Get-ChildItem "src/GastroGestion.Domain/Pedidos/Events/" | Measure-Object | Select-Object -ExpandProperty Count
# Expected: 4

dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-15** (Scenarios 15-A through 15-E) — 4 mandatory Slice 2 events (PedidoCreado, LineaPedidoAgregada, OrdenTrabajoCreada, PedidoEstadoCambiado) implementing IDomainEvent; raised/buffered only; cleared by infra.
- **REQ-06** (partial, Scenario 06-E) — IEfectivoPrecioService contract in domain; implementation deferred.
- Design §5c, §8.

---

### DP-16 — Slice 2 domain tests [ ]

**Work unit:** One commit.  
**Conventional commit:** `test(domain): add Slice 2 transactional aggregate tests`

#### What to do

Target project: `tests/GastroGestion.Domain.Tests/`

1. **`PedidoTests.cs`**:
   - `Crear_Salon_WithoutMesaId_ThrowsDomainException`
   - `Crear_Delivery_WithoutDireccion_ThrowsDomainException`
   - `Crear_TakeAway_WithoutMesaOrDireccion_Succeeds`
   - `Crear_RaisesPedidoCreadoEvent`
   - `AgregarLinea_BeforePriceConfirm_Succeeds`
   - `AgregarLinea_AfterAnyPriceConfirmed_ThrowsDomainException` (edit-lock)
   - `TransicionarEstado_InvalidTransition_ThrowsDomainException`
   - `TransicionarEstado_UnauthorizedRole_ThrowsDomainException`
   - `Cancelar_FromCreado_RaisesStockRestorationEvents`
   - `Cancelar_FromPreparandose_NoStockRestorationEvents`

2. **`OrdenTrabajoTests.cs`**:
   - `GenerarOrdenesTrabajo_WhenNoneExist_CreatesOnePerLine`
   - `GenerarOrdenesTrabajo_WhenAlreadyExist_ThrowsDomainException`
   - `MarcarCompletada_AllCompleted_AutoAvanzaEstado`
   - `OrdenTrabajo_RecetaSnapshot_IsImmutable`

3. **`PedidoTransicionRegistryTests.cs`**:
   - `EsValida_ValidSalonTransition_ReturnsTrue`
   - `EsValida_TerminalStateTransition_ReturnsFalse`
   - `EsValida_UnauthorizedRole_ReturnsFalse`

#### Verification

```powershell
dotnet test tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj
# Expected: all tests pass (Slice 1 + Slice 2 combined)
```

#### Spec requirements satisfied

- **REQ-18** — test coverage for Slice 2 aggregates.
- **REQ-07, REQ-08, REQ-09, REQ-10, REQ-11** — acceptance scenario coverage.

---

### DP-17 — Slice 2 build + test verification (no code changes) [ ]

**Work unit:** Verification-only — no commits.

#### Verification commands

```powershell
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj

dotnet test tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj --logger "console;verbosity=normal"
# Expected: all Slice 1 + Slice 2 tests pass

Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" -Pattern "PackageReference|ProjectReference"
# Expected: no matches
```

---

## SLICE 3 — Fiscal

Covers: REQ-12, REQ-13, REQ-15 (FacturaNecesitaCAE), REQ-18 (continued)  
Design sections: §2 (Factura TPH, MovimientoStock), §5a (polymorphic Comprobante), §5b (stock ledger), §5e (ICalculadorFactura)  
**Prerequisite:** DP-17 must pass before DP-18 begins.

---

### DP-18 — Zero-dependency .csproj gate (Slice 3 checkpoint) [ ]

**Work unit:** Verification-only gate.  
**Conventional commit:** N/A

Repeat DP-01 verification. Block if any dependency was accidentally added.

#### Spec requirements satisfied

- **REQ-01** (Scenario 01-A) — zero deps maintained through Slice 2.

---

### DP-19 — MovimientoStock ledger aggregate [ ]

**Work unit:** One commit.  
**Conventional commit:** `feat(domain): add MovimientoStock append-only ledger aggregate`

#### What to do

Create `src/GastroGestion.Domain/Stock/`:

1. **`MovimientoStock.cs`** — extends `AggregateRoot`; factory methods only (no public setters); two factories:
   - `static MovimientoStock RegistrarMovimiento(Guid ingredienteId, TipoMovimientoStock tipo, Cantidad cantidad, Guid? ordenTrabajoId, Guid? lineaPedidoId)` — validates `cantidad.Valor != 0`; sign convention: `Compra`/`LiberacionReserva`/`DevolucionCancelacion` → positive; `Consumo`/`Reserva`/`Ajuste` (negative for reductions) → quantity sign matches type (doc-comment clarifies convention per design §5b); `FechaMovimiento = DateTime.UtcNow`.
   - `static MovimientoStock RegistrarCompra(Guid ingredienteId, Cantidad cantidad, string? lote, DateOnly? fechaVencimiento)` — wraps `RegistrarMovimiento` with `TipoMovimientoStock.Compra`; adds `Lote` and `FechaVencimiento` fields.
   - Properties: `Guid IngredienteId`, `TipoMovimientoStock Tipo`, `Cantidad Cantidad`, `Guid? OrdenTrabajoId`, `Guid? LineaPedidoId`, `string? Lote`, `DateOnly? FechaVencimiento` (nullable from day one — near-expiry seam), `DateTime FechaMovimiento`.
   - NO mutation methods — append-only root; once created, immutable.
   - Static projection helper: `static Dinero CalcularDisponible(IEnumerable<MovimientoStock> movimientos)` — NOT stored; sum of signed quantities. Validates result `>= 0` in domain (throws `DomainException`) but actual enforcement (row-lock) is infrastructure (design §5b).

#### Must NOT do

- Do not add a `Balance` stored property — balance is always a projection.
- Do not add navigation properties.

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Stock/MovimientoStock.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-12** (Scenarios 12-A through 12-F) — append-only, sign convention, TipoMovimientoStock enum, lot/expiry fields, Balance projection, immutability.
- Design §5b (reservation lifecycle, check-then-reserve atomicity at infra).

---

### DP-20 — Factura aggregate (TPH) + FacturaLinea + Pago owned entities [ ]

**Work unit:** One commit.  
**Conventional commit:** `feat(domain): add Factura TPH aggregate with FacturaLinea and Pago`

#### What to do

Create `src/GastroGestion.Domain/Facturacion/`:

1. **`Pago.cs`** — extends `Entity`; owned by Factura; properties: `Dinero Monto`, `string MetodoPago` (string in v1; enum in later phase); validate `Monto.Monto > 0` in ctor.

2. **`FacturaLinea.cs`** — extends `Entity`; owned by Factura; properties: `Guid LineaPedidoId`, `Dinero PrecioUnitario`, `PorcentajeIVA IVA`, `int Cantidad`; computed `Dinero Subtotal → PrecioUnitario.Multiplicar(Cantidad)`; computed `Dinero SubtotalConIVA → Subtotal.ConIVA(IVA)`.

3. **`Factura.cs`** — extends `AggregateRoot`; `TipoComprobante TipoComprobante`; `Guid ClienteId`; `IReadOnlyList<Guid> PedidosFacturados`; `bool Cancelada = false`; `string? CAE`; `DateOnly? VencimientoCAE`; private lists for FacturaLinea and Pago; computed totals (never stored):
   - `Dinero SubTotal → sum of FacturaLinea.Subtotal`
   - `Dinero TotalIVA → sum of FacturaLinea.SubtotalConIVA - SubTotal`
   - `Dinero Total → SubTotal + TotalIVA`
   - `Dinero TotalPagado → sum of Pago.Monto`
   - `bool EstaPagada → TotalPagado >= Total`
   - Three factory methods (TPH discriminator is domain-only — EF TPH mapping is phase 3, per design §5a):
     - `static Factura CrearTicket(Guid clienteId, List<Guid> pedidos, List<FacturaLinea> lineas)` — `TipoComprobante = TicketInterno`; CAE must remain null (guard in factory); all IVA in lineas forced to `PorcentajeIVA.Cero`.
     - `static Factura CrearFacturaConIVA(Guid clienteId, List<Guid> pedidos, List<FacturaLinea> lineas)` — `TipoComprobante = FacturaConIVA`; CAE must remain null.
     - `static Factura CrearFacturaElectronica(Guid clienteId, List<Guid> pedidos, List<FacturaLinea> lineas)` — `TipoComprobante = FacturaElectronica`; CAE starts null; raises `FacturaNecesitaCAE` event (AFIP seam).
   - Methods:
     - `AsignarCae(string cae, DateOnly vencimiento)` — set-once; throws if already set or if `TipoComprobante != FacturaElectronica` (throw `DomainException`).
     - `RegistrarPago(Pago pago)` — throws if `Cancelada`.
     - `Cancelar()` — throws if `EstaPagada` (cancel guard); sets `Cancelada = true`.
     - `bool PuedeCombinarseConFactura(Factura otra)` — returns `ClienteId == otra.ClienteId && TipoComprobante == otra.TipoComprobante` (same-client grouping seam per REQ-13).

#### Must NOT do

- Do not add `[Discriminator]` or EF TPH attributes (phase 3).
- Do not compute totals from stored fields — always recompute from line items.

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Facturacion/Factura.cs"
Test-Path "src/GastroGestion.Domain/Facturacion/FacturaLinea.cs"
Test-Path "src/GastroGestion.Domain/Facturacion/Pago.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-13** (Scenarios 13-A through 13-K) — three factory methods, CAE guard, multi-payment, Pagada threshold, computed totals, cancel guard, same-client grouping.
- Design §5a (TPH discriminator, factory methods, no EF attribute in domain).

---

### DP-21 — ICalculadorFactura contract + FacturaNecesitaCAE event [ ]

**Work unit:** One commit.  
**Conventional commit:** `feat(domain): add ICalculadorFactura contract and FacturaNecesitaCAE event`

#### What to do

1. **`ICalculadorFactura.cs`** (in `src/GastroGestion.Domain/Services/`) — interface:
   ```
   ResultadoFactura Calcular(IReadOnlyList<FacturaLinea> lineas, TipoComprobante tipo)
   ```
   - Return type `ResultadoFactura` in same file (or adjacent): record with `Dinero SubTotal`, `IReadOnlyList<DesglosIVA> DesgloseIVA`, `Dinero TotalIVA`, `Dinero Total`.
   - `DesglosIVA` record: `PorcentajeIVA Alicuota`, `Dinero BaseImponible`, `Dinero MontoIVA`.
   - Per-line IVA rule in doc-comment: `Precio * Cantidad * Alicuota`; `TicketInterno` forces `IVA = Cero` (per design §5e).

2. **`FacturaNecesitaCAE.cs`** (in `src/GastroGestion.Domain/Facturacion/Events/`) — record implementing `IDomainEvent`: `Guid FacturaId`, `Guid ClienteId`, `Dinero Total`, `DateTime OccurredOn`.

#### Verification

```powershell
Test-Path "src/GastroGestion.Domain/Services/ICalculadorFactura.cs"
Test-Path "src/GastroGestion.Domain/Facturacion/Events/FacturaNecesitaCAE.cs"
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-15** (Scenario 15-F) — `FacturaNecesitaCAE` event (5th mandatory event per spec).
- **REQ-13** (Scenario 13-B) — `FacturaNecesitaCAE` raised by `CrearFacturaElectronica`.
- Design §5e (ICalculadorFactura contract; impl in application layer), §8.

---

### DP-22 — Slice 3 domain tests [ ]

**Work unit:** One commit.  
**Conventional commit:** `test(domain): add Slice 3 fiscal aggregate tests`

#### What to do

Target project: `tests/GastroGestion.Domain.Tests/`

1. **`MovimientoStockTests.cs`**:
   - `RegistrarCompra_CreatesPositiveMovimiento`
   - `RegistrarMovimiento_ZeroCantidad_ThrowsDomainException`
   - `CalcularDisponible_SignedSum_ReturnsCorrectBalance`
   - `CalcularDisponible_Negative_ThrowsDomainException`
   - `MovimientoStock_IsImmutable_NoMutationMethods` (reflection-based or design assertion)
   - `FechaVencimiento_IsNullableOnCompra` (seam test)

2. **`FacturaTests.cs`**:
   - `CrearTicket_SetsTicketInternoTipo`
   - `CrearTicket_ForcesIVACero_OnAllLines`
   - `CrearFacturaElectronica_RaisesFacturaNecesitaCAEEvent`
   - `AsignarCae_SetOnce_ThrowsOnSecondCall`
   - `AsignarCae_OnTicket_ThrowsDomainException`
   - `Cancelar_WhenPagada_ThrowsDomainException`
   - `RegistrarPago_WhenCancelada_ThrowsDomainException`
   - `EstaPagada_WhenTotalPagadoGteTotal_ReturnsTrue`
   - `PuedeCombinarse_SameClientAndTipo_ReturnsTrue`
   - `PuedeCombinarse_DifferentCliente_ReturnsFalse`

#### Verification

```powershell
dotnet test tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj
# Expected: ALL Slice 1 + 2 + 3 tests pass; 0 failed
```

#### Spec requirements satisfied

- **REQ-18** — test coverage for Slice 3 aggregates.
- **REQ-12, REQ-13** — acceptance scenario coverage.

---

### DP-23 — Slice 3 build + test verification (no code changes) [ ]

**Work unit:** Verification-only — final gate. Slice is shippable when this passes.

#### Verification commands

```powershell
# Full domain project build
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0

# All 3 slices' tests green
dotnet test tests/GastroGestion.Domain.Tests/GastroGestion.Domain.Tests.csproj --logger "console;verbosity=normal"
# Expected: all pass; 0 failed; 0 skipped

# Zero-dependency gate — final confirmation
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" -Pattern "PackageReference|ProjectReference"
# Expected: no matches

# Namespace-to-folder spot-checks
Select-String -Path "src/GastroGestion.Domain/Stock/MovimientoStock.cs" -Pattern "namespace GastroGestion.Domain.Stock"
Select-String -Path "src/GastroGestion.Domain/Facturacion/Factura.cs" -Pattern "namespace GastroGestion.Domain.Facturacion"
# Expected: one match each
```

---

## Parallel vs. sequential summary

| Task | Can run in parallel with | Blocked by |
|------|--------------------------|------------|
| DP-01 | — (gate first) | — |
| DP-02 | — | DP-01 gate pass |
| DP-03 | — | DP-02 |
| DP-04 | — | DP-03 |
| DP-05 | DP-06, DP-08 | DP-04 |
| DP-06 | DP-05, DP-08 | DP-04 |
| DP-07 | DP-08 | DP-06 (Plato must exist) |
| DP-08 | DP-05, DP-06 | DP-04 |
| DP-09 | — | DP-05, DP-06, DP-07, DP-08 all done |
| DP-10 | — (verification gate) | DP-09 |
| DP-11 | — (gate) | DP-10 pass |
| DP-12 | — | DP-11 gate pass |
| DP-13 | — | DP-12 |
| DP-14 | — | DP-13 |
| DP-15 | — | DP-14 |
| DP-16 | — | DP-15 |
| DP-17 | — (verification gate) | DP-16 |
| DP-18 | — (gate) | DP-17 pass |
| DP-19 | DP-20 (independent aggregates) | DP-18 gate pass |
| DP-20 | DP-19 | DP-18 gate pass |
| DP-21 | — | DP-20 (Factura type needed for FacturaLinea param) |
| DP-22 | — | DP-19, DP-20, DP-21 all done |
| DP-23 | — (verification gate) | DP-22 |

---

## Spec coverage matrix

| REQ | Scenarios | Covered by task(s) |
|-----|-----------|---------------------|
| REQ-01 | 01-A | DP-01, DP-11, DP-18 (all 3 gates) |
| REQ-02 | 02-A – 02-E | DP-03 (VO ctor rules), DP-09 (ValueObjectTests) |
| REQ-03 | 03-A – 03-F | DP-04 (Cliente), DP-09 (ClienteTests) |
| REQ-04 | 04-A – 04-D | DP-05 (Ingrediente), DP-09 (IngredienteTests) |
| REQ-05 | 05-A – 05-E | DP-06 (Plato/LineaReceta), DP-09 (PlatoTests) |
| REQ-06 | 06-A – 06-E | DP-07 (Menu), DP-15 (IEfectivoPrecioService), DP-09 (MenuTests) |
| REQ-07 | 07-A – 07-D | DP-13 (Pedido), DP-16 (PedidoTests) |
| REQ-08 | 08-A – 08-D | DP-13 (LineaPedido), DP-16 (PedidoTests) |
| REQ-09 | 09-A – 09-D | DP-12 (Registry), DP-13 (TransicionarEstado), DP-16 |
| REQ-10 | 10-A – 10-E | DP-14 (OrdenTrabajo), DP-16 (OrdenTrabajoTests) |
| REQ-11 | 11-A, 11-B | DP-13 (Cancelar), DP-16 (PedidoTests) |
| REQ-12 | 12-A – 12-F | DP-19 (MovimientoStock), DP-22 (MovimientoStockTests) |
| REQ-13 | 13-A – 13-K | DP-20 (Factura), DP-21 (ICalculadorFactura), DP-22 (FacturaTests) |
| REQ-14 | 14-A – 14-D | DP-08 (Mesa), DP-09 (MesaTests) |
| REQ-15 | 15-A – 15-F | DP-02 (IDomainEvent), DP-15 (S2 events), DP-21 (FacturaNecesitaCAE) |
| REQ-16 | 16-A, 16-B | DP-04, DP-05, DP-06, DP-08 (Desactivar pattern) |
| REQ-17 | 17-A | DP-03 (enum Spanish names) |
| REQ-18 | 18-A – 18-G | DP-09, DP-16, DP-22 (one class per aggregate) |

---

## Review Workload Forecast

### Estimated changed lines per slice

| Slice | Tasks | Est. additions | Est. deletions | Notes |
|-------|-------|----------------|----------------|-------|
| Slice 1 — Catalogue | DP-02 through DP-09 | ~550–650 | ~0 | Common kernel (~80L), 6 VOs (~200L), 10 enums (~80L), 4 aggregates (~200L), 6 test classes (~180L). Dense but mechanical. |
| Slice 2 — Transactional | DP-12 through DP-16 | ~450–550 | ~0 | DireccionEntrega+Registry (~80L), Pedido+LineaPedido (~200L), OT+Snapshot (~120L), contract+events (~60L), 3 test classes (~150L). |
| Slice 3 — Fiscal | DP-19 through DP-22 | ~400–500 | ~0 | MovimientoStock (~100L), Factura+lines+Pago (~220L), contract+event (~60L), 2 test classes (~150L). |
| **Total** | **DP-01 through DP-23** | **~1,400–1,700** | **~0** | Pure additions — new domain layer from scratch. |

### 400-line budget analysis

| Metric | Value |
|--------|-------|
| Slice 1 additions | ~600L |
| Slice 2 additions | ~500L |
| Slice 3 additions | ~450L |
| **Total** | **~1,550L** |
| 400-line budget risk — Slice 1 | **High** (~600L > 400L) |
| 400-line budget risk — Slice 2 | **High** (~500L > 400L) |
| 400-line budget risk — Slice 3 | **Medium–High** (~450L ≈ 400L) |
| Chained PRs recommended | **Yes** |
| Decision needed before apply | **Yes** |

### Recommended PR mapping

Each slice is independently buildable and test-covered, making it the natural PR boundary. With delivery strategy `ask-on-risk`, the recommended approach before `sdd-apply` begins:

| PR | Slice | Tasks | Est. diff | Review focus |
|----|-------|-------|-----------|--------------|
| PR #1 — Domain Catalogue | Slice 1 | DP-01 → DP-10 | ~600L added | Common kernel, VOs/enums, 4 aggregates (Cliente/Ingrediente/Plato/Menu/Mesa), tests |
| PR #2 — Domain Transactional | Slice 2 | DP-11 → DP-17 | ~500L added | Pedido/LineaPedido/OT state machine, PedidoTransicionRegistry, events, tests. Dep: PR #1 merged. |
| PR #3 — Domain Fiscal | Slice 3 | DP-18 → DP-23 | ~450L added | Factura TPH, MovimientoStock ledger, AFIP event seam, tests. Dep: PR #2 merged. |

Chain strategy options (ask user before apply):
- `stacked-to-main`: Each PR targets main in order; fast iteration; safe because each slice has its own tests.
- `feature-branch-chain`: Tracker branch `feat/domain-port`; PR #1 → tracker; PR #2 → PR #1 branch; etc. Only tracker merges to main. Better rollback control for a large domain introduction.

**Decision required before `sdd-apply`:** Which chain strategy to use? Stacked-to-main is recommended for this change since each slice is independently coherent and the review window per PR (~60 min each) is healthy.
