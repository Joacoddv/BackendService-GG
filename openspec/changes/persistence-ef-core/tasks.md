# Tasks — persistence-ef-core

**Generated:** 2026-06-14
**Artifact store:** openspec + engram
**Change:** persistence-ef-core
**Phase:** 3 of 7, GastroGestion .NET 8 EF Core persistence layer
**Delivery strategy:** ask-on-risk → chained PRs confirmed (est. 1,500–2,200 lines total)
**Chain strategy:** stacked-to-main (3 PRs, each slice merges directly to main in order)

---

## CRITICAL — CLR Name Reconciliation

The spec text contains stale names that DO NOT match the shipped domain. The apply phase MUST use the verified CLR names below. Mapping against spec text will produce non-compiling code.

| Spec text (stale) | Actual CLR member | File |
|---|---|---|
| `TipoPedido` (as property) | `Pedido.Tipo` | `Pedidos/Pedido.cs` |
| `ConcurrencyToken` | `RowVersion` (`byte[]`) | `Pedidos/Pedido.cs`, `Mesas/Mesa.cs` |
| `Cliente.Apellido`, `Cliente.Telefono`, `int NumeroCliente` | No such members; `NumeroCliente` is `Guid` | `Clientes/Cliente.cs` |
| `PrecioSnapshot`, `IVASnapshot` on LineaPedido | `PrecioUnitario` (`Dinero?`), `IVA` (`PorcentajeIVA?`) | `Pedidos/LineaPedido.cs` |
| `SubtotalLinea`, `IVALinea`, `TotalLinea` (computed getters to Ignore) | Same names — verify `Ignore()` is applied | `Pedidos/LineaPedido.cs` |
| `Cantidad` on LineaPedido | `int Cantidad` (units, not `Cantidad` VO) | `Pedidos/LineaPedido.cs` |
| `RecetaSnapshot` collection field | `_ordenesTrabajo` / `RecetaSnapshot` on `OrdenTrabajo` | `Pedidos/OrdenTrabajo.cs` |
| `_lineasReceta` on Plato | Verify: `Plato.LineasReceta` / field name | `Platos/Plato.cs` |
| `Pago.MetodoPago` | `MetodoPago` enum (not string) | `Facturacion/Pago.cs`, `Enums/MetodoPago.cs` |
| `VencimientoCAE` type | `DateOnly?` | `Facturacion/Factura.cs` |
| `MovimientoStock.Cantidad` | `decimal` (signed, not `Cantidad` VO) | `Stock/MovimientoStock.cs` |
| `Dinero.Monto`, `Dinero.Moneda` | `Monto` (`decimal`), `Moneda` (`Moneda` enum) | `ValueObjects/Dinero.cs` |
| `Cuit.Value` | `Cuit.Valor` | `ValueObjects/Cuit.cs` |
| `Email.Value` | `Email.Valor` | `ValueObjects/Email.cs` |
| `LegajoId.Value` | `LegajoId.Valor` | `ValueObjects/LegajoId.cs` |
| `PorcentajeIVA.Alicuota` | `AlicuotaIVA` enum member; `Tasa` is derived decimal | `ValueObjects/PorcentajeIVA.cs` |
| `LineaRecetaSnapshot` as Entity | Positional `sealed record` (no Id, no base class) | `Pedidos/LineaRecetaSnapshot.cs` |
| `OrdenTrabajo.CocineroAsignadoId` (Guid?) | `CocineroAsignado` (`LegajoId?`) | `Pedidos/OrdenTrabajo.cs` |
| `Factura.Cancelada` (bool) | `Estado` (`EstadoFactura` enum); no `bool Cancelada` field | `Facturacion/Factura.cs` |
| `Factura._pagos` backing field name | Verified: `_pagos` | `Facturacion/Factura.cs` |
| `EstadoOT` source | `Pedidos/EstadoOT.cs` (not in Enums/) | `Pedidos/EstadoOT.cs` |

---

## Dependency order

```
SLICE A — Foundation
  PE-01 (projects + packages gate)                     ← gate; must pass first
    └── PE-02 (GastroGestionDbContext + DbContextFactory + NullDispatcher)
          └── PE-03 (single-field value converters: Cuit, Email, LegajoId, PorcentajeIVA)
                └── PE-04 (ClienteConfiguration + IngredienteConfiguration)
                      └── PE-05 (PlatoConfiguration)
                            └── PE-06 (MenuConfiguration)
                                  └── PE-07 (MesaConfiguration)
                                        └── PE-08 (catalogue repositories: IUnitOfWork + 5 repos)
                                              └── PE-09 (InitialCatalogue migration)
                                                    └── PE-10 (LocalDbFixture + catalogue round-trip tests)
                                                          └── PE-11 (Slice A build + test verification gate)

SLICE B — Transactional  (PE-11 must pass first)
  PE-12 (domain change: internal PrecioConfirmado on LineaPedido)
    └── PE-13 (PedidoConfiguration — owned LineaPedido/OrdenTrabajo, DireccionEntrega, RowVersion, JSON)
          └── PE-14 (MovimientoStockConfiguration + append-only repository + SaveChanges guard)
                └── PE-15 (IDomainEventDispatcher + InProcessDomainEventDispatcher)
                      └── PE-16 (IPedidoRepository + IMovimientoStockRepository impl)
                            └── PE-17 (AddPedidoAndStock migration)
                                  └── PE-18 (transactional round-trip + guard + dispatch + rowversion tests)
                                        └── PE-19 (Slice B build + test verification gate)

SLICE C — Fiscal  (PE-19 must pass first)
  PE-20 (FacturaConfiguration — flat table, discriminator, nullable CAE, JSON PedidosFacturados)
    └── PE-21 (IFacturaRepository impl)
          └── PE-22 (EfectivoPrecioService + CalculadorFactura in Application)
                └── PE-23 (CrearFactura use case: command + handler + ConflictException + TipoComprobanteSolicitado)
                      └── PE-24 (AddFactura migration)
                            └── PE-25 (Factura round-trip + CrearFactura tests)
                                  └── PE-26 (Slice C build + test verification gate)
```

Within Slice A: PE-04 and PE-05 can be developed in parallel once PE-03 lands; PE-06 depends on PE-05 (MenuItem references Plato); PE-07 is independent after PE-03. All other tasks within a slice are sequential. Slices A → B → C are strictly ordered.

---

## SLICE A — Foundation

**PR #1 target:** `main`
Covers: REQ-01, REQ-02, REQ-03, REQ-04, REQ-12 (partial), REQ-13 (partial)
Design sections: §1 (layout), §2 (DbContext), §3.1–3.5 (catalogue configs), §3.10 (converters), §4 (repository/UoW), §6 (migration + test strategy)

---

### PE-01 — Projects, packages, and zero-dep gate (Slice A) [x]

**Work unit:** Project scaffolding + NuGet additions; no EF model yet.
**Conventional commit:** `build(infra): scaffold Infrastructure and test projects, add EF Core packages`

#### What to do

1. **Create `src/GastroGestion.Infrastructure/` project** (if not present):
   - `GastroGestion.Infrastructure.csproj` targeting `net8.0`
   - `<ProjectReference>` to `GastroGestion.Application`
   - NuGet packages (Infrastructure only):
     - `Microsoft.EntityFrameworkCore.SqlServer` 8.x
     - `Microsoft.EntityFrameworkCore.Tools` 8.x (CLI tooling)
     - `Microsoft.EntityFrameworkCore.Design` 8.x
   - Folder skeleton: `Persistence/Configurations/`, `Persistence/Converters/`, `Persistence/Repositories/`, `Persistence/Migrations/`, `Events/`

2. **Create `tests/GastroGestion.Infrastructure.Tests/` project** (if not present):
   - `GastroGestion.Infrastructure.Tests.csproj` targeting `net8.0`
   - `<ProjectReference>` to `GastroGestion.Infrastructure`
   - NuGet: `Microsoft.EntityFrameworkCore.SqlServer` 8.x, `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`
   - Folder skeleton: `Common/`, `Persistence/`, `Application/`

3. **Create `src/GastroGestion.Application/` additions** (if not present):
   - `Abstractions/Persistence/` — empty folder (populated by PE-08)
   - `Abstractions/Events/` — empty folder (populated by PE-15)
   - `Common/Exceptions/` — empty folder (populated by PE-23)

4. **Zero-dep gate:** verify `GastroGestion.Domain.csproj` still has zero `PackageReference` and `ProjectReference`.

#### Verification

```powershell
# Domain project must remain zero-dep
Select-String `
    -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: no matches

# Infrastructure project must reference Application only (not Domain directly)
Select-String `
    -Path "src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj" `
    -Pattern "PackageReference"
# Expected: EF Core packages listed

# Projects build (no EF model yet — just project scaffolding)
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-02** (Scenario 02-A, 02-B) — Domain project retains zero outward dependencies.
- Design §1 — project layout established.

---

### PE-02 — GastroGestionDbContext + IDesignTimeDbContextFactory + NullDomainEventDispatcher [x]

**Work unit:** Core DbContext skeleton — no entity configs yet; `ApplyConfigurationsFromAssembly` wired.
**Conventional commit:** `feat(infra): add GastroGestionDbContext with SaveChanges override and design-time factory`

#### What to do

1. **`src/GastroGestion.Infrastructure/Persistence/GastroGestionDbContext.cs`**:
   - `sealed class GastroGestionDbContext : DbContext`
   - Constructor: `(DbContextOptions<GastroGestionDbContext> options, IDomainEventDispatcher dispatcher)`
   - DbSets (expression-bodied `=> Set<T>()`):
     - `Clientes`, `Ingredientes`, `Platos`, `Menus`, `Mesas`, `Pedidos`, `MovimientosStock`, `Facturas`
   - `OnModelCreating`: `b.ApplyConfigurationsFromAssembly(typeof(GastroGestionDbContext).Assembly)`
   - `SaveChangesAsync` override (full implementation per design §2):
     - **(a) Ledger guard**: `GuardAppendOnlyLedger()` — iterate `ChangeTracker.Entries<MovimientoStock>()`; throw `InvalidOperationException("MovimientoStock is append-only; only inserts are permitted.")` for `Modified` or `Deleted` state.
     - **(b) Collect event-bearing roots**: `ChangeTracker.Entries<AggregateRoot>().Where(e => e.Entity.DomainEvents.Count > 0).Select(e => e.Entity).ToList()` — materialise BEFORE commit.
     - **(c)** `await base.SaveChangesAsync(ct)`
     - **(d) Dispatch + clear**: for each root, `await _dispatcher.DispatchAsync(root.DomainEvents, ct)` then `root.ClearDomainEvents()`

2. **`src/GastroGestion.Infrastructure/Persistence/GastroGestionDbContextFactory.cs`**:
   - Implements `IDesignTimeDbContextFactory<GastroGestionDbContext>`
   - Reads connection string from `appsettings.Development.json` (relative to `cwd`) or env var `ConnectionStrings__GastroGestion`
   - Default fallback: `@"Server=(localdb)\mssqllocaldb;Database=GastroGestion;Trusted_Connection=True;TrustServerCertificate=True"`
   - Injects `new NullDomainEventDispatcher()` (defined next)

3. **`src/GastroGestion.Infrastructure/Events/NullDomainEventDispatcher.cs`**:
   - Implements `IDomainEventDispatcher`; `DispatchAsync` returns `Task.CompletedTask` (no-op for design-time)

4. **`src/GastroGestion.Infrastructure/DependencyInjection.cs`**:
   - `AddInfrastructure(IServiceCollection, IConfiguration)` — registers `GastroGestionDbContext` via `UseSqlServer(ConnectionStrings:GastroGestion)` with `MigrationsAssembly`; placeholder registrations for repositories (populated as each slice lands); `IUnitOfWork` → `UnitOfWork`; `IDomainEventDispatcher` → `InProcessDomainEventDispatcher`

5. **`src/GastroGestion.Api/appsettings.json`** — add `"ConnectionStrings": { "GastroGestion": "" }` placeholder.

6. **`src/GastroGestion.Api/appsettings.Development.json`** — add `"ConnectionStrings": { "GastroGestion": "Server=(localdb)\\mssqllocaldb;Database=GastroGestion;Trusted_Connection=True;TrustServerCertificate=True" }`

7. **`src/GastroGestion.Api/Program.cs`** — call `builder.Services.AddInfrastructure(builder.Configuration)`; in Development, scope `db.Database.MigrateAsync()` on startup.

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0

# Domain still zero-dep
Select-String `
    -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: no matches
```

#### Spec requirements satisfied

- **REQ-02** — EF concern confined to Infrastructure.
- **REQ-08** — `SaveChangesAsync` guard architecture (ledger + dispatch seam wired).
- Design §2 — DbContext shape, factory, NullDispatcher.

---

### PE-03 — Single-field value object converters (Cuit, Email, LegajoId, PorcentajeIVA) [x]

**Work unit:** Four converter classes under `Persistence/Converters/`.
**Conventional commit:** `feat(infra): add EF Core value converters for single-field value objects`

#### What to do

Create in `src/GastroGestion.Infrastructure/Persistence/Converters/`:

1. **`CuitConverter.cs`** — `ValueConverter<Cuit, string>`:
   - To store: `cuit => cuit.Valor` (raw 11-digit string)
   - From store: `s => new Cuit(s)`
   - Static `Instance` property
   - Column type: `nvarchar(11)`

2. **`EmailConverter.cs`** — `ValueConverter<Email, string>`:
   - To store: `email => email.Valor` (already normalised lowercase)
   - From store: `s => new Email(s)`
   - Static `Instance` property
   - Column type: `nvarchar(320)`

3. **`LegajoIdConverter.cs`** — `ValueConverter<LegajoId, Guid>`:
   - To store: `legajo => legajo.Valor`
   - From store: `g => new LegajoId(g)`
   - Static `Instance` property

4. **`PorcentajeIvaConverter.cs`** — `ValueConverter<PorcentajeIVA, int>`:
   - To store: `p => (int)p.Alicuota` (stores the `AlicuotaIVA` enum ordinal)
   - From store: `i => new PorcentajeIVA((AlicuotaIVA)i)`
   - Static `Instance` property
   - Note: `PorcentajeIVA.Tasa` (decimal) is derived from `Alicuota` — never stored separately.

> **CLR alert**: use `Cuit.Valor`, `Email.Valor`, `LegajoId.Valor` (not `.Value`). Use `PorcentajeIVA.Alicuota` (not `.Alicuota.Value`). See reconciliation table above.

#### Verification

```powershell
Get-ChildItem "src/GastroGestion.Infrastructure/Persistence/Converters/" | Select-Object Name
# Expected: CuitConverter.cs, EmailConverter.cs, LegajoIdConverter.cs, PorcentajeIvaConverter.cs

dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-04** (Scenario 04-A) — `Cuit` and `Email` converters preserve normalised form on round-trip.
- Design §3.10 — all four single-field VO converters.

---

### PE-04 — ClienteConfiguration + IngredienteConfiguration [x]

**Work unit:** Two `IEntityTypeConfiguration<T>` files.
**Conventional commit:** `feat(infra): add EF Core config for Cliente and Ingrediente aggregates`

#### What to do

1. **`ClienteConfiguration.cs`** — implements `IEntityTypeConfiguration<Cliente>`:
   ```
   b.ToTable("Clientes")
   b.HasKey(c => c.Id); b.Property(c => c.Id).ValueGeneratedNever()
   b.Property(c => c.NumeroCliente)         // Guid, get-only via backing
   b.Property(c => c.Nombre).IsRequired().HasMaxLength(200)
   b.Property(c => c.CondicionIVA).HasConversion<int>()
   b.Property(c => c.Cuit).HasConversion(CuitConverter.Instance).HasMaxLength(11)   // nullable
   b.Property(c => c.Email).HasConversion(EmailConverter.Instance).HasMaxLength(320) // nullable
   b.Property(c => c.Activo)
   b.HasIndex(c => c.Cuit).IsUnique().HasFilter("[Cuit] IS NOT NULL")
   b.OwnsMany(c => c.Direcciones, d => {
       d.ToTable("ClienteDirecciones")
       d.WithOwner().HasForeignKey("ClienteId")
       d.HasKey("Id"); d.Property(x => x.Id).ValueGeneratedNever()
       d.Property(x => x.Calle).IsRequired()
       d.Property(x => x.Numero).IsRequired()
       d.Property(x => x.Piso)            // nullable
       d.Property(x => x.Departamento)    // nullable
       d.Property(x => x.Ciudad).IsRequired()
       d.Property(x => x.Provincia).IsRequired()
       d.Property(x => x.CodigoPostal).IsRequired()
   })
   b.Navigation(c => c.Direcciones).UsePropertyAccessMode(PropertyAccessMode.Field)
   // backing field is _direcciones
   ```
   - `Cliente` has NO `Apellido`, NO `Telefono`, NO `int NumeroCliente` — DO NOT map them.

2. **`IngredienteConfiguration.cs`** — implements `IEntityTypeConfiguration<Ingrediente>`:
   ```
   b.ToTable("Ingredientes")
   b.HasKey(i => i.Id); b.Property(i => i.Id).ValueGeneratedNever()
   b.Property(i => i.Nombre).IsRequired().HasMaxLength(200)
   b.Property(i => i.UnidadBase).HasConversion<int>()
   b.Property(i => i.Activo)
   b.HasIndex(i => i.Nombre).IsUnique()
   ```

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-03** (Scenario 03-A) — Cliente with Direcciones owned collection.
- **REQ-04** (Scenario 04-A) — Cuit/Email converters applied.
- Design §3.1, §3.2.

---

### PE-05 — PlatoConfiguration [x]

**Work unit:** One configuration file with nested VO and owned entity collection.
**Conventional commit:** `feat(infra): add EF Core config for Plato aggregate with LineaReceta`

#### What to do

**`PlatoConfiguration.cs`** — implements `IEntityTypeConfiguration<Plato>`:
```
b.ToTable("Platos")
b.HasKey(p => p.Id); b.Property(p => p.Id).ValueGeneratedNever()
b.Property(p => p.Nombre).IsRequired().HasMaxLength(200)
b.Property(p => p.AlicuotaIVA).HasConversion<int>()    // maps AlicuotaIVA enum
b.Property(p => p.Activo)
b.OwnsOne(p => p.PrecioBase, m => {
    m.Property(d => d.Monto).HasColumnName("PrecioBase_Monto").HasColumnType("decimal(18,2)")
    m.Property(d => d.Moneda).HasColumnName("PrecioBase_Moneda").HasConversion<int>()
})
b.Navigation(p => p.PrecioBase).IsRequired()
b.OwnsMany(p => p.LineasReceta, r => {
    r.ToTable("PlatoLineasReceta")
    r.WithOwner().HasForeignKey("PlatoId")
    r.HasKey("Id"); r.Property(x => x.Id).ValueGeneratedNever()
    r.Property(x => x.IngredienteId)
    r.Property(x => x.PlatoReferenciadoId)    // nullable — sub-recipe seam
    r.OwnsOne(x => x.Cantidad, c => {
        c.Property(q => q.Valor).HasColumnName("Cantidad_Valor").HasColumnType("decimal(18,3)")
        c.Property(q => q.Unidad).HasColumnName("Cantidad_Unidad").HasConversion<int>()
    })
    r.Navigation(x => x.Cantidad).IsRequired()
})
b.Navigation(p => p.LineasReceta).UsePropertyAccessMode(PropertyAccessMode.Field)
// backing field: verify actual field name in Plato.cs (_lineasReceta)
```

> **Tricky mapping**: `Dinero.Monto` and `Dinero.Moneda` must use `HasColumnName` per owned path. Without this, EF will generate a column collision when another aggregate also owns a `Dinero`. Verify field name for `LineasReceta` in `Plato.cs`.

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-03** (Scenario 03-B) — Plato with LineaReceta round-trip; `PrecioBase.Monto`/`Moneda` preserved.
- **REQ-04** (Scenario 04-B partial) — Dinero nested path uses distinct column names.
- Design §3.3.

---

### PE-06 — MenuConfiguration [x]

**Work unit:** One configuration file with nullable `Dinero` owned type on `MenuItem`.
**Conventional commit:** `feat(infra): add EF Core config for Menu aggregate with MenuItem`

#### What to do

**`MenuConfiguration.cs`** — implements `IEntityTypeConfiguration<Menu>`:
```
b.ToTable("Menus")
b.HasKey(m => m.Id); b.Property(m => m.Id).ValueGeneratedNever()
b.Property(m => m.Nombre).IsRequired().HasMaxLength(200)
b.Property(m => m.FechaVigencia)    // DateOnly → date column
b.Property(m => m.Activo)
b.OwnsMany(m => m.Items, it => {
    it.ToTable("MenuItems")
    it.WithOwner().HasForeignKey("MenuId")
    it.HasKey("Id"); it.Property(x => x.Id).ValueGeneratedNever()
    it.Property(x => x.PlatoId)
    it.OwnsOne(x => x.PrecioOverride, m => {
        m.Property(d => d.Monto).HasColumnName("PrecioOverride_Monto").HasColumnType("decimal(18,2)")
        m.Property(d => d.Moneda).HasColumnName("PrecioOverride_Moneda").HasConversion<int>()
    })
    it.Navigation(x => x.PrecioOverride).IsRequired(false)    // nullable override
})
b.Navigation(m => m.Items).UsePropertyAccessMode(PropertyAccessMode.Field)
// backing field: _items
```

> **Tricky mapping**: nullable `Dinero` on `MenuItem` requires `.IsRequired(false)` on the navigation or EF will treat all columns as non-nullable. Distinct column prefix `PrecioOverride_` avoids collision.

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- Design §3.4 — nullable `PrecioOverride` owned type.

---

### PE-07 — MesaConfiguration [x]

**Work unit:** One configuration file; includes `RowVersion` concurrency token.
**Conventional commit:** `feat(infra): add EF Core config for Mesa aggregate with RowVersion`

#### What to do

**`MesaConfiguration.cs`** — implements `IEntityTypeConfiguration<Mesa>`:
```
b.ToTable("Mesas")
b.HasKey(m => m.Id); b.Property(m => m.Id).ValueGeneratedNever()
b.Property(m => m.Numero)
b.Property(m => m.Capacidad)
b.Property(m => m.Estado).HasConversion<int>()    // EstadoMesa enum
b.Property(m => m.Activa)
b.Property(m => m.PedidoActivoId)                  // nullable Guid
b.Property(m => m.RowVersion).IsRowVersion()       // SQL Server rowversion; store-generated
b.HasIndex(m => m.Numero).IsUnique()
```

> **CLR alert**: property is `RowVersion` (not `ConcurrencyToken`). `IsRowVersion()` marks it store-generated; the domain default `[]` is ignored by EF on insert.

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-03** (Scenario 03-C) — Mesa with RowVersion non-empty after first save.
- **REQ-09** (Scenario 09-B) — RowVersion is non-null after first save.
- Design §3.5, §3.8.

---

### PE-08 — Port interfaces + catalogue repositories + UnitOfWork [x]

**Work unit:** Application port interfaces + Infrastructure repository implementations for Slice A aggregates.
**Conventional commit:** `feat(app+infra): add repository ports and catalogue repository implementations`

#### What to do

**Application ports** (`src/GastroGestion.Application/Abstractions/Persistence/`):

1. `IUnitOfWork.cs` — `Task<int> SaveChangesAsync(CancellationToken ct = default)`
2. `IClienteRepository.cs` — `GetByIdAsync(Guid, CancellationToken)`, `AddAsync(Cliente, CancellationToken)`
3. `IIngredienteRepository.cs` — `GetByIdAsync(Guid, CancellationToken)`, `AddAsync(Ingrediente, CancellationToken)`
4. `IPlatoRepository.cs` — `GetByIdAsync(Guid, CancellationToken)`, `AddAsync(Plato, CancellationToken)`
5. `IMenuRepository.cs` — `GetByIdAsync(Guid, CancellationToken)`, `AddAsync(Menu, CancellationToken)`
6. `IMesaRepository.cs` — `GetByIdAsync(Guid, CancellationToken)`, `AddAsync(Mesa, CancellationToken)`
7. `IPedidoRepository.cs` — `GetByIdAsync(Guid, CancellationToken)`, `GetByIdsAsync(IReadOnlyCollection<Guid>, CancellationToken)`, `AddAsync(Pedido, CancellationToken)` *(added here; impl in PE-16)*
8. `IMovimientoStockRepository.cs` — `AddAsync(MovimientoStock, CancellationToken)`, `CalcularBalanceAsync(Guid ingredienteId, CancellationToken)` *(NO Update, Remove, Delete)* *(impl in PE-16)*
9. `IFacturaRepository.cs` — `GetByIdAsync(Guid, CancellationToken)`, `AddAsync(Factura, CancellationToken)` *(impl in PE-21)*

**Infrastructure implementations** (`src/GastroGestion.Infrastructure/Persistence/Repositories/`):

10. `ClienteRepository.cs` — `_ctx.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct)` + `_ctx.Clientes.AddAsync(cliente, ct)`
11. `IngredienteRepository.cs` — same pattern
12. `PlatoRepository.cs` — same pattern
13. `MenuRepository.cs` — same pattern
14. `MesaRepository.cs` — same pattern

15. `UnitOfWork.cs` — delegates `SaveChangesAsync` to `_ctx.SaveChangesAsync(ct)`

**DI registration** — update `AddInfrastructure` to register all five Slice A repositories and `UnitOfWork`.

**Application DI** — create `src/GastroGestion.Application/DependencyInjection.cs` with `AddApplication(IServiceCollection)` (placeholder for Phase 4 handlers; registers nothing yet).

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
# Expected: exit 0 both
```

#### Spec requirements satisfied

- **REQ-12** (Scenarios 12-A, 12-B) — per-aggregate repository shape; UoW atomic commit.
- Design §4 — specific repos, no generic base; UoW delegates to DbContext override.

---

### PE-09 — InitialCatalogue migration [x]

**Work unit:** Generate and verify the first EF Core migration.
**Conventional commit:** `feat(infra): add InitialCatalogue EF Core migration`

#### What to do

1. Run from the repository root:
   ```powershell
   dotnet ef migrations add InitialCatalogue `
       --project src/GastroGestion.Infrastructure `
       --startup-project src/GastroGestion.Api `
       --output-dir Persistence/Migrations
   ```
2. Review the generated migration file — confirm tables for all 8 aggregates and no unexpected columns.
3. Do NOT manually edit the generated migration unless a column collision is found (fix via configuration instead).
4. Apply against LocalDB to validate:
   ```powershell
   dotnet ef database update `
       --project src/GastroGestion.Infrastructure `
       --startup-project src/GastroGestion.Api
   ```

> **Notes**: the migration only covers Slice A entities (Clientes, Ingredientes, Platos, Menus, Mesas). Pedidos/MovimientosStock/Facturas are added in subsequent slices. Verify no `Dinero` column name collisions in the migration output.

#### Verification

```powershell
# Migration file exists
Get-ChildItem "src/GastroGestion.Infrastructure/Persistence/Migrations/" | Select-Object Name
# Expected: *_InitialCatalogue.cs + GastroGestionDbContextModelSnapshot.cs

# Database update succeeds
dotnet ef database update `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api
# Expected: exit 0

# Idempotent re-apply (Scenario 01-B)
dotnet ef database update `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api
# Expected: exit 0, "No migrations were applied"
```

#### Spec requirements satisfied

- **REQ-01** (Scenarios 01-A, 01-B) — greenfield schema bootstraps; idempotent re-apply.
- Design §6 — `InitialCatalogue` migration, intent-named.

---

### PE-10 — LocalDbFixture + catalogue round-trip integration tests [x]

**Work unit:** Test harness + first integration test classes covering Slice A aggregates.
**Conventional commit:** `test(infra): add LocalDbFixture and catalogue aggregate round-trip tests`

#### What to do

1. **`tests/GastroGestion.Infrastructure.Tests/Common/LocalDbFixture.cs`**:
   - `IAsyncLifetime` implementation
   - `InitializeAsync`: build `DbContextOptions` using `Server=(localdb)\mssqllocaldb;Database=GastroGestion_Test_{ClassName}_{shortGuid}` pattern; `await db.Database.MigrateAsync()` (auto-applies all migrations)
   - `DisposeAsync`: `await db.Database.EnsureDeletedAsync()`
   - Exposes `GastroGestionDbContext CreateContext()` factory (injecting `NullDomainEventDispatcher`)

2. **`tests/GastroGestion.Infrastructure.Tests/Persistence/ClienteRoundTripTests.cs`**:
   - `ClienteWithDirecciones_RoundTrips` → covers Scenario 03-A
   - `Cuit_Email_ConvertersPreserveNormalizedValues` → covers Scenario 04-A (verifies `Cuit.Valor` and `Email.Valor` round-trip)
   - `DireccionEntrega_NullForNonDelivery` → Scenario 04-C (Pedido; include here as sanity or defer to Slice B)

3. **`tests/GastroGestion.Infrastructure.Tests/Persistence/PlatoRoundTripTests.cs`**:
   - `PlatoWithLineasReceta_RoundTrips` → covers Scenario 03-B (PrecioBase Dinero + nested Cantidad in LineaReceta)

4. **`tests/GastroGestion.Infrastructure.Tests/Persistence/MesaRoundTripTests.cs`**:
   - `Mesa_RowVersionIsNonEmpty_AfterFirstSave` → covers Scenario 03-C and Scenario 09-B
   - `Mesa_RowVersionConcurrencyConflict_ThrowsDbUpdateConcurrencyException` (stub for full Slice B test or do partial here against Mesa)

#### Verification

```powershell
dotnet test tests/GastroGestion.Infrastructure.Tests/ `
    --filter "Category=SliceA" `
    --logger "console;verbosity=normal"
# Expected: all Slice A tests pass; exit 0
```

#### Spec requirements satisfied

- **REQ-03** (Scenarios 03-A, 03-B, 03-C) — catalogue aggregate round-trips.
- **REQ-04** (Scenario 04-A) — Cuit/Email converters preserve normalised form.
- **REQ-13** (Scenario 13-B) — test harness applies migrations before execution.
- Design §6 — LocalDbFixture per-class DB create/migrate/drop.

---

### PE-11 — Slice A build + test verification gate [x]

**Work unit:** Verification-only — no code commits. Confirms Slice A is shippable as PR #1.

#### Verification commands

```powershell
# Full solution build
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
# Expected: exit 0 both

# All Slice A integration tests green
dotnet test tests/GastroGestion.Infrastructure.Tests/ `
    --logger "console;verbosity=normal"
# Expected: all pass; 0 failed; 0 skipped

# Domain zero-dep gate — final Slice A confirmation
Select-String `
    -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: no matches

# Domain compiles in isolation (Scenario 02-B)
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0
```

---

## SLICE B — Transactional

**PR #2 target:** `main` (after PR #1 is merged)
**Prerequisite:** PE-11 must pass.
Covers: REQ-05, REQ-06, REQ-07, REQ-08, REQ-09, REQ-12 (continued)
Design sections: §3.6 (Pedido config — highest complexity), §3.7 (MovimientoStock), §3.8 (RowVersion), §4 (dispatcher), §9 (PrecioConfirmado domain change)

---

### PE-12 — Domain change: expose `internal bool PrecioConfirmado` on LineaPedido

**Work unit:** Single-line additive change to the Domain project — the ONLY domain change in Phase 3.
**Conventional commit:** `feat(domain): expose internal PrecioConfirmado on LineaPedido for EF persistence`

#### What to do

In `src/GastroGestion.Domain/Pedidos/LineaPedido.cs`:

Replace the bare private field:
```csharp
private bool _precioConfirmado;
```
with an internal property backed by the same semantics:
```csharp
internal bool PrecioConfirmado { get; private set; }
```

Update `ConfirmarPrecio` to set `PrecioConfirmado = true` (instead of `_precioConfirmado = true`).
Update the guard `if (_precioConfirmado)` to `if (PrecioConfirmado)`.

> **Why `internal` is sufficient**: EF Core reflects on non-public members declared on the entity type. No `InternalsVisibleTo` is required. If reflection fails (unlikely with EF 8), fallback is a shadow property `Property<bool>("PrecioConfirmado").HasField("_precioConfirmado")` — do NOT use this unless the round-trip test fails.

> **Invariant preserved**: `ConfirmarPrecio` still throws `DomainException` on second call. The domain behavior is unchanged — this is purely an exposure change for EF mapping.

> **Domain csproj must remain zero-dep**: verify no package reference was added.

#### Verification

```powershell
# Domain still builds with zero deps
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0

Select-String `
    -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: no matches

# Existing domain tests still pass (confirm invariant unchanged)
dotnet test tests/GastroGestion.Domain.Tests/ `
    --logger "console;verbosity=normal"
# Expected: all pass; 0 failed
```

#### Spec requirements satisfied

- **REQ-05** (Scenarios 05-A, 05-B) — `PrecioConfirmado` mapped as column; reloaded line rejects second `ConfirmarPrecio`.
- **REQ-02** (Scenario 02-A) — Domain csproj still zero-dep after domain change.
- Design §9 — the one domain change, additive and independently revertible.

---

### PE-13 — PedidoConfiguration (highest mapping complexity)

**Work unit:** One large configuration class covering all owned types, JSON column, nullable address, RowVersion.
**Conventional commit:** `feat(infra): add EF Core config for Pedido aggregate with full owned graph`

#### What to do

**`PedidoConfiguration.cs`** — implements `IEntityTypeConfiguration<Pedido>`:

```
b.ToTable("Pedidos")
b.HasKey(p => p.Id); b.Property(p => p.Id).ValueGeneratedNever()
b.Property(p => p.Tipo).HasConversion<int>()          // TipoPedido — CLR: Tipo (not TipoPedido)
b.Property(p => p.Estado).HasConversion<int>()         // EstadoPedido
b.Property(p => p.MesaId)                              // nullable Guid
b.Property(p => p.ClienteId)                           // nullable Guid
b.Property(p => p.CreadoEnUtc)
b.Property(p => p.RowVersion).IsRowVersion()           // SQL Server rowversion; store-generated

// Nullable DireccionEntrega — flattened VO with explicit column names
b.OwnsOne(p => p.DireccionEntrega, dir => {
    dir.Property(x => x.Calle).HasColumnName("Entrega_Calle")
    dir.Property(x => x.Numero).HasColumnName("Entrega_Numero")
    dir.Property(x => x.Piso).HasColumnName("Entrega_Piso")
    dir.Property(x => x.Departamento).HasColumnName("Entrega_Departamento")
    dir.Property(x => x.Ciudad).HasColumnName("Entrega_Ciudad")
    dir.Property(x => x.Provincia).HasColumnName("Entrega_Provincia")
    dir.Property(x => x.CodigoPostal).HasColumnName("Entrega_CodigoPostal")
})
b.Navigation(p => p.DireccionEntrega).IsRequired(false)   // CRITICAL: null for Salon/Mostrador

// LineaPedido owned collection (field _lineas)
b.OwnsMany(p => p.Lineas, l => {
    l.ToTable("PedidoLineas")
    l.WithOwner().HasForeignKey("PedidoId")
    l.HasKey("Id"); l.Property(x => x.Id).ValueGeneratedNever()
    l.Property(x => x.PlatoId)
    l.Property(x => x.Cantidad)          // int — NOT Cantidad VO
    l.Property(x => x.Observaciones)    // nullable string

    // nullable PrecioUnitario (Dinero) — CLR: PrecioUnitario (not PrecioSnapshot)
    l.OwnsOne(x => x.PrecioUnitario, m => {
        m.Property(d => d.Monto).HasColumnName("PrecioUnitario_Monto").HasColumnType("decimal(18,2)")
        m.Property(d => d.Moneda).HasColumnName("PrecioUnitario_Moneda").HasConversion<int>()
    })
    l.Navigation(x => x.PrecioUnitario).IsRequired(false)

    // nullable IVA (PorcentajeIVA) — CLR: IVA (not IVASnapshot)
    l.Property(x => x.IVA).HasConversion(PorcentajeIvaConverter.Instance).HasColumnName("IVA_Alicuota")

    // set-once flag — CLR: internal bool PrecioConfirmado (from PE-12)
    l.Property(x => x.PrecioConfirmado).HasColumnName("PrecioConfirmado")

    // Computed getters — MUST Ignore (they throw if PrecioUnitario is null)
    l.Ignore(x => x.SubtotalLinea)
    l.Ignore(x => x.IVALinea)
    l.Ignore(x => x.TotalLinea)
})
b.Navigation(p => p.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field)
// backing field: _lineas

// OrdenTrabajo owned collection with LineaRecetaSnapshot JSON (field _ordenesTrabajo)
b.OwnsMany(p => p.OrdenesTrabajo, ot => {
    ot.ToTable("PedidoOrdenesTrabajo")
    ot.WithOwner().HasForeignKey("PedidoId")
    ot.HasKey("Id"); ot.Property(x => x.Id).ValueGeneratedNever()
    ot.Property(x => x.PlatoId)
    ot.Property(x => x.LineaPedidoId)
    ot.Property(x => x.Estado).HasConversion<int>()    // EstadoOT enum (in Pedidos/ not Enums/)

    // CocineroAsignado is LegajoId? (not Guid?) — apply LegajoIdConverter
    ot.Property(x => x.CocineroAsignado)
      .HasConversion(LegajoIdConverter.Instance).HasColumnName("CocineroAsignado")

    // RecetaSnapshot: IReadOnlyList<LineaRecetaSnapshot> (sealed record) → JSON column
    // LineaRecetaSnapshot is a positional record — no Id property
    ot.OwnsMany(x => x.RecetaSnapshot, snap => {
        snap.ToJson()           // column name defaults to "RecetaSnapshot"
        snap.Property(s => s.IngredienteId)
        snap.OwnsOne(s => s.Cantidad, c => {
            c.Property(q => q.Valor)
            c.Property(q => q.Unidad).HasConversion<int>()
        })
    })
})
b.Navigation(p => p.OrdenesTrabajo).UsePropertyAccessMode(PropertyAccessMode.Field)
// backing field: _ordenesTrabajo
```

> **Tricky mappings in this task — all verified against actual CLR**:
> - `Pedido.Tipo` (not `TipoPedido` as a property name) — Scenario 04-C/D correctness gate
> - `LineaPedido.Cantidad` is `int` (units) — do NOT use `Cantidad` VO config here
> - `LineaPedido.PrecioUnitario` / `IVA` (not `PrecioSnapshot`/`IVASnapshot`)
> - `PrecioConfirmado` is `internal bool` — verify EF reflects it without shadow property
> - `OrdenTrabajo.CocineroAsignado` is `LegajoId?` — apply `LegajoIdConverter`
> - `LineaRecetaSnapshot` is a positional `sealed record` with no `Id` — `ToJson()` handles shadow key
> - Computed getters (`SubtotalLinea`, `IVALinea`, `TotalLinea`) MUST be `Ignore()`d

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-05** — `PrecioConfirmado` column mapped.
- **REQ-06** (Scenario 06-A, 06-B) — Pedido + owned graphs config; snapshot JSON.
- **REQ-04** (Scenarios 04-C, 04-D) — nullable `DireccionEntrega`; Delivery address round-trip config.
- **REQ-09** — `RowVersion` on Pedido configured.
- Design §3.6 — all tricky mappings resolved.

---

### PE-14 — MovimientoStockConfiguration + append-only guard

**Work unit:** Config + the SaveChanges guard (already wired in DbContext skeleton from PE-02; verify it targets `MovimientoStock`).
**Conventional commit:** `feat(infra): add EF Core config for MovimientoStock with append-only SaveChanges guard`

#### What to do

1. **`MovimientoStockConfiguration.cs`** — implements `IEntityTypeConfiguration<MovimientoStock>`:
   ```
   b.ToTable("MovimientosStock")
   b.HasKey(m => m.Id); b.Property(m => m.Id).ValueGeneratedNever()
   b.Property(m => m.IngredienteId)
   b.Property(m => m.Cantidad).HasColumnType("decimal(18,3)")  // signed decimal
   b.Property(m => m.Tipo).HasConversion<int>()                // TipoMovimientoStock
   b.Property(m => m.FechaMovimiento)
   b.Property(m => m.OrdenTrabajoId)    // nullable Guid
   b.Property(m => m.LineaPedidoId)     // nullable Guid
   b.Property(m => m.Lote)              // nullable string
   b.Property(m => m.FechaVencimiento)  // DateOnly? → nullable date column
   b.HasIndex(m => m.IngredienteId)     // SUM query performance
   ```
   > **CLR alert**: `MovimientoStock.Cantidad` is `decimal` (signed, not a `Cantidad` VO). Do NOT apply `OwnsOne` — map as a plain decimal column.

2. **Verify the SaveChanges guard in `GastroGestionDbContext.GuardAppendOnlyLedger()`**:
   - Must iterate `ChangeTracker.Entries<MovimientoStock>()`
   - Must throw for both `EntityState.Modified` AND `EntityState.Deleted`
   - Must NOT throw for `EntityState.Added` (new entries are allowed)

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-07** (Scenarios 07-A, 07-B, 07-C) — append-only interface; SaveChanges guard rejects Modify/Delete.
- Design §3.7, §2(a) — dual enforcement at repository + DbContext.

---

### PE-15 — IDomainEventDispatcher + InProcessDomainEventDispatcher

**Work unit:** Application port + Infrastructure implementation for post-commit event dispatch.
**Conventional commit:** `feat(app+infra): add IDomainEventDispatcher and in-process post-commit implementation`

#### What to do

1. **`src/GastroGestion.Application/Abstractions/Events/IDomainEventDispatcher.cs`**:
   ```csharp
   public interface IDomainEventDispatcher
   {
       Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default);
   }
   ```

2. **`src/GastroGestion.Infrastructure/Events/InProcessDomainEventDispatcher.cs`**:
   - Minimal Phase 3 implementation: iterates events, does nothing per event (no handlers registered yet)
   - Satisfies the `FacturaNecesitaCAE` seam — Phase 5 registers a handler at this interface without touching DbContext
   - Example shape:
     ```csharp
     public sealed class InProcessDomainEventDispatcher : IDomainEventDispatcher
     {
         public Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default)
             => Task.CompletedTask;   // no handlers in Phase 3; seam established
     }
     ```

3. Update `AddInfrastructure` to register `IDomainEventDispatcher → InProcessDomainEventDispatcher` (scoped).

4. **`tests/GastroGestion.Infrastructure.Tests/Common/CapturingDomainEventDispatcher.cs`** — test double:
   - Records dispatched events in `List<IDomainEvent> CapturedEvents`
   - Used in `DomainEventDispatchTests` (PE-18)

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-08** (Scenarios 08-A, 08-B, 08-C) — dispatch interface and post-commit seam wired.
- Design §2(d), §4 — in-process dispatcher; `InProcessDomainEventDispatcher` walks events and clears.

---

### PE-16 — IPedidoRepository + IMovimientoStockRepository implementations

**Work unit:** Two repository implementations for Slice B aggregates.
**Conventional commit:** `feat(infra): add PedidoRepository and MovimientoStockRepository implementations`

#### What to do

1. **`PedidoRepository.cs`** — implements `IPedidoRepository`:
   - `GetByIdAsync`: `_ctx.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct)` — owned entities (`Lineas`, `OrdenesTrabajo`) load automatically with the root (EF owned type behavior)
   - `GetByIdsAsync`: `_ctx.Pedidos.Where(p => ids.Contains(p.Id)).ToListAsync(ct)` — returns `IReadOnlyList<Pedido>`
   - `AddAsync`: `await _ctx.Pedidos.AddAsync(pedido, ct)`

2. **`MovimientoStockRepository.cs`** — implements `IMovimientoStockRepository`:
   - `AddAsync`: `await _ctx.MovimientosStock.AddAsync(movimiento, ct)` — NO Update/Remove methods
   - `CalcularBalanceAsync`: `await _ctx.MovimientosStock.Where(m => m.IngredienteId == ingredienteId).SumAsync(m => m.Cantidad, ct)`

3. Register both in `AddInfrastructure`.

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-07** (Scenario 07-A, 07-D) — `IMovimientoStockRepository` exposes only Add + balance; SUM query correct.
- **REQ-12** (Scenario 12-A) — `IPedidoRepository.GetByIdAsync` loads full owned graph.
- Design §4 — specific repositories, no generic base.

---

### PE-17 — AddPedidoAndStock migration

**Work unit:** Generate and apply second migration covering Pedido + MovimientoStock tables.
**Conventional commit:** `feat(infra): add AddPedidoAndStock EF Core migration`

#### What to do

```powershell
dotnet ef migrations add AddPedidoAndStock `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api `
    --output-dir Persistence/Migrations

dotnet ef database update `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api
```

Review the migration for:
- `PedidoLineas` table with `PrecioConfirmado` column (boolean)
- `PedidoOrdenesTrabajo` table with `RecetaSnapshot` JSON column
- `Entrega_*` nullable columns for `DireccionEntrega` on `Pedidos` table
- `RowVersion` column as `rowversion` type on `Pedidos`
- No column name collision on `PrecioUnitario_Monto`/`PrecioUnitario_Moneda`
- `MovimientosStock` table with `decimal(18,3)` `Cantidad` column

#### Verification

```powershell
dotnet ef database update `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api
# Expected: exit 0

# Idempotent re-apply
dotnet ef database update `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api
# Expected: exit 0, no structural changes
```

#### Spec requirements satisfied

- **REQ-01** (Scenario 01-A) — schema contains all tables through Slice B.
- Design §6 — `AddPedidoAndStock` migration.

---

### PE-18 — Transactional round-trip + guard + dispatch + RowVersion integration tests

**Work unit:** Full Slice B integration test coverage.
**Conventional commit:** `test(infra): add Slice B transactional round-trip, guard, dispatch, and concurrency tests`

#### What to do

1. **`tests/GastroGestion.Infrastructure.Tests/Persistence/PedidoRoundTripTests.cs`**:
   - `Pedido_WithLineasAndOrdenesTrabajo_RoundTrips` → Scenario 06-A (lines + OTs + snapshot)
   - `RecetaSnapshot_IsImmutable_AfterPlatoRecipeChange` → Scenario 06-B (JSON survives Plato edit)
   - `DireccionEntrega_IsNull_ForSalonPedido` → Scenario 04-C
   - `DireccionEntrega_IsPresent_ForDeliveryPedido` → Scenario 04-D
   - `PrecioConfirmado_True_AfterReload_RejectsSecondConfirm` → Scenario 05-A (set-once invariant)
   - `PrecioConfirmado_False_AfterReload_AllowsFirstConfirm` → Scenario 05-B

2. **`tests/GastroGestion.Infrastructure.Tests/Persistence/MovimientoStockGuardTests.cs`**:
   - `ModifyPersistedMovimientoStock_ThrowsBeforeCommit` → Scenario 07-B
   - `DeletePersistedMovimientoStock_ThrowsBeforeCommit` → Scenario 07-C
   - `CalcularBalance_SumIsCorrect` → Scenario 07-D (Compra +20, Reserva -5, Consumo -5, LiberacionReserva +5 = 15)

3. **`tests/GastroGestion.Infrastructure.Tests/Persistence/DomainEventDispatchTests.cs`**:
   - `Events_AreDispatchedOnce_AfterSuccessfulSave` → Scenario 08-A (CapturingDomainEventDispatcher records events)
   - `Events_AreNotDispatched_OnSaveFailure` → Scenario 08-B (constraint violation → events not fired)
   - `Events_AreCleared_AfterSuccessfulDispatch` → Scenario 08-C (`DomainEvents.Count == 0` post-dispatch)

4. **Concurrency tests** (extend `PedidoRoundTripTests` or new file):
   - `ConcurrentPedidoUpdate_ThrowsDbUpdateConcurrencyException` → Scenario 09-A (two contexts, stale RowVersion)
   - `RowVersion_IsNonEmpty_AfterFirstSave` → Scenario 09-B

#### Verification

```powershell
dotnet test tests/GastroGestion.Infrastructure.Tests/ `
    --logger "console;verbosity=normal"
# Expected: all Slice A + B tests pass; 0 failed
```

#### Spec requirements satisfied

- **REQ-05** (05-A, 05-B) — `PrecioConfirmado` persistence invariant.
- **REQ-06** (06-A, 06-B) — Pedido owned graph round-trip; snapshot JSON freeze.
- **REQ-07** (07-B, 07-C, 07-D) — append-only guard; balance SUM.
- **REQ-08** (08-A, 08-B, 08-C) — event dispatch + clear.
- **REQ-09** (09-A, 09-B) — RowVersion concurrency.
- Design §6 — round-trip coverage targets.

---

### PE-19 — Slice B build + test verification gate

**Work unit:** Verification-only — no code commits. Confirms Slice B is shippable as PR #2.

#### Verification commands

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0 all

dotnet test tests/GastroGestion.Infrastructure.Tests/ `
    --logger "console;verbosity=normal"
# Expected: all Slice A + B tests pass; 0 failed

dotnet test tests/GastroGestion.Domain.Tests/ `
    --logger "console;verbosity=normal"
# Expected: all domain tests still pass (domain change must not break any)

Select-String `
    -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: no matches
```

---

## SLICE C — Fiscal

**PR #3 target:** `main` (after PR #2 is merged)
**Prerequisite:** PE-19 must pass.
Covers: REQ-10, REQ-11, REQ-12 (continued), REQ-13
Design sections: §3.9 (Factura config), §4 (IFacturaRepository), §5 (domain services + CrearFactura), §6 (AddFactura migration + tests)

---

### PE-20 — FacturaConfiguration

**Work unit:** One configuration class — flat table, discriminator column, nullable CAE, JSON PedidosFacturados, owned FacturaLinea/Pago.
**Conventional commit:** `feat(infra): add EF Core config for Factura aggregate (flat table, discriminator, JSON)`

#### What to do

**`FacturaConfiguration.cs`** — implements `IEntityTypeConfiguration<Factura>`:

```
b.ToTable("Facturas")
b.HasKey(f => f.Id); b.Property(f => f.Id).ValueGeneratedNever()
b.Property(f => f.TipoComprobante).HasConversion<int>()    // discriminator COLUMN — NOT EF inheritance
b.Property(f => f.Estado).HasConversion<int>()              // EstadoFactura enum
b.Property(f => f.ClienteId)
b.Property(f => f.FechaAlta)
b.Property(f => f.CAE).HasMaxLength(14)                     // nullable — FacturaElectronica only
b.Property(f => f.VencimientoCAE)                           // DateOnly? → nullable date column

// Computed totals MUST be Ignored (recomputed from lines, never stored)
b.Ignore(f => f.SubTotal)
b.Ignore(f => f.TotalIVA)
b.Ignore(f => f.Total)
b.Ignore(f => f.TotalPagado)
b.Ignore(f => f.EstaPagada)

// PedidosFacturados: backing field _pedidosFacturados → JSON primitive collection
b.PrimitiveCollection<IReadOnlyList<Guid>>("PedidosFacturados")
  .HasField("_pedidosFacturados")
  .UsePropertyAccessMode(PropertyAccessMode.Field)
  .ToJson("PedidosFacturados")

// FacturaLinea owned collection (backing field _lineas)
b.OwnsMany(f => f.Lineas, l => {
    l.ToTable("FacturaLineas")
    l.WithOwner().HasForeignKey("FacturaId")
    l.HasKey("Id"); l.Property(x => x.Id).ValueGeneratedNever()
    l.Property(x => x.LineaPedidoId)
    l.Property(x => x.Cantidad)                              // int
    l.OwnsOne(x => x.PrecioUnitario, m => {
        m.Property(d => d.Monto).HasColumnName("PrecioUnitario_Monto").HasColumnType("decimal(18,2)")
        m.Property(d => d.Moneda).HasColumnName("PrecioUnitario_Moneda").HasConversion<int>()
    })
    l.Navigation(x => x.PrecioUnitario).IsRequired()
    l.Property(x => x.IVA).HasConversion(PorcentajeIvaConverter.Instance).HasColumnName("IVA_Alicuota")
    l.Ignore(x => x.Subtotal)
    l.Ignore(x => x.SubtotalConIVA)
})
b.Navigation(f => f.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field)

// Pago owned collection (backing field _pagos)
b.OwnsMany(f => f.Pagos, p => {
    p.ToTable("FacturaPagos")
    p.WithOwner().HasForeignKey("FacturaId")
    p.HasKey("Id"); p.Property(x => x.Id).ValueGeneratedNever()
    p.Property(x => x.MetodoPago).HasConversion<int>()      // MetodoPago enum (not string)
    p.Property(x => x.FechaPago)
    p.OwnsOne(x => x.Monto, m => {
        m.Property(d => d.Monto).HasColumnName("Monto_Monto").HasColumnType("decimal(18,2)")
        m.Property(d => d.Moneda).HasColumnName("Monto_Moneda").HasConversion<int>()
    })
    p.Navigation(x => x.Monto).IsRequired()
})
b.Navigation(f => f.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field)
```

> **Critical design decisions to enforce**:
> - `TipoComprobante` is a plain int column — NOT `HasDiscriminator`. `Factura` is a single CLR class, not a hierarchy.
> - `Factura.Estado` is `EstadoFactura` enum — there is NO `bool Cancelada` field. Do not attempt to map `Cancelada`.
> - `Pago.MetodoPago` is the `MetodoPago` enum (verified in `Facturacion/Pago.cs`) — apply `HasConversion<int>()`.
> - `VencimientoCAE` is `DateOnly?` — EF 8 maps this as a nullable `date` column natively.
> - Computed totals (`SubTotal`, `TotalIVA`, `Total`, `TotalPagado`, `EstaPagada`) MUST all be `Ignore()`d.

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-10** (Scenarios 10-A through 10-D) — flat table config; discriminator column; nullable CAE; `PedidosFacturados` JSON.
- Design §3.9, §D4, §D5 — flat table, no TPH, JSON primitive collection.

---

### PE-21 — IFacturaRepository implementation

**Work unit:** One repository implementation.
**Conventional commit:** `feat(infra): add FacturaRepository implementation`

#### What to do

1. **`FacturaRepository.cs`** — implements `IFacturaRepository`:
   - `GetByIdAsync`: `_ctx.Facturas.FirstOrDefaultAsync(f => f.Id == id, ct)` (owned `Lineas` and `Pagos` load with root)
   - `AddAsync`: `await _ctx.Facturas.AddAsync(factura, ct)`

2. Register in `AddInfrastructure`.

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-12** — `IFacturaRepository` per-aggregate interface.
- Design §4 — specific repository, no generic base.

---

### PE-22 — EfectivoPrecioService + CalculadorFactura (Application)

**Work unit:** Two Application service implementations required by `CrearFactura`.
**Conventional commit:** `feat(app): add EfectivoPrecioService and CalculadorFactura implementations`

#### What to do

1. **`src/GastroGestion.Application/Services/EfectivoPrecioService.cs`** — implements `IEfectivoPrecioService`:
   - Needs `IMenuRepository` + `IPlatoRepository`
   - Resolution rule: for `platoId` + `fecha`, find active `Menu` where `FechaVigencia >= fecha`; if a matching `MenuItem` has a non-null `PrecioOverride`, return it; else return `Plato.PrecioBase`
   - IVA always comes from `Plato.AlicuotaIVA`
   - Return type: `(Dinero Precio, PorcentajeIVA IVA)` matching the domain interface

2. **`src/GastroGestion.Application/Services/CalculadorFactura.cs`** — implements `ICalculadorFactura`:
   - Pure computation: groups `FacturaLinea` items by `IVA.Alicuota`, builds `DesglosIVA` per group, sums to `ResultadoFactura`
   - `TipoComprobante.TicketInterno` forces all lines to `PorcentajeIVA.Cero` (matching `Factura.CrearTicket` which already forces this; ensure consistency)
   - No repository dependencies

3. Register both in `AddApplication`.

#### Verification

```powershell
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- Design §5 — `EfectivoPrecioService` and `CalculadorFactura` minimal implementations required by `CrearFactura`.

---

### PE-23 — CrearFactura use case + ConflictException + TipoComprobanteSolicitado

**Work unit:** The one Application use case that closes REQ-11/REQ-13-G.
**Conventional commit:** `feat(app): add CrearFactura use case with multi-client ConflictException guard`

#### What to do

1. **`src/GastroGestion.Application/Common/Exceptions/ConflictException.cs`**:
   ```csharp
   public sealed class ConflictException : Exception
   {
       public ConflictException(string message) : base(message) { }
   }
   ```

2. **`src/GastroGestion.Application/Facturacion/CrearFactura/TipoComprobanteSolicitado.cs`**:
   ```csharp
   public enum TipoComprobanteSolicitado
   {
       TicketInterno,
       FacturaConIVA,
       FacturaElectronica
   }
   ```

3. **`src/GastroGestion.Application/Facturacion/CrearFactura/CrearFacturaCommand.cs`**:
   ```csharp
   public sealed record CrearFacturaCommand(
       Guid ClienteId,
       IReadOnlyList<Guid> PedidoIds,
       TipoComprobanteSolicitado Tipo);
   ```

4. **`src/GastroGestion.Application/Facturacion/CrearFactura/CrearFacturaHandler.cs`**:
   - Constructor: `IPedidoRepository`, `IFacturaRepository`, `ICalculadorFactura`, `IUnitOfWork`
   - `Handle(CrearFacturaCommand, CancellationToken)`:
     1. Validate `PedidoIds` non-null/non-empty (throw `ConflictException`)
     2. `var pedidos = await _pedidos.GetByIdsAsync(cmd.PedidoIds, ct)`
     3. If `pedidos.Count != cmd.PedidoIds.Count` → throw `ConflictException("One or more Pedidos were not found.")`
     4. If `pedidos.Any(p => p.ClienteId != cmd.ClienteId)` → throw `ConflictException("All Pedidos in a Factura must belong to the same ClienteId (REQ-13-G).")`
     5. Build `List<FacturaLinea>` from confirmed Pedido lines
     6. `cmd.Tipo switch` → call `Factura.CrearTicket/CrearFacturaConIVA/CrearFacturaElectronica`
     7. `await _facturas.AddAsync(factura, ct)`
     8. `await _uow.SaveChangesAsync(ct)` — commits + dispatches `FacturaNecesitaCAE` for electronic
     9. Return `factura.Id`

5. Register `CrearFacturaHandler` in `AddApplication` (scoped).

#### Verification

```powershell
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-11** (Scenarios 11-A, 11-B, 11-C) — same-client accepted; mixed-client → `ConflictException`; missing Pedido → `ConflictException`.
- Design §5, §D11 — REQ-13/13-G enforced at Application boundary.

---

### PE-24 — AddFactura migration

**Work unit:** Generate and apply third migration covering Factura, FacturaLineas, FacturaPagos tables.
**Conventional commit:** `feat(infra): add AddFactura EF Core migration`

#### What to do

```powershell
dotnet ef migrations add AddFactura `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api `
    --output-dir Persistence/Migrations

dotnet ef database update `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api
```

Review migration for:
- `Facturas` table: `TipoComprobante int`, `Estado int`, `CAE nvarchar(14) NULL`, `VencimientoCAE date NULL`, `PedidosFacturados` JSON column
- `FacturaLineas` table: `PrecioUnitario_Monto decimal(18,2)`, `IVA_Alicuota int`
- `FacturaPagos` table: `MetodoPago int`, `Monto_Monto decimal(18,2)`
- No `Cancelada bool` column (no such CLR member)
- No EF discriminator infrastructure columns (no `Discriminator` or TPH shadow keys)

#### Verification

```powershell
dotnet ef database update `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api
# Expected: exit 0

dotnet ef database update `
    --project src/GastroGestion.Infrastructure `
    --startup-project src/GastroGestion.Api
# Expected: exit 0, no structural changes (idempotent)
```

#### Spec requirements satisfied

- **REQ-01** (Scenario 01-A) — schema now contains all 8 aggregate tables.
- Design §6 — `AddFactura` migration.

---

### PE-25 — Factura round-trip + CrearFactura integration tests

**Work unit:** Full Slice C integration test coverage.
**Conventional commit:** `test(infra): add Slice C Factura round-trip and CrearFactura use case tests`

#### What to do

1. **`tests/GastroGestion.Infrastructure.Tests/Persistence/FacturaRoundTripTests.cs`**:
   - `TicketInterno_Persists_WithNullCae` → Scenario 10-A
   - `FacturaConIVA_Persists_WithNullCae` → Scenario 10-B
   - `FacturaElectronica_AcceptsCaeAssignment_AfterReload` → Scenario 10-C (`AsignarCae` + second save)
   - `PedidosFacturados_JsonColumn_RoundTrips_ThreeGuids` → Scenario 10-D

2. **`tests/GastroGestion.Infrastructure.Tests/Application/CrearFacturaTests.cs`**:
   - `SameClientPedidos_CreatesFactura` → Scenario 11-A
   - `MixedClientPedidos_ThrowsConflictException` → Scenario 11-B
   - `NonExistentPedido_ThrowsConflictException` → Scenario 11-C

3. **Integration test for full integration suite** (within one of the test classes):
   - `AllIntegrationTests_PassOnLocalDb` → Scenario 13-A (smoke — confirmed by passing all tests)
   - `MigrateAsync_AppliesSchema_BeforeTests` → Scenario 13-B (verified by `LocalDbFixture.InitializeAsync`)

#### Verification

```powershell
dotnet test tests/GastroGestion.Infrastructure.Tests/ `
    --logger "console;verbosity=normal"
# Expected: ALL Slice A + B + C tests pass; 0 failed; 0 skipped
```

#### Spec requirements satisfied

- **REQ-10** (10-A through 10-D) — Factura flat table, discriminator, nullable CAE, JSON.
- **REQ-11** (11-A, 11-B, 11-C) — `CrearFactura` multi-client guard.
- **REQ-13** (13-A, 13-B) — integration tests on LocalDB; `MigrateAsync` in fixture.

---

### PE-26 — Slice C build + test verification gate

**Work unit:** Verification-only — no code commits. Confirms Slice C is shippable as PR #3.

#### Verification commands

```powershell
# All three projects build
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0 all

# Full integration test suite
dotnet test tests/GastroGestion.Infrastructure.Tests/ `
    --logger "console;verbosity=normal"
# Expected: all Slice A + B + C tests pass; 0 failed; 0 skipped

# Domain tests still green (domain change must not break anything)
dotnet test tests/GastroGestion.Domain.Tests/ `
    --logger "console;verbosity=normal"
# Expected: all pass; 0 failed

# Final zero-dep gate
Select-String `
    -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: no matches

# Design reviewer checklist spot-checks
# (a) Flat Facturas table — no EF discriminator column
# (b) PedidosFacturados is a JSON column, not a junction table
# (c) MovimientoStock.Cantidad is decimal(18,3), not a VO
# (d) Connection string is config-driven — no hardcoded server in non-factory code
```

---

## Parallel vs. sequential summary

| Task | Can run in parallel with | Blocked by |
|------|--------------------------|------------|
| PE-01 | — (gate first) | — |
| PE-02 | — | PE-01 gate pass |
| PE-03 | — | PE-02 |
| PE-04 | PE-05 (independently, after PE-03) | PE-03 |
| PE-05 | PE-04 | PE-03 |
| PE-06 | PE-07 | PE-05 (MenuItem references Plato concept) |
| PE-07 | PE-06 | PE-03 |
| PE-08 | — | PE-04, PE-05, PE-06, PE-07 all done |
| PE-09 | — | PE-08 |
| PE-10 | — | PE-09 |
| PE-11 | — (verification gate) | PE-10 |
| PE-12 | — | PE-11 gate pass |
| PE-13 | — | PE-12 |
| PE-14 | PE-15 (no mutual dependency) | PE-13 |
| PE-15 | PE-14 | PE-13 |
| PE-16 | — | PE-14, PE-15 both done |
| PE-17 | — | PE-16 |
| PE-18 | — | PE-17 |
| PE-19 | — (verification gate) | PE-18 |
| PE-20 | — | PE-19 gate pass |
| PE-21 | PE-22 (no mutual dependency) | PE-20 |
| PE-22 | PE-21 | PE-20 |
| PE-23 | — | PE-21, PE-22 both done |
| PE-24 | — | PE-23 |
| PE-25 | — | PE-24 |
| PE-26 | — (verification gate) | PE-25 |

---

## Requirement → task traceability

| REQ | Spec scenarios | Satisfied by |
|-----|----------------|--------------|
| REQ-01 | 01-A, 01-B | PE-09 (InitialCatalogue), PE-17 (AddPedidoAndStock), PE-24 (AddFactura), PE-11/PE-19/PE-26 gates |
| REQ-02 | 02-A, 02-B | PE-01 (zero-dep gate), PE-12 (domain change without package ref), PE-11/PE-19/PE-26 gates |
| REQ-03 | 03-A, 03-B, 03-C | PE-04 (ClienteConfig), PE-05 (PlatoConfig), PE-07 (MesaConfig), PE-10 (round-trip tests) |
| REQ-04 | 04-A, 04-B, 04-C, 04-D | PE-03 (converters), PE-05 (Dinero nested), PE-06 (nullable Dinero), PE-13 (DireccionEntrega nullable), PE-10/PE-18 (tests) |
| REQ-05 | 05-A, 05-B | PE-12 (expose PrecioConfirmado), PE-13 (map PrecioConfirmado column), PE-18 (round-trip tests) |
| REQ-06 | 06-A, 06-B | PE-13 (PedidoConfig owned graph + JSON snapshot), PE-18 (round-trip tests) |
| REQ-07 | 07-A, 07-B, 07-C, 07-D | PE-08 (IMovimientoStockRepository Add-only), PE-14 (config + guard), PE-16 (repo impl), PE-18 (guard tests) |
| REQ-08 | 08-A, 08-B, 08-C | PE-02 (DbContext SaveChanges override), PE-15 (IDomainEventDispatcher + InProcess impl), PE-18 (dispatch tests) |
| REQ-09 | 09-A, 09-B | PE-07 (Mesa RowVersion), PE-13 (Pedido RowVersion), PE-18 (concurrency test) |
| REQ-10 | 10-A, 10-B, 10-C, 10-D | PE-20 (FacturaConfig flat table + discriminator + JSON), PE-25 (Factura round-trip tests) |
| REQ-11 | 11-A, 11-B, 11-C | PE-23 (CrearFacturaHandler ConflictException guard), PE-25 (CrearFactura tests) |
| REQ-12 | 12-A, 12-B | PE-08 (repos + UoW ports), PE-16 (Pedido repo loads full graph), PE-21 (Factura repo), PE-18/PE-25 (tests) |
| REQ-13 | 13-A, 13-B | PE-10 (LocalDbFixture + MigrateAsync), PE-25 (full suite passes), PE-26 (final gate) |
| Modified REQ-08 (PrecioConfirmado) | 08-A, 08-B | PE-12 (expose internal property), PE-13 (map via EF), PE-18 (set-once round-trip test) |

---

## Review Workload Forecast

### Estimated changed lines per slice

| Slice | Tasks | Est. new files | Est. additions | Est. deletions | Notes |
|-------|-------|----------------|----------------|----------------|-------|
| Slice A — Foundation | PE-01 through PE-11 | ~20 files | ~700–900L | ~0 | Projects/csproj (~50L), DbContext+Factory+NullDispatcher (~150L), 4 converters (~80L), 5 entity configs (~250L), 8 port interfaces (~80L), 5 repo impls + UoW (~100L), migration (~100L), LocalDbFixture + 3 test classes (~200L). |
| Slice B — Transactional | PE-12 through PE-19 | ~10 files | ~600–800L | ~5L | Domain 1-line change (~5L), PedidoConfig (~150L), MovimientoStockConfig (~60L), IDomainEventDispatcher + InProcess + Capturing (~60L), 2 repo impls (~60L), migration (~80L), 3 test classes (~250L). |
| Slice C — Fiscal | PE-20 through PE-26 | ~12 files | ~450–550L | ~0 | FacturaConfig (~130L), FacturaRepo (~30L), EfectivoPrecioService + CalculadorFactura (~120L), ConflictException + command + handler (~100L), migration (~80L), 2 test classes (~150L). |
| **Total** | **PE-01 through PE-26** | **~42 files** | **~1,750–2,250L** | **~5L** | Consistent with design estimate of ~1,500–2,200L. |

### 400-line budget analysis

| Metric | Value |
|--------|-------|
| Slice A additions | ~800L (estimate midpoint) |
| Slice B additions | ~700L (estimate midpoint) |
| Slice C additions | ~500L (estimate midpoint) |
| **Total** | **~2,000L** |
| 400-line budget risk — Slice A | **High** (~800L >> 400L) |
| 400-line budget risk — Slice B | **High** (~700L >> 400L) |
| 400-line budget risk — Slice C | **High** (~500L > 400L) |
| Chained PRs recommended | **Yes** |
| Decision needed before apply | **Yes — already resolved: stacked-to-main, 3 PRs** |

### PR mapping

| PR | Slice | Tasks | Est. diff | Branch | Review focus |
|----|-------|-------|-----------|--------|--------------|
| PR #1 — Infrastructure Foundation | Slice A | PE-01 → PE-11 | ~800L added | `feat/pe-slice-a` → `main` | EF packages isolated to Infra; DbContext + factory shape; 4 converters; 5 catalogue configs (Dinero column names); UoW/repos; `InitialCatalogue` migration; LocalDbFixture + round-trip tests. |
| PR #2 — Transactional Persistence | Slice B | PE-12 → PE-19 | ~700L added | `feat/pe-slice-b` → `main` | Domain `internal PrecioConfirmado` change; Pedido full config (nested VO, nullable address, JSON, rowversion); MovimientoStock append-only guard; in-process dispatcher seam; concurrency + dispatch tests. |
| PR #3 — Fiscal Persistence | Slice C | PE-20 → PE-26 | ~500L added | `feat/pe-slice-c` → `main` | Factura flat-table config (no TPH, discriminator column); JSON PedidosFacturados; domain services; CrearFactura multi-client guard; final migration; full test suite green. |

**Chain order**: PR #1 → PR #2 (after #1 merges) → PR #3 (after #2 merges). Each PR is independently deployable and test-covered.
