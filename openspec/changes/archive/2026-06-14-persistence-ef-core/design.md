# Technical Design — EF Core 8 Persistence Layer (Phase 3 of 7)

This design locks the **HOW at architecture level** for giving the completed Phase-2 domain a persistence skin: where each file lives, how `GastroGestionDbContext` is shaped, the exact `IEntityTypeConfiguration<T>` strategy per aggregate (with the tricky owned/JSON/converter/rowversion mappings resolved to code-sketch level), the repository / UnitOfWork / dispatcher contracts, the `CrearFactura` use case that closes REQ-13/13-G, the migration + LocalDB integration-test strategy, and the three chained PRs.

> Scope guard: this document does NOT write the spec scenarios (that is `sdd-spec`) and does NOT enumerate implementation steps (that is `sdd-tasks`). It decides shapes, boundaries, and rationale. Confirmed decisions from the proposal are designed AROUND, not re-opened.

> Domain is authoritative over the spec text. Where the archived `Domain/spec.md` drifted from the shipped code (e.g. `Cliente` has no `Apellido`/`Telefono`/`int NumeroCliente`; the concurrency token is named `RowVersion` not `ConcurrencyToken`; `Pedido.Tipo` not `TipoPedido`; `MovimientoStock.Tipo`/`FechaMovimiento`; `VencimientoCAE` is `DateOnly?`), this design maps the **actual shipped CLR shape**. Every column name and field reference below was verified against `src/GastroGestion.Domain/`.

---

## Quick path (what gets built, in slice order)

1. **Slice A — Foundation.** EF Core 8 + SQL Server packages on Infrastructure only; `GastroGestionDbContext` + `IDesignTimeDbContextFactory`; single-field value converters; catalogue + Cliente configurations (Cliente/Direccion, Ingrediente, Plato/LineaReceta, Menu/MenuItem, Mesa); `IUnitOfWork` + catalogue repositories; **first migration**; LocalDB integration-test harness.
2. **Slice B — Transactional.** `Pedido` configuration (owned `LineaPedido`/`OrdenTrabajo`, nullable `DireccionEntrega`, `PrecioConfirmado`, `RowVersion`, `LineaRecetaSnapshot` JSON), `MovimientoStock` configuration + append-only repository + SaveChanges guard, `IDomainEventDispatcher` in-process post-commit dispatch. Depends on A.
3. **Slice C — Fiscal.** `Factura` configuration (flat table + `TipoComprobante` discriminator column, nullable CAE, `PedidosFacturados` JSON primitive collection), owned `FacturaLinea`/`Pago`, `IFacturaRepository`, `CrearFactura` use case + REQ-13/13-G `ConflictException`. Depends on A and B.

Each slice ends green on `(localdb)\mssqllocaldb` and is independently revertible.

---

## 1. Project layout & file placement

All EF concern lives in `Infrastructure`. Ports and the one use case live in `Application`. The reference graph from Phase 1 (`Domain ◄ Application ◄ Infrastructure ◄ Api`) is unchanged; this phase only fills the empty projects.

```text
src/
├─ GastroGestion.Domain/                         (1 minimal change — see §9)
│
├─ GastroGestion.Application/                     (ports + 1 use case)
│  ├─ Abstractions/
│  │  ├─ Persistence/
│  │  │  ├─ IUnitOfWork.cs
│  │  │  ├─ IClienteRepository.cs
│  │  │  ├─ IIngredienteRepository.cs
│  │  │  ├─ IPlatoRepository.cs
│  │  │  ├─ IMenuRepository.cs
│  │  │  ├─ IMesaRepository.cs
│  │  │  ├─ IPedidoRepository.cs
│  │  │  ├─ IMovimientoStockRepository.cs   (AddAsync + query only — NO update/delete)
│  │  │  └─ IFacturaRepository.cs
│  │  └─ Events/
│  │     └─ IDomainEventDispatcher.cs
│  ├─ Common/
│  │  └─ Exceptions/
│  │     └─ ConflictException.cs
│  ├─ Facturacion/
│  │  └─ CrearFactura/
│  │     ├─ CrearFacturaCommand.cs
│  │     ├─ CrearFacturaHandler.cs             (REQ-13/13-G enforcement)
│  │     └─ TipoComprobanteSolicitado.cs       (input enum mirror)
│  ├─ Services/
│  │  ├─ EfectivoPrecioService.cs              (impl of Domain IEfectivoPrecioService)
│  │  └─ CalculadorFactura.cs                  (impl of Domain ICalculadorFactura)
│  └─ DependencyInjection.cs                   (AddApplication)
│
├─ GastroGestion.Infrastructure/                 (EF Core home)
│  ├─ Persistence/
│  │  ├─ GastroGestionDbContext.cs
│  │  ├─ GastroGestionDbContextFactory.cs       (IDesignTimeDbContextFactory)
│  │  ├─ Configurations/
│  │  │  ├─ ClienteConfiguration.cs
│  │  │  ├─ IngredienteConfiguration.cs
│  │  │  ├─ PlatoConfiguration.cs
│  │  │  ├─ MenuConfiguration.cs
│  │  │  ├─ MesaConfiguration.cs
│  │  │  ├─ PedidoConfiguration.cs
│  │  │  ├─ MovimientoStockConfiguration.cs
│  │  │  └─ FacturaConfiguration.cs
│  │  ├─ Converters/
│  │  │  ├─ CuitConverter.cs
│  │  │  ├─ EmailConverter.cs
│  │  │  ├─ LegajoIdConverter.cs
│  │  │  └─ PorcentajeIvaConverter.cs
│  │  ├─ Repositories/
│  │  │  ├─ ClienteRepository.cs … FacturaRepository.cs
│  │  │  ├─ MovimientoStockRepository.cs        (Add + balance query only)
│  │  │  └─ UnitOfWork.cs
│  │  └─ Migrations/                            (dotnet ef output)
│  ├─ Events/
│  │  └─ InProcessDomainEventDispatcher.cs
│  └─ DependencyInjection.cs                    (AddInfrastructure — DbContext, repos, dispatcher)
│
└─ GastroGestion.Api/
   ├─ Program.cs                                (calls AddApplication + AddInfrastructure; MigrateAsync on startup in Dev)
   ├─ appsettings.json                          (ConnectionStrings:GastroGestion empty placeholder)
   └─ appsettings.Development.json              (LocalDB connection string)

tests/
└─ GastroGestion.Infrastructure.Tests/          (NEW — LocalDB integration)
   ├─ GastroGestion.Infrastructure.Tests.csproj
   ├─ Common/
   │  └─ LocalDbFixture.cs                       (per-class DB create/migrate/drop)
   ├─ Persistence/
   │  ├─ ClienteRoundTripTests.cs
   │  ├─ PedidoRoundTripTests.cs                 (nested VO, nullable address, PrecioConfirmado, snapshot JSON, rowversion)
   │  ├─ MovimientoStockGuardTests.cs            (append-only)
   │  ├─ FacturaRoundTripTests.cs                (discriminator, CAE, PedidosFacturados JSON)
   │  └─ DomainEventDispatchTests.cs
   └─ Application/
      └─ CrearFacturaTests.cs                    (REQ-13/13-G ConflictException)
```

### DI wiring contract

| Project | Method | Registers |
|---|---|---|
| `Infrastructure` | `AddInfrastructure(IServiceCollection, IConfiguration)` | `DbContext` via `UseSqlServer(ConnectionStrings:GastroGestion)`, all repositories (scoped), `IUnitOfWork`→`UnitOfWork`, `IDomainEventDispatcher`→`InProcessDomainEventDispatcher` |
| `Application` | `AddApplication(IServiceCollection)` | `IEfectivoPrecioService`, `ICalculadorFactura`, `CrearFacturaHandler` (scoped) |
| `Api` | `Program.cs` | `builder.Services.AddApplication().AddInfrastructure(builder.Configuration)`; in Development, `await db.Database.MigrateAsync()` on a startup scope |

**Decision — repositories register the same `DbContext` instance.** `UnitOfWork`, every repository, and the dispatcher resolve the one scoped `GastroGestionDbContext`, so a request shares a single change-tracker and a single `SaveChangesAsync`. `IUnitOfWork.SaveChangesAsync` simply delegates to the context (which runs the override in §2).

---

## 2. `GastroGestionDbContext` design

```csharp
public sealed class GastroGestionDbContext : DbContext
{
    private readonly IDomainEventDispatcher _dispatcher;

    public GastroGestionDbContext(
        DbContextOptions<GastroGestionDbContext> options,
        IDomainEventDispatcher dispatcher) : base(options)
        => _dispatcher = dispatcher;

    public DbSet<Cliente>          Clientes          => Set<Cliente>();
    public DbSet<Ingrediente>      Ingredientes      => Set<Ingrediente>();
    public DbSet<Plato>            Platos            => Set<Plato>();
    public DbSet<Menu>             Menus             => Set<Menu>();
    public DbSet<Mesa>             Mesas             => Set<Mesa>();
    public DbSet<Pedido>           Pedidos           => Set<Pedido>();
    public DbSet<MovimientoStock>  MovimientosStock  => Set<MovimientoStock>();
    public DbSet<Factura>          Facturas          => Set<Factura>();
    // No DbSet for owned types (LineaPedido, OrdenTrabajo, FacturaLinea, Pago,
    // Direccion, LineaReceta, MenuItem) — reached only through their owner.

    protected override void OnModelCreating(ModelBuilder b)
        => b.ApplyConfigurationsFromAssembly(typeof(GastroGestionDbContext).Assembly);

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        GuardAppendOnlyLedger();                     // (a) reject ledger mutation/delete
        var roots = CollectAggregatesWithEvents();   // (b) snapshot event-bearing roots
        var result = await base.SaveChangesAsync(ct); // (c) commit
        await DispatchAndClear(roots, ct);            // (d) post-commit, in-process
        return result;
    }
}
```

| Concern | Mechanism |
|---|---|
| **(a) Append-only ledger guard** | Iterate `ChangeTracker.Entries<MovimientoStock>()`; if any `State` is `Modified` or `Deleted`, throw `InvalidOperationException("MovimientoStock is append-only; only inserts are permitted.")`. Runs before commit so it never half-writes. |
| **(b) Collect events** | `ChangeTracker.Entries<AggregateRoot>().Where(e => e.Entity.DomainEvents.Count > 0).Select(e => e.Entity).ToList()` — materialise BEFORE commit because EF may detach after save. |
| **(c) Commit** | `base.SaveChangesAsync` — single transaction for the whole change set. |
| **(d) Dispatch + clear** | For each collected root: `await _dispatcher.DispatchAsync(root.DomainEvents, ct)` then `root.ClearDomainEvents()`. Post-commit and in-process (outbox deferred). Domain never clears its own events (per `AggregateRoot`). |

**Decision — in-process post-commit dispatch, no transaction enrolment of handlers.** Matches the locked decision and the exploration's Option A. The `FacturaNecesitaCAE` seam is satisfied (event fires after the Factura row exists, so a future AFIP handler can `AsignarCae` and save again) without any HTTP call inside the transaction. Accepted risk: if the process dies between commit and dispatch, the event is lost — acceptable for Phase 3; the outbox upgrade slots in at the same seam (§Risks).

**Decision — `ApplyConfigurationsFromAssembly`.** One call discovers every `IEntityTypeConfiguration<T>`; adding a config file needs no `OnModelCreating` edit. No conventions are relied on for the tricky shapes.

### `GastroGestionDbContextFactory` (design-time)

`dotnet ef` instantiates the context without DI. The factory reads the connection string from `appsettings.Development.json` (or `ConnectionStrings__GastroGestion` env var) and injects a **no-op dispatcher** (`NullDomainEventDispatcher`) because design-time never dispatches:

```csharp
public sealed class GastroGestionDbContextFactory
    : IDesignTimeDbContextFactory<GastroGestionDbContext>
{
    public GastroGestionDbContext CreateDbContext(string[] args)
    {
        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = cfg.GetConnectionString("GastroGestion")
            ?? @"Server=(localdb)\mssqllocaldb;Database=GastroGestion;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<GastroGestionDbContext>()
            .UseSqlServer(cs, sql => sql.MigrationsAssembly(typeof(GastroGestionDbContext).Assembly.FullName))
            .Options;

        return new GastroGestionDbContext(options, new NullDomainEventDispatcher());
    }
}
```

---

## 3. Per-aggregate configuration strategy

General rules applied to every config:

- **Backing-field collections**: `b.HasMany(...)` / `b.OwnsMany(...)` with `.UsePropertyAccessMode(PropertyAccessMode.Field)`; navigation metadata uses `Metadata.SetField("_fieldName")` where the property name differs from the field. EF Core matches `Lineas`→`_lineas` by convention but the snapshot/JSON cases need explicit field naming.
- **Surrogate key**: `b.HasKey(x => x.Id)`; `Id` is value-assigned by the domain factory (`Guid.NewGuid()`), so `ValueGeneratedNever()`.
- **Single-field VOs** → value converters (§3.7). **Multi-field VOs** (`Dinero`, `Cantidad`, `DireccionEntrega`) → `OwnsOne` with explicit `HasColumnName` per path. **Record snapshot collections** → `OwnsMany(...).ToJson()`.

### 3.1 Cliente (+ Direccion) — Slice A

```csharp
b.ToTable("Clientes");
b.HasKey(c => c.Id);
b.Property(c => c.Id).ValueGeneratedNever();
b.Property(c => c.NumeroCliente);              // Guid, get-only — EF reads via backing
b.Property(c => c.Nombre).IsRequired().HasMaxLength(200);
b.Property(c => c.CondicionIVA).HasConversion<int>();
b.Property(c => c.Cuit).HasConversion(CuitConverter.Instance).HasMaxLength(11);   // nullable
b.Property(c => c.Email).HasConversion(EmailConverter.Instance).HasMaxLength(320); // nullable
b.Property(c => c.Activo);
b.HasIndex(c => c.Cuit).IsUnique().HasFilter("[Cuit] IS NOT NULL");  // domain uniqueness intent (REQ-03)

b.OwnsMany(c => c.Direcciones, d =>
{
    d.ToTable("ClienteDirecciones");
    d.WithOwner().HasForeignKey("ClienteId");
    d.HasKey("Id");                            // Direccion is an Entity with Guid Id
    d.Property(x => x.Id).ValueGeneratedNever();
    d.Property(x => x.Calle).IsRequired();
    d.Property(x => x.Numero).IsRequired();
    d.Property(x => x.Piso);                   // nullable
    d.Property(x => x.Departamento);           // nullable
    d.Property(x => x.Ciudad).IsRequired();
    d.Property(x => x.Provincia).IsRequired();
    d.Property(x => x.CodigoPostal).IsRequired();
});
b.Navigation(c => c.Direcciones).UsePropertyAccessMode(PropertyAccessMode.Field); // _direcciones
```

> `Direccion` is an owned **entity collection in its own table** (it has a real `Id` and is a 1-to-N child), not flattened. Distinct from `DireccionEntrega` (a flattened VO on Pedido, §3.6).

### 3.2 Ingrediente — Slice A

```csharp
b.ToTable("Ingredientes");
b.HasKey(i => i.Id);
b.Property(i => i.Id).ValueGeneratedNever();
b.Property(i => i.Nombre).IsRequired().HasMaxLength(200);
b.Property(i => i.UnidadBase).HasConversion<int>();
b.Property(i => i.Activo);
b.HasIndex(i => i.Nombre).IsUnique();   // name uniqueness (REQ-04 infra concern)
```

### 3.3 Plato (+ LineaReceta) — Slice A

`PrecioBase` is `Dinero` (owned, 2 columns). `LineaReceta` is an owned entity whose `Cantidad` is itself a nested 2-field VO → nested `OwnsOne`.

```csharp
b.ToTable("Platos");
b.HasKey(p => p.Id);
b.Property(p => p.Id).ValueGeneratedNever();
b.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
b.Property(p => p.AlicuotaIVA).HasConversion<int>();
b.Property(p => p.Activo);

b.OwnsOne(p => p.PrecioBase, m =>
{
    m.Property(d => d.Monto).HasColumnName("PrecioBase_Monto").HasColumnType("decimal(18,2)");
    m.Property(d => d.Moneda).HasColumnName("PrecioBase_Moneda").HasConversion<int>();
});
b.Navigation(p => p.PrecioBase).IsRequired();

b.OwnsMany(p => p.LineasReceta, r =>          // field _lineasReceta
{
    r.ToTable("PlatoLineasReceta");
    r.WithOwner().HasForeignKey("PlatoId");
    r.HasKey("Id");
    r.Property(x => x.Id).ValueGeneratedNever();
    r.Property(x => x.IngredienteId);
    r.Property(x => x.PlatoReferenciadoId);   // nullable combo seam
    r.OwnsOne(x => x.Cantidad, c =>
    {
        c.Property(q => q.Valor).HasColumnName("Cantidad_Valor").HasColumnType("decimal(18,3)");
        c.Property(q => q.Unidad).HasColumnName("Cantidad_Unidad").HasConversion<int>();
    });
    r.Navigation(x => x.Cantidad).IsRequired();
});
b.Navigation(p => p.LineasReceta).UsePropertyAccessMode(PropertyAccessMode.Field);
```

### 3.4 Menu (+ MenuItem) — Slice A

`MenuItem.PrecioOverride` is a **nullable** `Dinero` owned type → `OwnsOne(...).Navigation(...).IsRequired(false)` (same pattern as §3.6).

```csharp
b.ToTable("Menus");
b.HasKey(m => m.Id);
b.Property(m => m.Id).ValueGeneratedNever();
b.Property(m => m.Nombre).IsRequired().HasMaxLength(200);
b.Property(m => m.FechaVigencia);             // DateOnly → date column
b.Property(m => m.Activo);

b.OwnsMany(m => m.Items, it =>                // field _items
{
    it.ToTable("MenuItems");
    it.WithOwner().HasForeignKey("MenuId");
    it.HasKey("Id");
    it.Property(x => x.Id).ValueGeneratedNever();
    it.Property(x => x.PlatoId);
    it.OwnsOne(x => x.PrecioOverride, m =>
    {
        m.Property(d => d.Monto).HasColumnName("PrecioOverride_Monto").HasColumnType("decimal(18,2)");
        m.Property(d => d.Moneda).HasColumnName("PrecioOverride_Moneda").HasConversion<int>();
    });
    it.Navigation(x => x.PrecioOverride).IsRequired(false);   // nullable override
});
b.Navigation(m => m.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
```

### 3.5 Mesa — Slice A

```csharp
b.ToTable("Mesas");
b.HasKey(m => m.Id);
b.Property(m => m.Id).ValueGeneratedNever();
b.Property(m => m.Numero);
b.Property(m => m.Capacidad);
b.Property(m => m.Estado).HasConversion<int>();   // EstadoMesa
b.Property(m => m.Activa);
b.Property(m => m.PedidoActivoId);                // nullable
b.Property(m => m.RowVersion).IsRowVersion();     // SQL Server rowversion (see §3.8)
b.HasIndex(m => m.Numero).IsUnique();
```

### 3.6 Pedido (+ LineaPedido + OrdenTrabajo) — Slice B (highest mapping complexity)

This is the aggregate that exercises every tricky case: nested VO column collisions, nullable owned type, set-once flag, rowversion, and a record-snapshot JSON collection.

```csharp
b.ToTable("Pedidos");
b.HasKey(p => p.Id);
b.Property(p => p.Id).ValueGeneratedNever();
b.Property(p => p.Tipo).HasConversion<int>();          // TipoPedido
b.Property(p => p.Estado).HasConversion<int>();        // EstadoPedido
b.Property(p => p.MesaId);                              // nullable
b.Property(p => p.ClienteId);                           // nullable
b.Property(p => p.CreadoEnUtc);
b.Property(p => p.RowVersion).IsRowVersion();           // optimistic concurrency (§3.8)

// --- nullable DireccionEntrega owned VO, flattened with explicit names ---
b.OwnsOne(p => p.DireccionEntrega, dir =>
{
    dir.Property(x => x.Calle).HasColumnName("Entrega_Calle");
    dir.Property(x => x.Numero).HasColumnName("Entrega_Numero");
    dir.Property(x => x.Piso).HasColumnName("Entrega_Piso");
    dir.Property(x => x.Departamento).HasColumnName("Entrega_Departamento");
    dir.Property(x => x.Ciudad).HasColumnName("Entrega_Ciudad");
    dir.Property(x => x.Provincia).HasColumnName("Entrega_Provincia");
    dir.Property(x => x.CodigoPostal).HasColumnName("Entrega_CodigoPostal");
});
b.Navigation(p => p.DireccionEntrega).IsRequired(false);  // CRITICAL: Salon/Mostrador have null address

// --- LineaPedido owned collection ---
b.OwnsMany(p => p.Lineas, l =>                          // field _lineas
{
    l.ToTable("PedidoLineas");
    l.WithOwner().HasForeignKey("PedidoId");
    l.HasKey("Id");
    l.Property(x => x.Id).ValueGeneratedNever();
    l.Property(x => x.PlatoId);
    l.Property(x => x.Cantidad);
    l.Property(x => x.Observaciones);                   // nullable

    // nullable PrecioUnitario (Dinero) — distinct column names
    l.OwnsOne(x => x.PrecioUnitario, m =>
    {
        m.Property(d => d.Monto).HasColumnName("PrecioUnitario_Monto").HasColumnType("decimal(18,2)");
        m.Property(d => d.Moneda).HasColumnName("PrecioUnitario_Moneda").HasConversion<int>();
    });
    l.Navigation(x => x.PrecioUnitario).IsRequired(false);

    // nullable IVA (PorcentajeIVA, single-field) — value converter, NOT owned
    l.Property(x => x.IVA).HasConversion(PorcentajeIvaConverter.Instance).HasColumnName("IVA_Alicuota");

    // set-once flag — see §9: maps the new internal property PrecioConfirmado
    l.Property(x => x.PrecioConfirmado).HasColumnName("PrecioConfirmado");

    // SubtotalLinea / IVALinea / TotalLinea are computed getters → Ignore
    l.Ignore(x => x.SubtotalLinea);
    l.Ignore(x => x.IVALinea);
    l.Ignore(x => x.TotalLinea);
});
b.Navigation(p => p.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field);

// --- OrdenTrabajo owned collection with LineaRecetaSnapshot JSON ---
b.OwnsMany(p => p.OrdenesTrabajo, ot =>                 // field _ordenesTrabajo
{
    ot.ToTable("PedidoOrdenesTrabajo");
    ot.WithOwner().HasForeignKey("PedidoId");
    ot.HasKey("Id");
    ot.Property(x => x.Id).ValueGeneratedNever();
    ot.Property(x => x.PlatoId);
    ot.Property(x => x.LineaPedidoId);
    ot.Property(x => x.Estado).HasConversion<int>();    // EstadoOT
    ot.Property(x => x.CocineroAsignado)                // LegajoId? single-field VO
      .HasConversion(LegajoIdConverter.Instance).HasColumnName("CocineroAsignado");

    // RecetaSnapshot: IReadOnlyList<LineaRecetaSnapshot> (record with nested Cantidad VO) → JSON
    ot.OwnsMany(x => x.RecetaSnapshot, snap =>
    {
        snap.ToJson();                                  // EF Core 8 JSON column "RecetaSnapshot"
        snap.Property(s => s.IngredienteId);
        snap.OwnsOne(s => s.Cantidad, c =>
        {
            c.Property(q => q.Valor);
            c.Property(q => q.Unidad).HasConversion<int>();
        });
    });
});
b.Navigation(p => p.OrdenesTrabajo).UsePropertyAccessMode(PropertyAccessMode.Field);
```

Resolved tricky points:

| Problem | Resolution |
|---|---|
| Nested `Dinero` collides if two owned `Dinero` share `Monto`/`Moneda` column names | Explicit `HasColumnName` per path (`PrecioUnitario_Monto`, `PrecioBase_Monto`, `PrecioOverride_Monto`, …). One round-trip test per nested-VO aggregate. |
| Nullable `DireccionEntrega` — owned types non-nullable by default | `b.Navigation(p => p.DireccionEntrega).IsRequired(false)`. Round-trip test for a Salon/Mostrador Pedido (null address) and a Delivery Pedido (full address). |
| `LineaPedido.IVA` is `PorcentajeIVA?` (single-field) | Value converter, not owned. Stored as the `AlicuotaIVA` int in `IVA_Alicuota`, nullable. |
| `LineaRecetaSnapshot` is a `sealed record` carrying a `Cantidad` VO | `OwnsMany(...).ToJson()`; nested `Cantidad` serialised inside the JSON. Audit-only, never filtered in SQL. Round-trip test asserts the snapshot survives a later Plato recipe edit (REQ-10-E). |
| Computed getters (`SubtotalLinea`, etc.) | `Ignore(...)` so EF does not try to map/persist them. |

### 3.7 MovimientoStock — Slice B (append-only)

```csharp
b.ToTable("MovimientosStock");
b.HasKey(m => m.Id);
b.Property(m => m.Id).ValueGeneratedNever();
b.Property(m => m.IngredienteId);
b.Property(m => m.Cantidad).HasColumnType("decimal(18,3)");  // signed
b.Property(m => m.Tipo).HasConversion<int>();                // TipoMovimientoStock
b.Property(m => m.FechaMovimiento);
b.Property(m => m.OrdenTrabajoId);   // nullable
b.Property(m => m.LineaPedidoId);    // nullable
b.Property(m => m.Lote);             // nullable
b.Property(m => m.FechaVencimiento); // DateOnly? → nullable date
b.HasIndex(m => m.IngredienteId);    // balance query SUM(Cantidad) WHERE IngredienteId = @id
```

Enforcement is at two layers (both required, per exploration Option A+B):
1. **Repository** (`IMovimientoStockRepository`) exposes only `AddAsync` + balance query — no `Update`/`Remove`.
2. **DbContext guard** (`SaveChangesAsync` §2a) rejects any `Modified`/`Deleted` `MovimientoStock` entry, catching mutation that bypasses the repository.

`CalcularDisponible` stays the domain projection; the repository balance query is `_ctx.MovimientosStock.Where(m => m.IngredienteId == id).SumAsync(m => m.Cantidad, ct)`. Materialised balance table deferred (Phase 5+).

### 3.8 RowVersion (Pedido, Mesa) — Slice B

Both expose `public byte[] RowVersion { get; private set; } = []`. Config: `b.Property(x => x.RowVersion).IsRowVersion();` maps to SQL Server `rowversion` (8-byte, DB-generated, auto-incremented on update). The domain default `[]` is irrelevant — EF marks it store-generated and never sends it on insert. Concurrency test: load the same Pedido in two contexts, save both, assert the second throws `DbUpdateConcurrencyException`.

### 3.9 Factura (+ FacturaLinea + Pago + PedidosFacturados) — Slice C (highest product risk)

Flat single table. `TipoComprobante` is a **plain discriminator column** (an `int`), NOT an EF inheritance hierarchy (`Factura` is a single class — no `HasDiscriminator`, no TPH/TPT). Nullable `CAE`/`VencimientoCAE`. `PedidosFacturados` (`List<Guid>` backing `_pedidosFacturados`) → JSON primitive collection via `HasField`.

```csharp
b.ToTable("Facturas");
b.HasKey(f => f.Id);
b.Property(f => f.Id).ValueGeneratedNever();
b.Property(f => f.TipoComprobante).HasConversion<int>();   // discriminator COLUMN, not EF inheritance
b.Property(f => f.Estado).HasConversion<int>();            // EstadoFactura
b.Property(f => f.ClienteId);
b.Property(f => f.FechaAlta);
b.Property(f => f.CAE).HasMaxLength(14);                   // nullable — only FacturaElectronica
b.Property(f => f.VencimientoCAE);                          // DateOnly? nullable date

// computed totals (SubTotal, TotalIVA, Total, TotalPagado, EstaPagada) → Ignore
b.Ignore(f => f.SubTotal);
b.Ignore(f => f.TotalIVA);
b.Ignore(f => f.Total);
b.Ignore(f => f.TotalPagado);
b.Ignore(f => f.EstaPagada);

// PedidosFacturados: List<Guid> backing field _pedidosFacturados → JSON primitive collection
b.PrimitiveCollection<IReadOnlyList<Guid>>("PedidosFacturados")
    .HasField("_pedidosFacturados")
    .UsePropertyAccessMode(PropertyAccessMode.Field)
    .ToJson("PedidosFacturados");

// FacturaLinea owned: nested Dinero + PorcentajeIVA value converter
b.OwnsMany(f => f.Lineas, l =>                             // field _lineas
{
    l.ToTable("FacturaLineas");
    l.WithOwner().HasForeignKey("FacturaId");
    l.HasKey("Id");
    l.Property(x => x.Id).ValueGeneratedNever();
    l.Property(x => x.LineaPedidoId);
    l.Property(x => x.Cantidad);
    l.OwnsOne(x => x.PrecioUnitario, m =>
    {
        m.Property(d => d.Monto).HasColumnName("PrecioUnitario_Monto").HasColumnType("decimal(18,2)");
        m.Property(d => d.Moneda).HasColumnName("PrecioUnitario_Moneda").HasConversion<int>();
    });
    l.Navigation(x => x.PrecioUnitario).IsRequired();
    l.Property(x => x.IVA).HasConversion(PorcentajeIvaConverter.Instance).HasColumnName("IVA_Alicuota");
    l.Ignore(x => x.Subtotal);
    l.Ignore(x => x.SubtotalConIVA);
});
b.Navigation(f => f.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field);

// Pago owned
b.OwnsMany(f => f.Pagos, p =>                             // field _pagos
{
    p.ToTable("FacturaPagos");
    p.WithOwner().HasForeignKey("FacturaId");
    p.HasKey("Id");
    p.Property(x => x.Id).ValueGeneratedNever();
    p.Property(x => x.MetodoPago).HasConversion<int>();
    p.Property(x => x.FechaPago);
    p.OwnsOne(x => x.Monto, m =>
    {
        m.Property(d => d.Monto).HasColumnName("Monto_Monto").HasColumnType("decimal(18,2)");
        m.Property(d => d.Moneda).HasColumnName("Monto_Moneda").HasConversion<int>();
    });
    p.Navigation(x => x.Monto).IsRequired();
});
b.Navigation(f => f.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field);
```

> Note on the misleading code comment: `Factura.cs` says "TPH / EF Core TPH mapping configured in phase 3." Confirmed FALSE for EF purposes — there is no class hierarchy. This design maps a flat table with a discriminator-as-data column. No `HasDiscriminator` is configured.

### 3.10 Value converters (single-field VOs) — Slice A

| VO | Stored as | Converter |
|---|---|---|
| `Cuit` | `nvarchar(11)` (raw digits) | `v => v.Valor`, `s => new Cuit(s)` |
| `Email` | `nvarchar(320)` (normalised) | `v => v.Valor`, `s => new Email(s)` |
| `LegajoId` | `uniqueidentifier` | `v => v.Valor`, `g => new LegajoId(g)` |
| `PorcentajeIVA` | `int` (`AlicuotaIVA`) | `v => (int)v.Alicuota`, `i => new PorcentajeIVA((AlicuotaIVA)i)` |

Each is a `ValueConverter<TVo, TStore>` exposing a static `Instance`. The VO constructors re-validate on read (defence in depth). `PorcentajeIVA` round-trips through its `AlicuotaIVA` enum, not the decimal `Tasa` (the decimal is derived).

---

## 4. Repository, UnitOfWork & dispatcher contracts

**Decision — specific repositories per aggregate, no generic `IRepository<T>` base.** A generic base would leak `UpdateAsync`/`DeleteAsync` onto `MovimientoStock`, breaking the append-only invariant at the type level. Each interface exposes only what its aggregate needs.

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Cliente cliente, CancellationToken ct = default);
    // Update is implicit via the change tracker; no explicit Update needed for tracked roots.
}
// IIngrediente/IPlato/IMenu/IMesa/IPedido follow the same shape (GetByIdAsync + AddAsync).

public interface IPedidoRepository
{
    Task<Pedido?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Pedido>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default); // for CrearFactura
    Task AddAsync(Pedido pedido, CancellationToken ct = default);
}

public interface IMovimientoStockRepository           // append-only — NO Update/Remove
{
    Task AddAsync(MovimientoStock movimiento, CancellationToken ct = default);
    Task<decimal> CalcularBalanceAsync(Guid ingredienteId, CancellationToken ct = default);
}

public interface IFacturaRepository
{
    Task<Factura?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Factura factura, CancellationToken ct = default);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default);
}
```

Implementation notes:
- Repositories take the scoped `GastroGestionDbContext`. `GetByIdAsync` uses `Include`/`ThenInclude` only where the aggregate needs eager owned loading; owned types load automatically with the root, so most reads are `await _ctx.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct)`.
- `UnitOfWork.SaveChangesAsync` delegates to `_ctx.SaveChangesAsync(ct)`, which runs the override (guard + dispatch) in §2.
- `InProcessDomainEventDispatcher` for Phase 3 is a minimal walker: it iterates events and (later) resolves typed handlers. With no handlers registered yet, it is effectively a no-op that satisfies the seam and clears events. The seam is wired so Phase 5 can register a `FacturaNecesitaCAE` handler without touching the DbContext.

---

## 5. Domain services & the CrearFactura use case

### IEfectivoPrecioService → `EfectivoPrecioService` (Application)

Implements the Domain interface. Needs `IMenuRepository` + `IPlatoRepository`. Resolution rule (REQ-06): for the given `fecha`, find the active Menu's `MenuItem.PrecioOverride` for the `platoId`; if present and non-null use it, else fall back to `Plato.PrecioBase`. IVA comes from `Plato.AlicuotaIVA`. Returns `(Dinero Precio, PorcentajeIVA IVA)`. Placed in Application (it crosses aggregates via repositories — never inside the domain).

### ICalculadorFactura → `CalculadorFactura` (Application)

Pure computation, no repository. Groups lines by `IVA.Alicuota`, builds `DesglosIVA` per group, sums to `ResultadoFactura`. `TipoComprobante.TicketInterno` forces every line to `PorcentajeIVA.Cero` (matches `Factura.CrearTicket`).

> Both land minimally in this phase only as far as Factura needs them; the full use-case layer is Phase 4 (per proposal "Out of scope").

### CrearFactura — closes REQ-13/13-G (Slice C)

`ConflictException : Exception` (Application/Common/Exceptions) signals a business-rule conflict the API maps to HTTP 409.

```csharp
public sealed record CrearFacturaCommand(
    Guid ClienteId,
    IReadOnlyList<Guid> PedidoIds,
    TipoComprobanteSolicitado Tipo);

public sealed class CrearFacturaHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IFacturaRepository _facturas;
    private readonly ICalculadorFactura _calculador;
    private readonly IUnitOfWork _uow;

    public async Task<Guid> Handle(CrearFacturaCommand cmd, CancellationToken ct)
    {
        if (cmd.PedidoIds is null || cmd.PedidoIds.Count == 0)
            throw new ConflictException("At least one Pedido is required to create a Factura.");

        var pedidos = await _pedidos.GetByIdsAsync(cmd.PedidoIds, ct);

        // REQ-13-G: every loaded Pedido must exist AND belong to the requested ClienteId.
        if (pedidos.Count != cmd.PedidoIds.Count)
            throw new ConflictException("One or more Pedidos were not found.");
        if (pedidos.Any(p => p.ClienteId != cmd.ClienteId))
            throw new ConflictException(
                "All Pedidos in a Factura must belong to the same ClienteId (REQ-13-G).");

        // Build FacturaLinea snapshots from each Pedido's confirmed lines.
        var lineas = BuildLineasFromPedidos(pedidos);

        var factura = cmd.Tipo switch
        {
            TipoComprobanteSolicitado.TicketInterno     => Factura.CrearTicket(cmd.ClienteId, cmd.PedidoIds.ToList(), lineas),
            TipoComprobanteSolicitado.FacturaConIVA      => Factura.CrearFacturaConIVA(cmd.ClienteId, cmd.PedidoIds.ToList(), lineas),
            TipoComprobanteSolicitado.FacturaElectronica => Factura.CrearFacturaElectronica(cmd.ClienteId, cmd.PedidoIds.ToList(), lineas),
            _ => throw new ConflictException($"Unsupported comprobante type: {cmd.Tipo}.")
        };

        await _facturas.AddAsync(factura, ct);
        await _uow.SaveChangesAsync(ct);   // commits + dispatches FacturaNecesitaCAE (electronic)
        return factura.Id;
    }
}
```

The validation runs at the application boundary (loads Pedidos, checks ClienteId) before calling the domain factory — exactly the Phase-2 deferral. The domain factory still does not block multi-client (it cannot load aggregates); this handler is the enforcer.

---

## 6. Migration & integration-test strategy

### Migrations

| Aspect | Decision |
|---|---|
| Tooling | `dotnet ef migrations add <Name> -p src/GastroGestion.Infrastructure -s src/GastroGestion.Api` (startup project = Api for config resolution). |
| Naming | Per slice: `InitialCatalogue` (A), `AddPedidoAndStock` (B), `AddFactura` (C). PascalCase, intent-named. |
| Output | `src/GastroGestion.Infrastructure/Persistence/Migrations/`. |
| Apply (dev) | `dotnet ef database update` or `await db.Database.MigrateAsync()` at Api startup in Development. |
| Apply (test) | `LocalDbFixture` calls `MigrateAsync()` against a per-class database. |
| Greenfield | No legacy ETL. Each migration adds new empty tables (locked Assumption 1). |
| Connection | `ConnectionStrings:GastroGestion`, empty in `appsettings.json`, real value in `appsettings.Development.json` / user-secrets / env. Azure SQL = config-only change later. |

### Integration tests (LocalDB, no Docker)

```text
LocalDbFixture (IAsyncLifetime, per test class):
  InitializeAsync:  DB name = "GastroGestion_Test_{ClassName}_{Guid:N short}"
                    build options → UseSqlServer(localdb, that DB)
                    await db.Database.MigrateAsync()
  DisposeAsync:     await db.Database.EnsureDeletedAsync()   // clean drop per class
```

| Decision | Rationale |
|---|---|
| **One database per test class**, dropped on dispose | Isolation without cross-test bleed; LocalDB create/drop is cheap. No shared mutable state. |
| Real `GastroGestionDbContext` (not in-memory provider) | The in-memory/SQLite providers diverge on `rowversion`, JSON columns, and decimal precision — the exact features under test. LocalDB is the same engine family as Azure SQL. |
| Dispatcher in tests | A capturing test-double `IDomainEventDispatcher` records dispatched events so `DomainEventDispatchTests` can assert post-commit firing. |
| No Testcontainers / CI gating | Locked decision; LocalDB only. Testcontainers is the documented future upgrade when CI lands. |

Coverage targets: round-trip per aggregate (save→reload, invariants intact); nullable `DireccionEntrega`; `PrecioConfirmado` survives reload (reload → second `ConfirmarPrecio` throws); `LineaRecetaSnapshot` JSON survives a later Plato edit; append-only guard (attempt Modify and Delete of a ledger row → both throw); rowversion conflict → `DbUpdateConcurrencyException`; `CrearFactura` multi-client → `ConflictException`.

---

## 7. Slicing into 3 chained PRs (stacked-to-main)

Strict dependency order; each slice independently reviewable and green on LocalDB.

```text
Slice A (Foundation) ──► Slice B (Transactional) ──► Slice C (Fiscal)
   first migration          depends on A schema         depends on A+B
   catalogue + Cliente      Pedido + Stock + dispatcher  Factura + CrearFactura
```

| Slice | Lands | Depends on | Risk |
|---|---|---|---|
| **A — Foundation** | EF packages (Infra only); `GastroGestionDbContext` + DbSets + `IDesignTimeDbContextFactory`; value converters (§3.10); Cliente/Direccion, Ingrediente, Plato/LineaReceta, Menu/MenuItem, Mesa configs; `IUnitOfWork` + catalogue repositories; **`InitialCatalogue` migration**; `LocalDbFixture` + first round-trip tests | — | Low (structural). Establishes the converter/owned patterns everything else reuses. |
| **B — Transactional** | `Pedido` config (owned LineaPedido/OrdenTrabajo, nullable DireccionEntrega, `PrecioConfirmado`, RowVersion, LineaRecetaSnapshot JSON); `MovimientoStock` config + append-only repo + SaveChanges guard; `IDomainEventDispatcher` + `InProcessDomainEventDispatcher`; `AddPedidoAndStock` migration; transactional round-trip + guard + dispatch tests | A | High (mapping complexity: nested VO collisions, nullable owned, JSON snapshot, rowversion). |
| **C — Fiscal** | `Factura` config (flat table + discriminator column, nullable CAE, PedidosFacturados JSON, owned FacturaLinea/Pago); `IFacturaRepository`; `EfectivoPrecioService` + `CalculadorFactura`; `CrearFactura` use case + `ConflictException` (REQ-13/13-G); `AddFactura` migration; Factura round-trip + CrearFactura tests | A, B | High product risk (fiscal); isolated in its own slice. |

**The `LineaPedido.PrecioConfirmado` domain change (§9) ships in Slice B** (the first slice that maps Pedido). It is additive and independently revertible.

Review Workload Forecast: ~1,500–2,200 changed lines total — well above the 400-line single-PR budget. Chained/stacked PRs required (`ask-on-risk` → confirm before apply).

---

## 8. ADR-style decision log

| # | Decision | Alternatives rejected | Rationale |
|---|---|---|---|
| D1 | Per-aggregate `IEntityTypeConfiguration<T>` in Infrastructure, discovered by `ApplyConfigurationsFromAssembly` | Data annotations on domain (REJECTED — adds EF dep, violates REQ-01); convention-only (REJECTED — misses owned/JSON/rowversion/converters) | Zero domain pollution; all EF concern in one place; Clean Architecture innermost-layer rule holds. |
| D2 | Multi-field VOs (`Dinero`, `Cantidad`, `DireccionEntrega`) → `OwnsOne`/`OwnsMany` with explicit `HasColumnName` per path | Single shared column names | Nested owned types collide on `Monto`/`Moneda` etc. across paths; explicit per-path names prevent collision. |
| D3 | Single-field VOs (`Cuit`, `Email`, `LegajoId`, `PorcentajeIVA`) → value converters | Owned types | One primitive value each; owned type is overkill and adds a join/complex type for nothing. |
| D4 | `Factura` = flat single table with `TipoComprobante` as a plain int discriminator **column** | EF TPH/TPT inheritance (`HasDiscriminator`) | Domain is a single class with an enum, NOT a class hierarchy. No EF inheritance applies. The "TPH" code comment is misleading and does not bind EF config. |
| D5 | `Factura.PedidosFacturados` → JSON primitive collection via `ToJson` + `HasField("_pedidosFacturados")` | Junction table `FacturaPedidos` | Audit-style reference list, never filtered in SQL (locked Assumption 2). If "find Facturas for a Pedido" becomes a query, switch to a junction table then. |
| D6 | `LineaRecetaSnapshot` (record + nested Cantidad) → `OwnsMany(...).ToJson()` | Owned entity table with shadow key | It is a `record` audit snapshot, read-once, never queried by field. JSON keeps it inline with the OT and avoids a shadow-keyed child table. |
| D7 | Append-only ledger enforced at BOTH repository (Add-only) and DbContext (`SaveChanges` guard) | Repository only; DB trigger | Repo enforces the contract; the guard catches any bypass via the tracker. DB trigger is non-portable and can't be expressed via EF migrations alone. |
| D8 | In-process post-`SaveChanges` domain-event dispatch | Pre-commit dispatch (WRONG — side effects before persistence); transactional outbox (deferred) | Simple, satisfies the `FacturaNecesitaCAE` seam, no HTTP in the transaction. Outbox slots in at the same seam in the AFIP phase. |
| D9 | `RowVersion` via `IsRowVersion()` on Pedido/Mesa | App-managed token; no concurrency control | SQL Server `rowversion` is DB-generated and free; domain already exposes `byte[] RowVersion`. Optimistic concurrency surfaces `DbUpdateConcurrencyException`. |
| D10 | Specific repositories, no generic `IRepository<T>` | Generic base + specific overrides | A generic base leaks `Update`/`Delete` onto `MovimientoStock`, breaking append-only at the type level. |
| D11 | REQ-13/13-G enforced in `CrearFactura` (Application), not the domain | Enforce inside the domain factory | The domain cannot load other aggregates (Clean boundary). The handler loads Pedidos, checks `ClienteId`, throws `ConflictException`, then calls the factory. |
| D12 | LocalDB + one DB per test class, dropped on dispose; no Docker/Testcontainers | SQLite in-memory (REJECTED — diverges on rowversion/JSON/decimal); Testcontainers (deferred) | LocalDB is the same engine family as Azure SQL and ships with the SDK/VS; per-class isolation without cross-test bleed. |
| D13 | Map the **shipped CLR shape**, not the drifted spec text | Trust `Domain/spec.md` literally | Code is authoritative. Verified actual names (`RowVersion`, `Pedido.Tipo`, `Cliente.NumeroCliente:Guid`, no `Apellido`, `MovimientoStock.Tipo`, `VencimientoCAE:DateOnly?`). Mapping the spec text would not compile. |

---

## 9. The one domain change

`LineaPedido` has a `private bool _precioConfirmado` field that backs the set-once `ConfirmarPrecio` guard. If not persisted, a reloaded line would allow a second `ConfirmarPrecio` (broken invariant). Per the locked decision, expose it as an internal property so EF maps it as a plain column (no framework dependency):

```csharp
// in LineaPedido.cs — replace the bare field with a backed internal property
internal bool PrecioConfirmado { get; private set; }
// ConfirmarPrecio sets PrecioConfirmado = true; the guard reads it.
```

Mapped in `PedidoConfiguration` (§3.6) via `l.Property(x => x.PrecioConfirmado).HasColumnName("PrecioConfirmado")`. `internal` keeps it invisible outside the assembly while EF (same assembly via the domain's `InternalsVisibleTo` is NOT needed — EF maps internal members on the entity's own type by reflection). This is additive, ships in Slice B, and is independently revertible. It is the ONLY change to `GastroGestion.Domain`; `GastroGestion.Domain.csproj` still lists zero package/project references (REQ-01 holds).

---

## 10. Risks & mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Nested `Dinero`/owned-VO column-name collision (e.g. two `Dinero` on FacturaLinea/Pago) | High | Explicit `HasColumnName` per owned path (§3.3/3.6/3.9); a round-trip test per nested-VO aggregate. |
| Nullable `DireccionEntrega` owned type — non-nullable by default | Med | `b.Navigation(p => p.DireccionEntrega).IsRequired(false)`; round-trip a null-address Pedido and a Delivery Pedido. |
| `RowVersion` generation / domain default `[]` | Med | `IsRowVersion()` marks it store-generated; concurrency test asserts `DbUpdateConcurrencyException`. |
| `LineaRecetaSnapshot` JSON with nested `Cantidad` VO | Med | `OwnsMany(...).ToJson()` with nested `OwnsOne`; test that the snapshot survives a later Plato recipe edit (REQ-10-E). |
| `_precioConfirmado` not persisted → reloaded line allows second `ConfirmarPrecio` | Med | Expose `internal PrecioConfirmado` (§9); map it; reload → second `ConfirmarPrecio` → throws. |
| Append-only guard misses an entry state | Low | Guard rejects both `Modified` and `Deleted`; integration test attempts both → both throw. |
| EF maps the computed getters (Subtotal/Total/EstaPagada) | Med | Explicit `Ignore(...)` for every computed property on Pedido lines and Factura. |
| `internal PrecioConfirmado` not reflected by EF | Low | EF maps non-public members declared on the mapped type; verify with the round-trip test. If it fails, fall back to a shadow property `Property<bool>("PrecioConfirmado")` reading the field via `HasField`. |
| Domain accidentally gains an EF dependency | Low | Gate: review `GastroGestion.Domain.csproj` for zero refs before each slice merge (REQ-01). |
| Post-commit dispatch loses events on crash between commit and dispatch | Low | Accepted for Phase 3 (no consumer yet); outbox upgrade slots into the same seam in the AFIP phase. |
| LocalDB unavailable on a dev/CI box | Low | Config-driven connection string; document LocalDB prerequisite; Testcontainers is the documented future upgrade. |
| `PedidosFacturados` JSON not queryable | Low | Locked Assumption 2 — never filtered in SQL; switch to junction table only if a query need appears. |

---

## Checklist (reviewer can confirm)

- [ ] `GastroGestion.Domain.csproj` still lists zero package/project references after all slices.
- [ ] `dotnet ef migrations add` + `database update` build the greenfield schema on `(localdb)\mssqllocaldb`.
- [ ] Every aggregate round-trips (save→reload) with invariants intact, including `PrecioConfirmado` and the append-only guard.
- [ ] Nested `Dinero` paths have distinct column names; no migration column collision.
- [ ] `Pedido.DireccionEntrega` persists null for Salon/Mostrador and full for Delivery.
- [ ] `MovimientoStock` repo exposes only `AddAsync` + balance; SaveChanges guard rejects Modify/Delete.
- [ ] `Factura` is one flat table with an int `TipoComprobante` column (no EF inheritance); `PedidosFacturados` is a JSON column.
- [ ] In-process dispatch fires post-commit and clears events.
- [ ] `CrearFactura` rejects multi-client Pedido groups with `ConflictException` (REQ-13/13-G).
- [ ] Connection string is config-driven (no hardcoded server).
- [ ] Integration tests pass against LocalDB via `dotnet test`.

## Next step

Proceed to `sdd-tasks` (requires this design + the spec). Tasks will sequence the three slices: Slice A (packages → DbContext + factory → converters → catalogue configs → repositories + UoW → InitialCatalogue migration → LocalDB fixture + round-trip tests) → Slice B (Pedido config + PrecioConfirmado domain change → MovimientoStock + guard → dispatcher → migration → tests) → Slice C (Factura config → repository → domain services → CrearFactura + ConflictException → migration → tests).
