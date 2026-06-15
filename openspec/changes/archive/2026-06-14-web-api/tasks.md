# Tasks — web-api

**Generated:** 2026-06-14
**Artifact store:** openspec + engram
**Change:** web-api
**Phase:** 4 of 7 — Application use cases + Minimal API + Dev seeder
**Delivery strategy:** ask-on-risk → chained PRs confirmed (est. 1,800–2,500 lines total)
**Chain strategy:** stacked-to-main (3 PRs, each slice merges directly to main in order)

---

## CRITICAL — CLR Name Reconciliation

The design was written against the actual shipped CLR types. The apply phase MUST use the locked names below. Any drift from spec/proposal text to actual CLR name is resolved here.

| Proposal / spec text (stale or indicative) | Actual CLR name (LOCKED) | Source |
|---------------------------------------------|--------------------------|--------|
| `ResolverPrecioEfectivo` (sync) | `ResolverPrecioEfectivoAsync` (Task-returning) | Design §4a |
| `fechaMenu` (Menu factory param) | `FechaVigencia` (DateOnly param to `Menu.Crear`) | Design §2a |
| `CondicionIVA.Exento` | `CondicionIVA.ExentoIVA` | Design §7b |
| `Exento` in seeder | `ExentoIVA` | Design §7b |
| Indicative `EstadoDestino` DTO field | `EstadoNuevo` (`EstadoPedido`) in `TransicionarEstadoPedidoCommand` | Design §2b |
| Indicative `RolUsuario` DTO field | `Rol` (`RolUsuario` enum) in `TransicionarEstadoPedidoCommand` | Design §2b |
| `NotFoundException` (implicit) | `NotFoundException : Exception` in `Application/Common/Exceptions/` | Design §5b |
| `MovimientoStock.RegistrarCompra` | `MovimientoStock.RegistrarMovimiento` (general factory, Tipo enum drives sign) | Design §2c |
| `POST /pedidos/{id}/transiciones` (endpoint) | `POST /pedidos/{id}/transicion` (singular) | Design §3 |
| `GET /stock/{ingredienteId}/balance` | `GET /stock/balance/{ingredienteId}` | Design §3 |

---

## Dependency order

```
PR 1 — API Foundation
  WA-01 (Domain zero-dep gate + W-01 async interface change)
    └── WA-02 (EfectivoPrecioService async rewrite + call-site audit)
          └── WA-03 (NotFoundException added to Application)
                └── WA-04 (GastroGestionExceptionHandler : IExceptionHandler + AddProblemDetails)
                      └── WA-05 (ValidationFilter<T> + WithValidation<T> extension)
                            └── WA-06 (Package changes: +JwtBearer, +FluentValidation, -OpenApi)
                                  └── WA-07 (Program.cs rewrite + JWT pipeline + public partial Program)
                                        └── WA-08 (DevDataSeeder — full sample dataset)
                                              └── WA-09 (GastroGestion.Api.Tests project scaffold + ApiFactory)
                                                    └── WA-10 (PR 1 smoke tests: health + seeder boot + ProblemDetails shape)
                                                          └── WA-11 (Slice A/PR 1 build + test verification gate)

PR 2 — Catalogue Endpoints + Use Cases  (WA-11 must pass first)  ✅ COMPLETE
  WA-12 (GetAllAsync on all 5 catalogue ports + EF impls) ✅
    └── WA-13 (Contracts project ref → Application; FluentValidation in Contracts) ✅
          └── WA-14 (Cliente: DTOs + validator + mapping + handler + endpoint) ✅
                └──╮
  WA-15 (Ingrediente) ─ WA-16 (Plato) ─ WA-17 (Menu) ─ WA-18 (Mesa) — all can run parallel  ✅ all done
                ╰──────────────────────────────────────────────────────────────┘
                      └── WA-19 (Register all catalogue handlers in AddApplication + DI wiring) ✅
                            └── WA-20 (Catalogue endpoint tests: happy + error paths per group) ✅
                                  └── WA-21 (Slice B/PR 2 build + test verification gate) ✅

PR 3 — Transactional + Fiscal + Stock  (WA-21 must pass first)  ✅ COMPLETE
  WA-22 (Pedido DTOs + validators + mapping) ✅
    └── WA-23 (CrearPedidoHandler + AgregarLineaHandler + endpoints) ✅
          └── WA-24 (ConfirmarPrecioLineaHandler — live W-01 path) ✅
                └── WA-25 (TransicionarEstadoPedidoHandler + PHASE-5 seam markers) ✅
                      └── WA-26 (Factura DTOs + mapping + RegistrarPagoHandler + GetFacturaByIdHandler + endpoints) ✅
                            └── WA-27 (Stock DTOs + mapping + RegistrarMovimientoStockHandler + GetBalanceStockHandler + endpoints) ✅
                                  └── WA-28 (Register all transactional + fiscal + stock handlers in DI) ✅
                                        └── WA-29 (Transactional + fiscal + stock endpoint tests) ✅
                                              └── WA-30 (Slice C/PR 3 build + test verification gate) ✅
```

Within PR 2: WA-14 through WA-18 (one per catalogue aggregate) are independent of each other and can be developed in parallel once WA-12 and WA-13 land. All other tasks within a slice are sequential. PR 1 → PR 2 → PR 3 are strictly ordered.

---

## SLICE A — API Foundation

**PR #1 target:** `main`
**Covers:** REQ-01, REQ-02, REQ-03, REQ-04, REQ-05, REQ-06, REQ-07
**Design sections:** §1 (layout), §1c (Program.cs composition), §4 (W-01), §5 (error handler), §6 (validation), §7 (seeder), §9 (tests), §11 (packages)

---

### WA-01 — W-01 async interface + Domain zero-dep gate ✅

**Work unit:** Change `IEfectivoPrecioService` to the async signature; verify Domain.csproj remains zero-dependency.
**Conventional commit:** `feat(domain): make IEfectivoPrecioService async (W-01 — Task is BCL, zero new dep)`

#### What to do

1. **`src/GastroGestion.Domain/Services/IEfectivoPrecioService.cs`** — replace the synchronous method with:
   ```csharp
   public interface IEfectivoPrecioService
   {
       Task<(Dinero Precio, PorcentajeIVA IVA)> ResolverPrecioEfectivoAsync(
           Guid platoId, DateOnly fecha, CancellationToken ct = default);
   }
   ```
   Remove the old synchronous overload entirely. `Task` is BCL — no `using` addition triggers a package reference.

2. **Gate:** verify `GastroGestion.Domain.csproj` still contains zero `<PackageReference>` and zero `<ProjectReference>` elements.

3. **`rg "ResolverPrecioEfectivo"` search gate:** run across `src/` and `tests/`. After this task only `ResolverPrecioEfectivoAsync` may appear — no residual sync name.

#### Verification

```powershell
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
# Expected: exit 0

Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: no matches
```

#### Spec requirements satisfied

- **REQ-01** (Scenarios 01-A, 01-C) — async signature on domain interface; Domain zero-dep preserved.

---

### WA-02 — EfectivoPrecioService async rewrite + call-site audit ✅

**Work unit:** Rewrite `EfectivoPrecioService` to genuinely await both repo calls; audit all call sites.
**Conventional commit:** `feat(app): rewrite EfectivoPrecioService as genuinely async (W-01 impl)`

#### What to do

1. **`src/GastroGestion.Application/Services/EfectivoPrecioService.cs`** — implement the new async signature:
   ```csharp
   public async Task<(Dinero Precio, PorcentajeIVA IVA)> ResolverPrecioEfectivoAsync(
       Guid platoId, DateOnly fecha, CancellationToken ct = default)
   {
       var plato = await _platos.GetByIdAsync(platoId, ct)
           ?? throw new InvalidOperationException($"Plato {platoId} not found.");
       var iva = new PorcentajeIVA(plato.AlicuotaIVA);
       var menus = await _menus.GetActivosByFechaAsync(fecha, ct);
       var overridePrice = menus
           .SelectMany(m => m.Items)
           .Where(it => it.PlatoId == platoId && it.PrecioOverride is not null)
           .Select(it => it.PrecioOverride)
           .FirstOrDefault();
       var precio = overridePrice ?? plato.PrecioBase;
       return (precio, iva);
   }
   ```
   **No `.GetAwaiter().GetResult()` or `.Result` anywhere.** Remove the stale XML comment referencing the synchronous domain interface.

2. Update any **existing Application unit tests** that mock `IEfectivoPrecioService`: change sync mock setup to `ReturnsAsync(...)`.

3. **Phase-3 note:** `CrearFacturaHandler` (from Phase 3) calls `EfectivoPrecioService` via `IEfectivoPrecioService`. Confirm the existing handler is updated to `await _precios.ResolverPrecioEfectivoAsync(...)` — it was previously calling the sync version. This is part of WA-02 call-site coverage; `ConfirmarPrecioLineaHandler` (WA-24) is the new call site and is created against the async version directly.

#### Verification

```powershell
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
# Expected: exit 0

# No blocking calls remain (Scenario 01-B)
Select-String -Path "src/" -Pattern "\.GetAwaiter\(\)\.GetResult\(\)|\.Result" -Recurse
# Expected: no matches in EfectivoPrecioService.cs
```

#### Spec requirements satisfied

- **REQ-01** (Scenario 01-B) — no `.GetAwaiter().GetResult()` in implementation.

---

### WA-03 — NotFoundException in Application/Common/Exceptions ✅

**Work unit:** Add `NotFoundException` as a sibling to the existing `ConflictException`.
**Conventional commit:** `feat(app): add NotFoundException for write-path 404 signalling`

#### What to do

1. **`src/GastroGestion.Application/Common/Exceptions/NotFoundException.cs`**:
   ```csharp
   public sealed class NotFoundException : Exception
   {
       public NotFoundException(string message) : base(message) { }
   }
   ```

2. This type is consumed by write-path handlers (WA-23 through WA-26) when a target aggregate is missing (e.g., `AgregarLinea` when Pedido does not exist). GET-path handlers continue to return `T?` and map `null → TypedResults.NotFound()` at the endpoint.

#### Verification

```powershell
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-02** (Scenario 02-C) — not-found path produces 404 ProblemDetails via exception handler.
- Design §5b — `NotFoundException` standardises write-path 404 signalling.

---

### WA-04 — GastroGestionExceptionHandler + AddProblemDetails wiring ✅

**Work unit:** `IExceptionHandler` implementation + registration in `Program.cs` service block.
**Conventional commit:** `feat(api): add GastroGestionExceptionHandler with RFC 7807 exception mapping`

#### What to do

1. **`src/GastroGestion.Api/ErrorHandling/GastroGestionExceptionHandler.cs`** — implements `IExceptionHandler`:
   - Map `ConflictException` → 409, title "Business rule conflict", detail = `ex.Message`.
   - Map `NotFoundException` → 404, title "Resource not found", detail = `ex.Message`.
   - Map `DomainException` → 422, title "Domain rule violation", detail = `ex.Message`.
   - Any other exception → 500, title "An unexpected error occurred", detail = generic string (log full exception via Serilog; do NOT expose `ex.Message`).
   - Write using `IHttpContextAccessor` + `HttpContext.Response.WriteAsJsonAsync` with `ProblemDetails` shape.
   - `TryHandleAsync` returns `true` for all four branches.

2. **`Program.cs` service registration** (step 4 in design §1c order):
   - `builder.Services.AddProblemDetails();`
   - `builder.Services.AddExceptionHandler<GastroGestionExceptionHandler>();`

3. **`Program.cs` middleware** (step 1 in pipeline — MUST be first):
   - `app.UseExceptionHandler();`

4. No endpoint may contain a try/catch for `ConflictException`, `NotFoundException`, or `DomainException`.

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-02** (Scenarios 02-A, 02-B, 02-C, 02-D) — all four HTTP status mappings.
- Design §5a, §5c — IExceptionHandler, no middleware, no Result<T> rewrite.

---

### WA-05 — ValidationFilter<T> + WithValidation<T> extension ✅

**Work unit:** Generic endpoint filter + convenience extension method.
**Conventional commit:** `feat(api): add ValidationFilter<T> endpoint filter for FluentValidation 400 short-circuit`

#### What to do

1. **`src/GastroGestion.Api/Filters/ValidationFilter.cs`**:
   ```csharp
   public sealed class ValidationFilter<T> : IEndpointFilter where T : class
   {
       private readonly IValidator<T> _validator;
       public ValidationFilter(IValidator<T> validator) => _validator = validator;

       public async ValueTask<object?> InvokeAsync(
           EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
       {
           var arg = ctx.Arguments.OfType<T>().FirstOrDefault();
           if (arg is null) return await next(ctx);
           var result = await _validator.ValidateAsync(arg, ctx.HttpContext.RequestAborted);
           if (!result.IsValid)
               return TypedResults.ValidationProblem(result.ToDictionary());
           return await next(ctx);
       }
   }
   ```

2. Add a `RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder b)` extension on `RouteHandlerBuilder` (e.g. in `Filters/ValidationFilterExtensions.cs`) wrapping `.AddEndpointFilter<ValidationFilter<T>>()`.

3. **`Program.cs` service registration** (step 5 in §1c):
   - `builder.Services.AddValidatorsFromAssemblyContaining<CrearClienteRequest>();` (scans the Contracts assembly — placeholder reference until WA-13 adds the DTO; confirm the assembly reference compiles after WA-13).

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-03** (Scenarios 03-A, 03-B) — filter short-circuits before handler on invalid input; valid passes through.
- Design §6a, §6b — generic filter; `WithValidation<T>` extension.

---

### WA-06 — Package changes: +JwtBearer, +FluentValidation in Contracts, -OpenApi from Api ✅

**Work unit:** `.csproj` edits — three projects.
**Conventional commit:** `build(api,contracts): add JwtBearer and FluentValidation, remove redundant AspNetCore.OpenApi`

#### What to do

1. **`GastroGestion.Api.csproj`**:
   - ADD: `<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />` (pin to 8.0.x — must match SDK).
   - REMOVE: `<PackageReference Include="Microsoft.AspNetCore.OpenApi" ... />` (was 8.0.27 — confirm exact entry and delete it).
   - Keep `Swashbuckle.AspNetCore` 6.6.2 as-is.

2. **`GastroGestion.Contracts.csproj`**:
   - ADD: `<PackageReference Include="FluentValidation" Version="11.*" />`
   - ADD: `<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.*" />`
   - ProjectReference to Application is added in WA-13 (separate task).

3. Verify the solution builds with no implicit `Microsoft.AspNetCore.OpenApi` re-introduction.

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0

Select-String -Path "src/GastroGestion.Api/GastroGestion.Api.csproj" `
    -Pattern "AspNetCore.OpenApi"
# Expected: no matches (Scenario 06-C)

Select-String -Path "src/GastroGestion.Api/GastroGestion.Api.csproj" `
    -Pattern "JwtBearer"
# Expected: one match
```

#### Spec requirements satisfied

- **REQ-06** (Scenario 06-C) — `Microsoft.AspNetCore.OpenApi` removed; `Swashbuckle.AspNetCore` present.
- **REQ-04** — JWT pipeline package in place before WA-07 wires it.
- Design §11 — locked package delta.

---

### WA-07 — Program.cs rewrite: composition order + JWT pipeline + public partial Program ✅

**Work unit:** Full `Program.cs` composition in the locked order from design §1c; expose `Program` for test factory.
**Conventional commit:** `feat(api): rewrite Program.cs with locked composition order, JWT pipeline, and test exposure`

#### What to do

Rewrite `Program.cs` following the exact service/middleware order from design §1c:

**Service registration block (builder.Services):**
1. Serilog (keep existing).
2. `AddApplication()` + `AddInfrastructure(config)` (keep existing).
3. `AddHealthChecks()` (keep existing).
4. `AddProblemDetails()` + `AddExceptionHandler<GastroGestionExceptionHandler>()` (WA-04 wired here).
5. `AddValidatorsFromAssemblyContaining<CrearClienteRequest>()` (WA-05 wired here; placeholder until WA-13 lands).
6. `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => { ... })` configured from `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey` config keys. Keep the existing JWT signing-key startup guard verbatim.
7. `AddAuthorization()`.
8. `AddEndpointsApiExplorer()` + `AddSwaggerGen()` (keep existing; add JWT bearer security definition to `AddSwaggerGen` for later).

**Middleware pipeline (app):**
1. `app.UseExceptionHandler()` — MUST be first.
2. Dev-only block: `MigrateAsync()` → `await DevDataSeeder.SeedAsync(scope.ServiceProvider)` (after migrate, same scope — WA-08).
3. Dev-only: `UseSwagger()` + `UseSwaggerUI()`.
4. `UseSerilogRequestLogging()`.
5. `UseAuthentication()` → `UseAuthorization()`.
6. `MapHealthChecks("/health")`.
7. Endpoint group registrations (placeholder comments for WA-14 through WA-27; populated per slice).
8. `app.Run()`.

**Bottom of file (one line):**
```csharp
public partial class Program { }
```

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0

# JWT pipeline present (Scenario 04-B)
Select-String -Path "src/GastroGestion.Api/Program.cs" `
    -Pattern "UseAuthentication|UseAuthorization|JwtBearerDefaults"
# Expected: all three found
```

#### Spec requirements satisfied

- **REQ-04** (Scenarios 04-A, 04-B) — JWT pipeline wired; unauthenticated calls not blocked.
- **REQ-06** (Scenario 06-A, 06-B) — health + Swagger dev-only.
- **REQ-07** — `public partial class Program { }` enables `WebApplicationFactory<Program>`.
- Design §1c — locked composition order.

---

### WA-08 — DevDataSeeder with exact sample dataset ✅

**Work unit:** Full runtime seeder using domain factories; idempotent; dev-only; tomorrow-date Menu.
**Conventional commit:** `feat(infra): add DevDataSeeder with idempotent runtime seed via domain factories`

#### What to do

Create **`src/GastroGestion.Infrastructure/Persistence/Seed/DevDataSeeder.cs`**:

```
public static class DevDataSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        // 1. Resolve GastroGestionDbContext + repos + IUnitOfWork + IEfectivoPrecioService
        // 2. Idempotency guard:
        //    if (await db.Clientes.AnyAsync()) return;

        // 3. Build via domain factories (exact CLR names — LOCKED):
        //    Clientes (3):
        //      Cliente.Crear("Consumidor Demo", CondicionIVA.ConsumidorFinal, null, null)
        //      Cliente.Crear("RI Demo", CondicionIVA.ResponsableInscripto, new Cuit("30-71659554-9"), new Email("ri@demo.test"))
        //      Cliente.Crear("Exento Demo", CondicionIVA.ExentoIVA, null, null)   // ExentoIVA not Exento
        //
        //    Ingredientes (5): varied UnidadDeMedida (Gramo, Kilogramo, Litro, Unidad, Mililitro)
        //      Ingrediente.Crear("Harina", UnidadDeMedida.Kilogramo)
        //      Ingrediente.Crear("Agua", UnidadDeMedida.Litro)
        //      Ingrediente.Crear("Sal", UnidadDeMedida.Gramo)
        //      Ingrediente.Crear("Huevo", UnidadDeMedida.Unidad)
        //      Ingrediente.Crear("Aceite", UnidadDeMedida.Mililitro)
        //
        //    Platos (3):
        //      Plato.Crear("Milanesa", new Dinero(850m), AlicuotaIVA.General)
        //        .AgregarLineaReceta(harina.Id, new Cantidad(200m, UnidadDeMedida.Gramo))
        //        .AgregarLineaReceta(huevo.Id, new Cantidad(2m, UnidadDeMedida.Unidad))
        //      Plato.Crear("Ensalada", new Dinero(450m), AlicuotaIVA.Reducida)
        //      Plato.Crear("Tarta", new Dinero(650m), AlicuotaIVA.General)
        //        .AgregarLineaReceta(harina.Id, new Cantidad(300m, UnidadDeMedida.Gramo))
        //        .AgregarLineaReceta(sal.Id, new Cantidad(5m, UnidadDeMedida.Gramo))
        //
        //    Menu (1):
        //      var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        //      Menu.Crear("Menú del Día", tomorrow)          // param name: FechaVigencia
        //        .AgregarItem(milanesa.Id, null)
        //        .AgregarItem(tarta.Id, new Dinero(600m))    // one PrecioOverride
        //
        //    Mesas (4):
        //      Mesa.Crear(1, 2), Mesa.Crear(2, 4), Mesa.Crear(3, 4), Mesa.Crear(4, 6)
        //
        //    Pedido Salon (1):
        //      var pedidoSalon = Pedido.Crear(TipoPedido.Salon, mesa1.Id, cliente1.Id, null, DateTime.UtcNow)
        //      mesa1.AsignarPedido(pedidoSalon.Id)
        //      var linea = pedidoSalon.AgregarLinea(milanesa.Id, 2, null)
        //      var (precio, iva) = await precioService.ResolverPrecioEfectivoAsync(
        //          milanesa.Id, DateOnly.FromDateTime(DateTime.UtcNow), ct)
        //      linea.ConfirmarPrecio(precio, iva)
        //
        //    Pedido TakeAway (1):
        //      var pedidoTakeAway = Pedido.Crear(TipoPedido.TakeAway, null, cliente1.Id, null, DateTime.UtcNow)
        //      var linea2 = pedidoTakeAway.AgregarLinea(tarta.Id, 1, null)
        //      var (p2, iva2) = await precioService.ResolverPrecioEfectivoAsync(...)
        //      linea2.ConfirmarPrecio(p2, iva2)
        //
        //    Factura (1):
        //      Factura.CrearTicket(cliente1.Id, [pedidoTakeAway.Id], lineasFromTakeAway)
        //      (mirror CrearFacturaHandler.BuildLineasFromPedidos pattern)

        // 4. AddAsync each via its repository
        // 5. await uow.SaveChangesAsync(ct)
    }
}
```

Called from Program.cs dev block (WA-07) after MigrateAsync, same scope.

**CRITICAL correctness gates:**
- Use `CondicionIVA.ExentoIVA` (not `Exento`) — drift trap.
- Use `Menu.Crear(nombre, FechaVigencia)` — param is `FechaVigencia` (not `fechaMenu`).
- Menu `FechaVigencia` = `DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)` — computed at runtime, never hardcoded.
- `await` the async `EfectivoPrecioService` — never sync-over-async.
- CUIT `"30-71659554-9"` must pass `Cuit` check-digit validation — verify against the domain VO constructor.

#### Verification

```powershell
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0
# Runtime check via WA-10 integration test (Scenario 05-A)
```

#### Spec requirements satisfied

- **REQ-05** (Scenarios 05-A through 05-E) — seeded dataset; future-date Menu; idempotency; dev-only; invariants hold.
- Design §7a, §7b, §7d — static SeedAsync; exact sample data; gating.

---

### WA-09 — GastroGestion.Api.Tests project scaffold + ApiFactory ✅

**Work unit:** New xUnit project wired to `WebApplicationFactory<Program>` + LocalDB.
**Conventional commit:** `test(api): scaffold Api.Tests project with WebApplicationFactory and LocalDB ApiFactory`

#### What to do

1. **Create `tests/GastroGestion.Api.Tests/GastroGestion.Api.Tests.csproj`**:
   - Target `net8.0`.
   - `<ProjectReference>` to `GastroGestion.Api`.
   - NuGet: `Microsoft.AspNetCore.Mvc.Testing`, `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `FluentAssertions`, `coverlet.collector`.

2. **`tests/GastroGestion.Api.Tests/ApiFactory.cs`** — `ApiFactory : WebApplicationFactory<Program>`:
   - Override `ConfigureWebHost`: set `Environment = "Development"` (triggers seeder + auto-migrate).
   - Override `ConnectionStrings:GastroGestion` to a dedicated test LocalDB: `Server=(localdb)\mssqllocaldb;Database=GastroGestion_ApiTests;...`.
   - Override `Jwt:SigningKey` to an inline test key (same length as the startup guard requires; e.g. 32+ chars).
   - `IAsyncLifetime` on `ApiFactory`: `InitializeAsync` → `await Services.CreateScope() → db.Database.MigrateAsync()`; `DisposeAsync` → `db.Database.EnsureDeletedAsync()`.

3. Add the project to the solution file (`.sln`).

#### Verification

```powershell
dotnet build tests/GastroGestion.Api.Tests/GastroGestion.Api.Tests.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-07** (Scenario 07-A) — test project exists with `WebApplicationFactory<Program>` + LocalDB.
- Design §9a — `ApiFactory` override shape.

---

### WA-10 — PR 1 smoke tests: health + seeder boot + ProblemDetails shape ✅

**Work unit:** Minimum smoke test set for PR 1; covers health, ProblemDetails wiring, and seeder idempotency.
**Conventional commit:** `test(api): add PR1 smoke tests — health, ProblemDetails shape, seeder idempotency`

#### What to do

**`tests/GastroGestion.Api.Tests/SmokeTests.cs`** — all tests `[Trait("Category","Integration")]`:

- `GET_Health_Returns200` — `GET /health` → 200 (Scenario 06-A, 07-A).
- `UnrecognisedRoute_Returns404ProblemDetails` — `GET /nonexistent` → response has `Content-Type: application/problem+json` (confirms ProblemDetails is wired).
- `ApiFactory_SeedsDatabase_OnFirstBoot` — after factory starts, `GET /health` returns 200 AND the test scope's `DbContext.Clientes.CountAsync()` ≥ 3 (Scenario 05-A partial — seeder ran).
- `ApiFactory_SecondBoot_DoesNotDuplicateClientes` — restart factory (or re-use same factory), count Clientes: same count as first boot (Scenario 05-B partial).

Note: full seeder validation (all entity counts, future-date Menu) is covered in the PR 2 catalogue tests (WA-20) once endpoints exist.

#### Verification

```powershell
dotnet test tests/GastroGestion.Api.Tests/ `
    --filter "Category=Integration" `
    --logger "console;verbosity=normal"
# Expected: all 4 smoke tests pass; exit 0 (Scenario 07-A)
```

#### Spec requirements satisfied

- **REQ-06** (Scenario 06-A) — health endpoint.
- **REQ-07** (Scenario 07-A) — test suite exists and passes against LocalDB.
- **REQ-02** (Scenario 02-D partial) — 500 ProblemDetails shape wired (no stack trace leaked).
- **REQ-05** (Scenarios 05-A, 05-B partial) — seeder ran; no duplication.

---

### WA-11 — Slice A / PR 1 build + test verification gate ✅

**Work unit:** Verification-only — no code commits. Confirms PR 1 is shippable.

#### Verification commands

```powershell
# Domain zero-dep gate (REQ-01 Scenario 01-C)
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: exit 0 + no matches

# Full API host builds
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0

# No sync name remains (W-01 gate)
Select-String -Path "src/" -Pattern "ResolverPrecioEfectivo[^A]" -Recurse
# Expected: no matches (only ResolverPrecioEfectivoAsync exists)

# Microsoft.AspNetCore.OpenApi removed (Scenario 06-C)
Select-String -Path "src/GastroGestion.Api/GastroGestion.Api.csproj" -Pattern "AspNetCore.OpenApi"
# Expected: no matches

# Smoke tests pass
dotnet test tests/GastroGestion.Api.Tests/ `
    --filter "Category=Integration" `
    --logger "console;verbosity=normal"
# Expected: all pass; 0 failed; exit 0
```

---

## SLICE B — Catalogue Endpoints + Use Cases

**PR #2 target:** `main` (after PR 1 merged)
**Prerequisite:** WA-11 must pass.
**Covers:** REQ-08, REQ-09, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14, REQ-18, REQ-19
**Design sections:** §2a (catalogue handler signatures), §3 (DTO/endpoint surface), §6b (validator placement), §8 (GetAllAsync)

---

### WA-12 — GetAllAsync on all 5 catalogue ports + EF implementations ✅

**Work unit:** Port interface additions + EF Core repository implementations for the five catalogue aggregates.
**Conventional commit:** `feat(app+infra): add GetAllAsync to 5 catalogue repository ports and EF implementations`

#### What to do

For each of the five ports in `src/GastroGestion.Application/Abstractions/Persistence/`:

| Port | New method signature |
|------|---------------------|
| `IClienteRepository` | `Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken ct = default)` |
| `IIngredienteRepository` | same shape |
| `IPlatoRepository` | same shape |
| `IMenuRepository` | same shape |
| `IMesaRepository` | same shape |

For each EF implementation in `src/GastroGestion.Infrastructure/Persistence/Repositories/`:
- `ClienteRepository`: `(await _ctx.Clientes.ToListAsync(ct)).AsReadOnly()`
- `IngredienteRepository`: `_ctx.Ingredientes.ToListAsync(ct)` then `.AsReadOnly()`
- `PlatoRepository`: `_ctx.Platos.ToListAsync(ct)` — `LineasReceta` auto-loads via `OwnsMany` field access.
- `MenuRepository`: `_ctx.Menus.ToListAsync(ct)` — `Items` auto-loads via `OwnsMany`.
- `MesaRepository`: `_ctx.Mesas.ToListAsync(ct)` then `.AsReadOnly()`

No `.Include()` needed — owned-entity loading is automatic (confirmed by design §8, matching Phase-3 `GetByIdAsync` contract).

#### Verification

```powershell
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
# Expected: exit 0 both
```

#### Spec requirements satisfied

- **REQ-08** (Scenarios 08-A, 08-B) — `GetAllAsync` returns full graph; owned entities loaded.
- **REQ-14** — GET-all endpoints rely on these impls.

---

### WA-13 — Contracts project: add ProjectReference → Application + FluentValidation ✅

**Work unit:** `.csproj` change to make mapping extensions in Contracts compile-safe.
**Conventional commit:** `build(contracts): add ProjectReference to Application for DTO mapping extensions`

#### What to do

1. **`GastroGestion.Contracts.csproj`** — add:
   ```xml
   <ProjectReference Include="..\GastroGestion.Application\GastroGestion.Application.csproj" />
   ```
   (`FluentValidation` packages were added in WA-06.)

2. Verify no cycle: Contracts is referenced by Api only; nothing references Contracts from Domain/Application/Infrastructure.

#### Verification

```powershell
dotnet build src/GastroGestion.Contracts/GastroGestion.Contracts.csproj
# Expected: exit 0

# No cycle (Api is the only consumer of Contracts)
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- Design §1a — Contracts → Application reference decision; mapping extensions (`ToCommand()`, `ToResponse()`) compile.

---

### WA-14 — Cliente: DTOs + validator + mapping + handlers + endpoint group ✅

**Work unit:** Full Cliente slice — Contracts 4-file pattern + handlers + `RouteGroupBuilder`.
**Conventional commit:** `feat(api+app+contracts): add Cliente endpoints with create/get/get-all handlers and DTOs`

#### What to do

**`src/GastroGestion.Contracts/Clientes/`** (4 files):

1. **`ClienteRequests.cs`**: `CrearClienteRequest(string Nombre, CondicionIVA CondicionIVA, string? Cuit, string? Email)`.
2. **`ClienteResponses.cs`**: `ClienteResponse(Guid Id, string Nombre, CondicionIVA CondicionIVA, string? Cuit, string? Email, bool Activo)`.
3. **`ClienteValidators.cs`**: `ClienteValidator : AbstractValidator<CrearClienteRequest>` — require non-empty `Nombre`; require `Cuit` when `CondicionIVA == ResponsableInscripto`.
4. **`ClienteMappings.cs`**: `ToCommand(this CrearClienteRequest)` → `CrearClienteCommand`; `ToResponse(this Cliente)` → `ClienteResponse` (flatten `Cuit.Valor`, `Email.Valor`).

**Application handlers** (`src/GastroGestion.Application/Clientes/`):

5. **`CrearCliente/CrearClienteCommand.cs`**: `sealed record CrearClienteCommand(string Nombre, CondicionIVA CondicionIVA, string? Cuit, string? Email)`.
6. **`CrearCliente/CrearClienteHandler.cs`**: `Handle(CrearClienteCommand, CT)` → `Cliente.Crear(nombre, condicionIVA, cuit is null ? null : new Cuit(cuit), email is null ? null : new Email(email))` → `AddAsync` → `SaveChangesAsync` → returns `cliente.Id`.
7. **`GetClienteById/GetClienteByIdQuery.cs`** + **`GetClienteByIdHandler.cs`**: `Handle(query, CT)` → `IClienteRepository.GetByIdAsync` → returns `Cliente?`.
8. **`GetAllClientes/GetAllClientesQuery.cs`** + **`GetAllClientesHandler.cs`**: `Handle(query, CT)` → `IClienteRepository.GetAllAsync` → returns `IReadOnlyList<Cliente>`.

**API endpoint group** (`src/GastroGestion.Api/Endpoints/ClienteEndpoints.cs`):

9. `MapClienteEndpoints(this WebApplication app)` extension:
   - `POST /clientes` — `[AllowAnonymous]` + `.WithValidation<CrearClienteRequest>()` + injects `CrearClienteHandler`; returns `TypedResults.Created($"/clientes/{id}", id)`.
   - `GET /clientes/{id:guid}` — `[AllowAnonymous]` + injects `GetClienteByIdHandler`; maps `null → TypedResults.NotFound()`, otherwise `Ok(cliente.ToResponse())`.
   - `GET /clientes` — `[AllowAnonymous]` + injects `GetAllClientesHandler`; returns `Ok(list.Select(c => c.ToResponse()).ToList())`.

No `IMediator`; handlers injected directly into delegates. Domain aggregate `Cliente` never returned directly from endpoints.

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-09** (Scenarios 09-A, 09-B, 09-C, 09-D) — create/get/get-all; 201+Location; 422 for RI without CUIT; 404 for missing; 200 array.
- **REQ-18** — `ClienteResponse` is a flat DTO; no aggregate properties on wire.
- **REQ-19** — handler injected directly; no `IMediator`.

---

### WA-15 — Ingrediente: DTOs + validator + mapping + handlers + endpoint group ✅

**Work unit:** Full Ingrediente slice — mirrors WA-14 pattern.
**Conventional commit:** `feat(api+app+contracts): add Ingrediente endpoints with create/get/get-all handlers and DTOs`

#### What to do

**`src/GastroGestion.Contracts/Ingredientes/`** (4 files):
- `CrearIngredienteRequest(string Nombre, UnidadDeMedida UnidadBase)`.
- `IngredienteResponse(Guid Id, string Nombre, UnidadDeMedida UnidadBase, bool Activo)`.
- `IngredienteValidator : AbstractValidator<CrearIngredienteRequest>` — require non-blank `Nombre`.
- `IngredienteMappings.cs` — `ToCommand` → `CrearIngredienteCommand`; `ToResponse` → `IngredienteResponse`.

**Application handlers** (`src/GastroGestion.Application/Ingredientes/`):
- `CrearIngredienteCommand(string Nombre, UnidadDeMedida UnidadBase)` + `CrearIngredienteHandler` → `Ingrediente.Crear(nombre, unidadBase)` → `AddAsync` → save → `ingrediente.Id`.
- `GetIngredienteByIdQuery(Guid Id)` + `GetIngredienteByIdHandler` → `IIngredienteRepository.GetByIdAsync` → `Ingrediente?`.
- `GetAllIngredientesQuery()` + `GetAllIngredientesHandler` → `IIngredienteRepository.GetAllAsync`.

**API endpoint group** (`src/GastroGestion.Api/Endpoints/IngredienteEndpoints.cs`):
- `POST /ingredientes` `[AllowAnonymous]` + `.WithValidation<CrearIngredienteRequest>()` → `Created`.
- `GET /ingredientes/{id:guid}` `[AllowAnonymous]` → `Ok` / `NotFound`.
- `GET /ingredientes` `[AllowAnonymous]` → `Ok(list)`.

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-10** (Scenarios 10-A, 10-B) — blank-name 422; valid 201.
- **REQ-18**, **REQ-19** — DTO only; direct handler injection.

---

### WA-16 — Plato: DTOs + validator + mapping + handlers + endpoint group ✅

**Work unit:** Full Plato slice — includes `RecetaLineaRequest` sub-DTO and `PlatoResponse` with Receta lines.
**Conventional commit:** `feat(api+app+contracts): add Plato endpoints with create/get/get-all handlers and DTOs`

#### What to do

**`src/GastroGestion.Contracts/Platos/`** (4 files):
- `CrearPlatoRequest(string Nombre, decimal PrecioBase, AlicuotaIVA AlicuotaIVA, RecetaLineaRequest[] Lineas)` + `RecetaLineaRequest(Guid IngredienteId, decimal Cantidad, UnidadDeMedida Unidad)`.
- `PlatoResponse(Guid Id, string Nombre, decimal PrecioBase, string Moneda, AlicuotaIVA AlicuotaIVA, bool Activo, RecetaLineaResponse[] Receta)` + `RecetaLineaResponse(Guid Id, Guid IngredienteId, decimal Cantidad, UnidadDeMedida Unidad)`.
- `PlatoValidator` — require non-blank `Nombre`; require `PrecioBase > 0`.
- `PlatoMappings.cs` — `ToCommand` converts `Lineas` to `IReadOnlyList<RecetaLineaInput>`; `ToResponse` includes `Receta` lines.

**Application handlers** (`src/GastroGestion.Application/Platos/`):
- `CrearPlatoCommand(string Nombre, decimal PrecioBase, AlicuotaIVA AlicuotaIVA, IReadOnlyList<RecetaLineaInput> Lineas)` + `RecetaLineaInput(Guid IngredienteId, decimal Cantidad, UnidadDeMedida Unidad)`.
- `CrearPlatoHandler` → `Plato.Crear(nombre, new Dinero(precioBase), alicuotaIVA)`; `foreach line: plato.AgregarLineaReceta(line.IngredienteId, new Cantidad(line.Cantidad, line.Unidad))` → `AddAsync` → save → `plato.Id`.
- `GetPlatoByIdQuery` + `GetPlatoByIdHandler` → `IPlatoRepository.GetByIdAsync`.
- `GetAllPlatosQuery` + `GetAllPlatosHandler` → `IPlatoRepository.GetAllAsync`.

**API endpoint group** (`src/GastroGestion.Api/Endpoints/PlatoEndpoints.cs`):
- `POST /platos` `[AllowAnonymous]` + `.WithValidation<CrearPlatoRequest>()` → `Created`.
- `GET /platos/{id:guid}` `[AllowAnonymous]` → `Ok` / `NotFound`.
- `GET /platos` `[AllowAnonymous]` → `Ok`.

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-11** (Scenarios 11-A, 11-B) — valid 201 with Receta lines; negative PrecioBase 422.
- **REQ-18**, **REQ-19**.

---

### WA-17 — Menu: DTOs + validator + mapping + handlers + endpoint group ✅

**Work unit:** Full Menu slice — `FechaVigencia` DateOnly; past-date domain guard surfaces as 422.
**Conventional commit:** `feat(api+app+contracts): add Menu endpoints with create/get/get-all handlers and DTOs`

#### What to do

**`src/GastroGestion.Contracts/Menus/`** (4 files):
- `CrearMenuRequest(string Nombre, DateOnly FechaVigencia, MenuItemRequest[] Items)` + `MenuItemRequest(Guid PlatoId, decimal? PrecioOverride)`.
- `MenuResponse(Guid Id, string Nombre, DateOnly FechaVigencia, bool Activo, MenuItemResponse[] Items)` + `MenuItemResponse(Guid Id, Guid PlatoId, decimal? PrecioOverride)`.
- `MenuValidator` — require non-blank `Nombre`; require `FechaVigencia > DateOnly.FromDateTime(DateTime.UtcNow)` (validator gives friendly 400; domain guard still fires 422 if bypassed).
- `MenuMappings.cs`.

**Application handlers** (`src/GastroGestion.Application/Menus/`):
- `CrearMenuCommand(string Nombre, DateOnly FechaVigencia, IReadOnlyList<MenuItemInput> Items)` + `MenuItemInput(Guid PlatoId, decimal? PrecioOverride)`.
- `CrearMenuHandler` → `Menu.Crear(nombre, fechaVigencia)`; `foreach: menu.AgregarItem(item.PlatoId, item.PrecioOverride is null ? null : new Dinero(item.PrecioOverride.Value))` → `AddAsync` → save. **CRITICAL: factory param is `FechaVigencia`, NOT `fechaMenu`.**
- `GetMenuByIdQuery` + `GetMenuByIdHandler`.
- `GetAllMenusQuery` + `GetAllMenusHandler`.

**API endpoint group** (`src/GastroGestion.Api/Endpoints/MenuEndpoints.cs`):
- `POST /menus` `[AllowAnonymous]` + `.WithValidation<CrearMenuRequest>()` → `Created`.
- `GET /menus/{id:guid}` `[AllowAnonymous]` → `Ok` / `NotFound`.
- `GET /menus` `[AllowAnonymous]` → `Ok`.

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-12** (Scenarios 12-A, 12-B) — past-date 422; future-date 201 with Items.
- **REQ-14** (Scenario 14-A) — GET /menus returns seeded menu with future date.
- **REQ-18**, **REQ-19**.

---

### WA-18 — Mesa: DTOs + validator + mapping + handlers + endpoint group ✅

**Work unit:** Full Mesa slice — zero/negative Capacidad domain guard.
**Conventional commit:** `feat(api+app+contracts): add Mesa endpoints with create/get/get-all handlers and DTOs`

#### What to do

**`src/GastroGestion.Contracts/Mesas/`** (4 files):
- `CrearMesaRequest(int Numero, int Capacidad)`.
- `MesaResponse(Guid Id, int Numero, int Capacidad, EstadoMesa Estado, bool Activa, Guid? PedidoActivoId)`.
- `MesaValidator` — require `Capacidad > 0`; require `Numero > 0`.
- `MesaMappings.cs`.

**Application handlers** (`src/GastroGestion.Application/Mesas/`):
- `CrearMesaCommand(int Numero, int Capacidad)` + `CrearMesaHandler` → `Mesa.Crear(numero, capacidad)` → `AddAsync` → save.
- `GetMesaByIdQuery` + `GetMesaByIdHandler`.
- `GetAllMesasQuery` + `GetAllMesasHandler`.

**API endpoint group** (`src/GastroGestion.Api/Endpoints/MesaEndpoints.cs`):
- `POST /mesas` `[AllowAnonymous]` + `.WithValidation<CrearMesaRequest>()` → `Created`.
- `GET /mesas/{id:guid}` `[AllowAnonymous]` → `Ok` / `NotFound`.
- `GET /mesas` `[AllowAnonymous]` → `Ok`.

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-13** (Scenarios 13-A, 13-B) — zero Capacidad 422; valid 201.
- **REQ-18**, **REQ-19**.

---

### WA-19 — Register all catalogue handlers in AddApplication + endpoint groups in Program.cs ✅

**Work unit:** DI registration for all 9 new catalogue handlers + wire endpoint groups.
**Conventional commit:** `feat(app+api): register catalogue handlers and wire endpoint groups in Program.cs`

#### What to do

1. **`src/GastroGestion.Application/DependencyInjection.cs`** — add `AddScoped` for each handler created in WA-14 through WA-18:
   - `CrearClienteHandler`, `GetClienteByIdHandler`, `GetAllClientesHandler`
   - `CrearIngredienteHandler`, `GetIngredienteByIdHandler`, `GetAllIngredientesHandler`
   - `CrearPlatoHandler`, `GetPlatoByIdHandler`, `GetAllPlatosHandler`
   - `CrearMenuHandler`, `GetMenuByIdHandler`, `GetAllMenusHandler`
   - `CrearMesaHandler`, `GetMesaByIdHandler`, `GetAllMesasHandler`

2. **`Program.cs`** — replace endpoint-group placeholder comments with actual calls:
   ```csharp
   app.MapClienteEndpoints();
   app.MapIngredienteEndpoints();
   app.MapPlatoEndpoints();
   app.MapMenuEndpoints();
   app.MapMesaEndpoints();
   ```

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-19** — direct DI injection, no mediator.
- Design §1b — `DependencyInjection.cs` registers all handlers.

---

### WA-20 — Catalogue endpoint integration tests ✅

**Work unit:** `CatalogueEndpointTests.cs` — at least one happy-path + one error-path per group.
**Conventional commit:** `test(api): add catalogue endpoint integration tests — happy and error paths`

#### What to do

**`tests/GastroGestion.Api.Tests/CatalogueEndpointTests.cs`** — all `[Trait("Category","Integration")]`:

- `POST_Clientes_ValidRequest_Returns201WithLocation` (Scenario 09-A).
- `POST_Clientes_RIWithoutCuit_Returns422` (Scenario 09-B).
- `GET_Clientes_ById_NotFound_Returns404` (Scenario 09-C).
- `GET_Clientes_Returns_SeededClientes` (Scenario 09-D / REQ-14 partial).
- `POST_Ingredientes_BlankName_Returns422` (Scenario 10-A).
- `POST_Ingredientes_ValidRequest_Returns201` (Scenario 10-B).
- `POST_Platos_NegativePrecioBase_Returns422` (Scenario 11-B).
- `POST_Platos_ValidRequest_Returns201WithRecetaLines` (Scenario 11-A).
- `POST_Menus_PastFechaVigencia_Returns422` (Scenario 12-A).
- `POST_Menus_FutureDate_Returns201` (Scenario 12-B).
- `GET_Menus_Returns_SeededMenu_WithFutureDate` (Scenario 14-A / REQ-14).
- `POST_Mesas_ZeroCapacidad_Returns422` (Scenario 13-A).
- `POST_Mesas_ValidRequest_Returns201` (Scenario 13-B).
- `POST_Clientes_EmptyNombre_Returns400ValidationProblem` (Scenario 03-A pattern — validator short-circuits).

#### Verification

```powershell
dotnet test tests/GastroGestion.Api.Tests/ `
    --filter "Category=Integration" `
    --logger "console;verbosity=normal"
# Expected: all catalogue tests pass (including PR 1 smoke tests); exit 0
```

#### Spec requirements satisfied

- **REQ-09** through **REQ-14**, **REQ-18**, **REQ-20** (Slice 2 coverage).
- Design §9c — PR 2 endpoint tests.

---

### WA-21 — Slice B / PR 2 build + test verification gate ✅

**Work unit:** Verification-only — no code commits. Confirms PR 2 is shippable.

#### Verification commands

```powershell
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
dotnet build src/GastroGestion.Contracts/GastroGestion.Contracts.csproj
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0 all

dotnet test tests/GastroGestion.Api.Tests/ `
    --filter "Category=Integration" `
    --logger "console;verbosity=normal"
# Expected: all PR 1 + PR 2 tests pass; 0 failed; exit 0

# No mediator references introduced
Select-String -Path "src/" -Pattern "IMediator|ISender" -Recurse
# Expected: no matches (REQ-19)

# Domain still zero-dep
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: no matches
```

---

## SLICE C — Transactional + Fiscal + Stock Endpoints

**PR #3 target:** `main` (after PR 2 merged)
**Prerequisite:** WA-21 must pass.
**Covers:** REQ-15, REQ-16, REQ-17, REQ-18, REQ-19, REQ-20
**Design sections:** §2b (Pedido handlers), §2c (Fiscal/Stock handlers), §3 (endpoint surface), §10 (PHASE-5 seam)

---

### WA-22 — Pedido DTOs + validators + mapping (Contracts)

**Work unit:** All Pedido-related Contracts types — request DTOs, response DTOs, validators, mappings.
**Conventional commit:** `feat(contracts): add Pedido DTOs, validators, and mapping extensions`

#### What to do

**`src/GastroGestion.Contracts/Pedidos/`** (4 files):

- **Requests**: `CrearPedidoRequest(TipoPedido Tipo, Guid? MesaId, Guid? ClienteId, DireccionEntregaRequest? DireccionEntrega)` + `DireccionEntregaRequest(string Calle, string Numero, string Ciudad, string Provincia, string CodigoPostal, string? Piso, string? Departamento)` + `AgregarLineaRequest(Guid PlatoId, int Cantidad, string? Observaciones)` + `TransicionarEstadoRequest(EstadoPedido EstadoNuevo, RolUsuario Rol)`.
- **Responses**: `PedidoResponse(Guid Id, TipoPedido Tipo, EstadoPedido Estado, Guid? MesaId, Guid? ClienteId, DireccionEntregaResponse? DireccionEntrega, DateTime CreadoEnUtc, IReadOnlyList<LineaPedidoResponse> Lineas)` + `LineaPedidoResponse(Guid Id, Guid PlatoId, int Cantidad, string? Observaciones, decimal? PrecioUnitario, string? Moneda, decimal? IvaTasa, decimal? SubtotalLinea, decimal? TotalLinea)`.
- **Validators**: `CrearPedidoValidator` — require `MesaId` when `Tipo == Salon`; `AgregarLineaValidator` — require `Cantidad > 0`.
- **Mappings**: `ToCommand` for each request type; `ToResponse(this Pedido)` flattening `Dinero`/`PorcentajeIVA` to primitives; `ToResponse(this LineaPedido)` using nullable flatten.

**NOTE on `TransicionarEstadoRequest`:** the `Rol` field maps to `RolUsuario` enum — this is the `// PHASE-5` seam (role from body). The field name in the command is `Rol` (type `RolUsuario`), not a string.

#### Verification

```powershell
dotnet build src/GastroGestion.Contracts/GastroGestion.Contracts.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-15** — Pedido request/response surface; role from body (seam documented).
- **REQ-18** — `PedidoResponse` is a flat DTO; no aggregate on wire.

---

### WA-23 — CrearPedidoHandler + AgregarLineaHandler + Pedido endpoint group (partial)

**Work unit:** Two handlers + the `/pedidos` endpoint group with `POST /pedidos` and `POST /{id}/lineas`.
**Conventional commit:** `feat(app+api): add CrearPedido and AgregarLinea handlers with Pedido endpoint group`

#### What to do

**Application handlers** (`src/GastroGestion.Application/Pedidos/`):

1. **`CrearPedido/CrearPedidoCommand.cs`**: `sealed record CrearPedidoCommand(TipoPedido Tipo, Guid? MesaId, Guid? ClienteId, DireccionEntregaInput? DireccionEntrega, DateTime CreadoEnUtc)` + `DireccionEntregaInput(...)`.
2. **`CrearPedido/CrearPedidoHandler.cs`**: inject `IPedidoRepository`, `IMesaRepository`, `IUnitOfWork`. Call `Pedido.Crear(Tipo, MesaId, ClienteId, dir, cmd.CreadoEnUtc)`. If `Tipo == Salon`: `var mesa = await _mesas.GetByIdAsync(MesaId, ct) ?? throw new NotFoundException(...)`, then `mesa.AsignarPedido(pedido.Id)`. `AddAsync(pedido)` → `SaveChangesAsync` → return `pedido.Id`.
3. **`AgregarLinea/AgregarLineaCommand.cs`**: `sealed record AgregarLineaCommand(Guid PedidoId, Guid PlatoId, int Cantidad, string? Observaciones)`.
4. **`AgregarLinea/AgregarLineaHandler.cs`**: load pedido (throw `NotFoundException` if missing); `var linea = pedido.AgregarLinea(PlatoId, Cantidad, Observaciones)` → `SaveChangesAsync` → return `linea.Id`.

**API endpoint group** (`src/GastroGestion.Api/Endpoints/PedidoEndpoints.cs`) — partial, extended in WA-24 and WA-25:

5. `MapPedidoEndpoints(this WebApplication app)`:
   - `POST /pedidos` `[AllowAnonymous]` + `.WithValidation<CrearPedidoRequest>()` → inject `CrearPedidoHandler`; pass `DateTime.UtcNow` as `CreadoEnUtc`; returns `Created($"/pedidos/{id}", id)`.
   - `POST /pedidos/{id:guid}/lineas` `[AllowAnonymous]` + `.WithValidation<AgregarLineaRequest>()` → inject `AgregarLineaHandler`; returns `Created($"/pedidos/{id}/lineas/{lineaId}", lineaId)`.

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-15** (Scenarios 15-B, 15-A partial) — Mostrador 201; Salon null MesaId → 422 from domain.

---

### WA-24 — ConfirmarPrecioLineaHandler — live W-01 path

**Work unit:** The handler that exercises `ResolverPrecioEfectivoAsync` on the real HTTP stack.
**Conventional commit:** `feat(app+api): add ConfirmarPrecioLineaHandler — live W-01 async price resolution path`

#### What to do

**`src/GastroGestion.Application/Pedidos/ConfirmarPrecioLinea/`**:

1. **`ConfirmarPrecioLineaCommand.cs`**: `sealed record ConfirmarPrecioLineaCommand(Guid PedidoId, Guid LineaId)`.
2. **`ConfirmarPrecioLineaHandler.cs`**: inject `IPedidoRepository`, `IEfectivoPrecioService`, `IUnitOfWork`.
   ```csharp
   var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
       ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");
   var linea = pedido.Lineas.FirstOrDefault(l => l.Id == cmd.LineaId)
       ?? throw new NotFoundException($"LineaPedido {cmd.LineaId} not found.");
   var (precio, iva) = await _precios.ResolverPrecioEfectivoAsync(
       linea.PlatoId, DateOnly.FromDateTime(pedido.CreadoEnUtc), ct);
   linea.ConfirmarPrecio(precio, iva);
   await _uow.SaveChangesAsync(ct);
   ```

3. **`PedidoEndpoints.cs`** — add to existing group:
   - `POST /pedidos/{id:guid}/lineas/{lineaId:guid}/confirmar-precio` `[AllowAnonymous]` → inject `ConfirmarPrecioLineaHandler`; returns `TypedResults.NoContent()` on success (or `TypedResults.NotFound()` if NotFoundException propagates to handler — but NotFoundException is caught by the exception handler).

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
# Live path verified by WA-29 integration test (Scenario 15-C)
```

#### Spec requirements satisfied

- **REQ-15** (Scenarios 15-C, 15-D) — async price resolution; second confirmation 422.
- **REQ-01** — W-01 live in the HTTP stack.

---

### WA-25 — TransicionarEstadoPedidoHandler + GetPedidoByIdHandler + PHASE-5 seam markers

**Work unit:** Two handlers; PHASE-5 comment seams inserted at the exact role-from-body lines.
**Conventional commit:** `feat(app+api): add TransicionarEstadoPedido and GetPedidoById handlers with PHASE-5 seam markers`

#### What to do

**`src/GastroGestion.Application/Pedidos/TransicionarEstadoPedido/`**:

1. **`TransicionarEstadoPedidoCommand.cs`**: `sealed record TransicionarEstadoPedidoCommand(Guid PedidoId, EstadoPedido EstadoNuevo, RolUsuario Rol)`.
2. **`TransicionarEstadoPedidoHandler.cs`**: load pedido (throw `NotFoundException` if missing); `pedido.TransicionarEstado(cmd.EstadoNuevo, cmd.Rol)` → `SaveChangesAsync`.

**`src/GastroGestion.Application/Pedidos/GetPedidoById/`**:

3. **`GetPedidoByIdQuery.cs`** + **`GetPedidoByIdHandler.cs`** → `IPedidoRepository.GetByIdAsync` → `Pedido?`.

**`PedidoEndpoints.cs`** — add to existing group:

4. `POST /pedidos/{id:guid}/transicion` `[AllowAnonymous]` — inject `TransicionarEstadoPedidoHandler`:
   ```csharp
   // PHASE-5: replace body-supplied Rol with JWT claim (User.FindFirst(ClaimTypes.Role))
   var rol = request.Rol;
   ```
   Returns `TypedResults.Ok(pedido.ToResponse())` after the transition.

5. `GET /pedidos/{id:guid}` `[AllowAnonymous]` — inject `GetPedidoByIdHandler`; `null → NotFound()`, otherwise `Ok(pedido.ToResponse())`.

**`TransicionarEstadoPedidoHandler.cs`** — also add a seam comment at the exact line where `cmd.Rol` is used:
```csharp
// PHASE-5: replace body-supplied Rol with JWT claim
```

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0

# Seam markers greppable
Select-String -Path "src/" -Pattern "PHASE-5" -Recurse
# Expected: at least 2 matches (endpoint delegate + handler)
```

#### Spec requirements satisfied

- **REQ-15** (Scenarios 15-E, 15-F, 15-G) — wrong role 422; valid role 200; GET 404.
- Design §10 — `// PHASE-5` seam markers present and greppable.

---

### WA-26 — Factura: DTOs + mapping + RegistrarPagoHandler + GetFacturaByIdHandler + endpoint group

**Work unit:** Fiscal slice — wire existing `CrearFacturaHandler`; add two new handlers; full `/facturas` group.
**Conventional commit:** `feat(app+api+contracts): add Factura endpoints — CrearFactura wire-up, RegistrarPago, GetFacturaById`

#### What to do

**`src/GastroGestion.Contracts/Facturacion/`** (4 files):
- `CrearFacturaRequest(Guid ClienteId, Guid[] PedidoIds, TipoComprobanteSolicitado Tipo)`.
- `RegistrarPagoRequest(decimal Monto, MetodoPago MetodoPago)`.
- `FacturaResponse(Guid Id, TipoComprobante TipoComprobante, EstadoFactura Estado, Guid ClienteId, DateTime FechaAlta, decimal SubTotal, decimal TotalIVA, decimal Total, decimal TotalPagado, bool EstaPagada, string? CAE, DateOnly? VencimientoCAE, IReadOnlyList<FacturaLineaResponse> Lineas, IReadOnlyList<PagoResponse> Pagos)` + `FacturaLineaResponse(Guid Id, Guid LineaPedidoId, int Cantidad, decimal PrecioUnitario, string Moneda, decimal IvaTasa)` + `PagoResponse(Guid Id, decimal Monto, MetodoPago MetodoPago, DateTime FechaPago)`.
- Validators: `CrearFacturaValidator` — require non-empty `PedidoIds`; `RegistrarPagoValidator` — require `Monto > 0`.
- Mappings: `ToResponse(this Factura)` — flatten all VOs; computed totals read from aggregate properties.

**Application handlers** (`src/GastroGestion.Application/Facturacion/`):
- `CrearFacturaHandler` already exists (Phase 3) — no changes needed to the handler itself.
- **`RegistrarPago/RegistrarPagoCommand.cs`**: `sealed record RegistrarPagoCommand(Guid FacturaId, decimal Monto, MetodoPago MetodoPago)`.
- **`RegistrarPago/RegistrarPagoHandler.cs`**: load `IFacturaRepository.GetByIdAsync` (throw `NotFoundException` if missing); `factura.RegistrarPago(new Dinero(cmd.Monto), cmd.MetodoPago, DateTime.UtcNow)` → `SaveChangesAsync`.
- **`GetFacturaById/GetFacturaByIdQuery.cs`** + **`GetFacturaByIdHandler.cs`** → `IFacturaRepository.GetByIdAsync` → `Factura?`.

**API endpoint group** (`src/GastroGestion.Api/Endpoints/FacturaEndpoints.cs`):
- `POST /facturas` `[AllowAnonymous]` + `.WithValidation<CrearFacturaRequest>()` → inject `CrearFacturaHandler`; returns `Created($"/facturas/{id}", id)`.
- `POST /facturas/{id:guid}/pagos` `[AllowAnonymous]` + `.WithValidation<RegistrarPagoRequest>()` → inject `RegistrarPagoHandler`; returns `NoContent`.
- `GET /facturas/{id:guid}` `[AllowAnonymous]` → inject `GetFacturaByIdHandler`; `null → NotFound`, else `Ok(factura.ToResponse())`.

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-16** (Scenarios 16-A through 16-F) — all Factura paths.
- **REQ-18**, **REQ-19**.

---

### WA-27 — Stock: DTOs + mapping + RegistrarMovimientoStockHandler + GetBalanceStockHandler + endpoint group

**Work unit:** Stock slice — append-only; balance returns 0 (not 404) for unknown ingrediente.
**Conventional commit:** `feat(app+api+contracts): add Stock endpoints — RegistrarMovimiento and GetBalance`

#### What to do

**`src/GastroGestion.Contracts/Stock/`** (4 files):
- `RegistrarMovimientoStockRequest(Guid IngredienteId, TipoMovimientoStock Tipo, decimal Cantidad, Guid? OrdenTrabajoId, Guid? LineaPedidoId)`.
- `MovimientoStockResponse(Guid Id, Guid IngredienteId, TipoMovimientoStock Tipo, decimal Cantidad, DateTime FechaMovimiento)`.
- `BalanceStockResponse(Guid IngredienteId, decimal Balance)`.
- `RegistrarMovimientoStockValidator` — require `Cantidad > 0` (caller passes absolute value; factory applies sign).
- Mappings: `ToResponse(this MovimientoStock)`.

**Application handlers** (`src/GastroGestion.Application/Stock/`):
- **`RegistrarMovimientoStock/RegistrarMovimientoStockCommand.cs`**: `sealed record RegistrarMovimientoStockCommand(Guid IngredienteId, TipoMovimientoStock Tipo, decimal Cantidad, Guid? OrdenTrabajoId, Guid? LineaPedidoId)`.
- **`RegistrarMovimientoStock/RegistrarMovimientoStockHandler.cs`**: `MovimientoStock.RegistrarMovimiento(IngredienteId, Tipo, Cantidad, OrdenTrabajoId, LineaPedidoId)` → `IMovimientoStockRepository.AddAsync` → `SaveChangesAsync` → return `mov.Id`. **NOTE: use `RegistrarMovimiento` (general factory), not `RegistrarCompra`.**
- **`GetBalanceStock/GetBalanceStockQuery.cs`** + **`GetBalanceStockHandler.cs`**: `IMovimientoStockRepository.CalcularBalanceAsync(query.IngredienteId, ct)` → returns `decimal`. Zero movements returns 0 (not 404) — `CalcularBalanceAsync` naturally returns 0 from SUM.

**API endpoint group** (`src/GastroGestion.Api/Endpoints/StockEndpoints.cs`):
- `POST /stock/movimientos` `[AllowAnonymous]` + `.WithValidation<RegistrarMovimientoStockRequest>()` → inject `RegistrarMovimientoStockHandler`; returns `Created($"/stock/movimientos/{id}", id)`.
- `GET /stock/balance/{ingredienteId:guid}` `[AllowAnonymous]` → inject `GetBalanceStockHandler`; returns `Ok(new BalanceStockResponse(ingredienteId, balance))`. **Never returns 404 — zero-balance is valid (Scenario 17-D).**

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-17** (Scenarios 17-A, 17-B, 17-C, 17-D) — Compra 201; Consumo negative Cantidad; balance sum; zero-balance 200.
- **REQ-18**, **REQ-19**.

---

### WA-28 — Register all transactional + fiscal + stock handlers in AddApplication + endpoint groups in Program.cs

**Work unit:** DI registration for all PR 3 handlers + wire endpoint groups.
**Conventional commit:** `feat(app+api): register transactional/fiscal/stock handlers and wire endpoint groups`

#### What to do

1. **`src/GastroGestion.Application/DependencyInjection.cs`** — add `AddScoped` for:
   - `CrearPedidoHandler`, `AgregarLineaHandler`, `ConfirmarPrecioLineaHandler`, `TransicionarEstadoPedidoHandler`, `GetPedidoByIdHandler`
   - `RegistrarPagoHandler`, `GetFacturaByIdHandler` (`CrearFacturaHandler` was registered in Phase 3)
   - `RegistrarMovimientoStockHandler`, `GetBalanceStockHandler`

2. **`Program.cs`** — add after existing catalogue endpoint registrations:
   ```csharp
   app.MapPedidoEndpoints();
   app.MapFacturaEndpoints();
   app.MapStockEndpoints();
   ```

#### Verification

```powershell
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0
```

#### Spec requirements satisfied

- **REQ-19** — all handlers registered as scoped, injected directly.

---

### WA-29 — Transactional + fiscal + stock endpoint integration tests

**Work unit:** `TransactionalEndpointTests.cs` — covers Pedido lifecycle, Factura, Stock.
**Conventional commit:** `test(api): add transactional, fiscal, and stock endpoint integration tests`

#### What to do

**`tests/GastroGestion.Api.Tests/TransactionalEndpointTests.cs`** — all `[Trait("Category","Integration")]`:

**Pedido lifecycle:**
- `POST_Pedidos_Salon_WithoutMesaId_Returns422` (Scenario 15-A).
- `POST_Pedidos_Mostrador_Returns201` (Scenario 15-B).
- `POST_Pedidos_AddLine_ThenConfirmPrice_Returns200_ExercisesW01` (Scenario 15-C — the W-01 deadlock regression test; full HTTP round-trip confirms no blocking call).
- `POST_Pedidos_ConfirmPriceTwice_Returns422` (Scenario 15-D).
- `POST_Pedidos_Transicion_WrongRole_Returns422` (Scenario 15-E).
- `POST_Pedidos_Transicion_ValidRole_Returns200WithNewEstado` (Scenario 15-F).
- `GET_Pedidos_NotFound_Returns404` (Scenario 15-G).

**Factura:**
- `POST_Facturas_MixedClientPedidos_Returns409` (Scenario 16-A).
- `POST_Facturas_PedidoWithNoConfirmedLines_Returns409` (Scenario 16-B).
- `POST_Facturas_ValidRequest_Returns201WithLocation` (Scenario 16-C).
- `POST_Facturas_RegistrarPago_FullAmount_Returns200_Pagada` (Scenario 16-D).
- `POST_Facturas_RegistrarPago_OnCancelada_Returns422` (Scenario 16-E).
- `GET_Facturas_NotFound_Returns404` (Scenario 16-F).

**Stock:**
- `POST_Stock_Compra_Returns201` (Scenario 17-A).
- `POST_Stock_Consumo_Returns201_WithNegativeCantidad` (Scenario 17-B).
- `GET_Stock_Balance_ReturnsCorrectNet` (Scenario 17-C).
- `GET_Stock_Balance_NoMovements_Returns0` (Scenario 17-D).

#### Verification

```powershell
dotnet test tests/GastroGestion.Api.Tests/ `
    --filter "Category=Integration" `
    --logger "console;verbosity=normal"
# Expected: ALL PR 1 + PR 2 + PR 3 tests pass; 0 failed; exit 0 (REQ-20 Scenario 20-A)
```

#### Spec requirements satisfied

- **REQ-15** through **REQ-17**, **REQ-20** (full integration suite across all 3 slices).
- Design §9c — PR 3 integration tests including W-01 deadlock regression.

---

### WA-30 — Slice C / PR 3 build + test verification gate

**Work unit:** Verification-only — no code commits. Confirms PR 3 is shippable.

#### Verification commands

```powershell
# All projects build
dotnet build src/GastroGestion.Domain/GastroGestion.Domain.csproj
dotnet build src/GastroGestion.Application/GastroGestion.Application.csproj
dotnet build src/GastroGestion.Infrastructure/GastroGestion.Infrastructure.csproj
dotnet build src/GastroGestion.Contracts/GastroGestion.Contracts.csproj
dotnet build src/GastroGestion.Api/GastroGestion.Api.csproj
# Expected: exit 0 all

# Full integration test suite
dotnet test tests/GastroGestion.Api.Tests/ `
    --filter "Category=Integration" `
    --logger "console;verbosity=normal"
# Expected: ALL tests across all 3 slices pass; 0 failed; exit 0

# Domain still zero-dep
Select-String -Path "src/GastroGestion.Domain/GastroGestion.Domain.csproj" `
    -Pattern "PackageReference|ProjectReference"
# Expected: no matches

# No mediator
Select-String -Path "src/" -Pattern "IMediator|ISender" -Recurse
# Expected: no matches

# W-01 async — no sync name remains
Select-String -Path "src/" -Pattern "ResolverPrecioEfectivo[^A]" -Recurse
# Expected: no matches

# PHASE-5 seam markers present (REQ-15 security posture)
Select-String -Path "src/" -Pattern "PHASE-5" -Recurse
# Expected: at least 2 matches

# OpenApi package gone
Select-String -Path "src/GastroGestion.Api/GastroGestion.Api.csproj" -Pattern "AspNetCore.OpenApi"
# Expected: no matches
```

---

## Parallel vs. sequential summary

| Task | Can run in parallel with | Blocked by |
|------|--------------------------|------------|
| WA-01 | — (gate first) | — |
| WA-02 | — | WA-01 |
| WA-03 | — | WA-02 |
| WA-04 | — | WA-03 |
| WA-05 | WA-04 (no mutual dep) | WA-03 |
| WA-06 | WA-04, WA-05 | WA-03 |
| WA-07 | — | WA-04, WA-05, WA-06 all done |
| WA-08 | — | WA-07 |
| WA-09 | WA-08 (no mutual dep) | WA-07 |
| WA-10 | — | WA-08, WA-09 both done |
| WA-11 | — (gate) | WA-10 |
| WA-12 | — | WA-11 gate pass |
| WA-13 | WA-12 (no mutual dep) | WA-11 gate pass |
| WA-14 | WA-15, WA-16, WA-17, WA-18 (all parallel) | WA-12, WA-13 both done |
| WA-15 | WA-14, WA-16, WA-17, WA-18 | WA-12, WA-13 |
| WA-16 | WA-14, WA-15, WA-17, WA-18 | WA-12, WA-13 |
| WA-17 | WA-14, WA-15, WA-16, WA-18 | WA-12, WA-13 |
| WA-18 | WA-14, WA-15, WA-16, WA-17 | WA-12, WA-13 |
| WA-19 | — | WA-14 through WA-18 all done |
| WA-20 | — | WA-19 |
| WA-21 | — (gate) | WA-20 |
| WA-22 | — | WA-21 gate pass |
| WA-23 | — | WA-22 |
| WA-24 | — | WA-23 |
| WA-25 | — | WA-24 |
| WA-26 | WA-27 (no mutual dep) | WA-25 |
| WA-27 | WA-26 | WA-25 |
| WA-28 | — | WA-26, WA-27 both done |
| WA-29 | — | WA-28 |
| WA-30 | — (gate) | WA-29 |

---

## Requirement → task traceability

| REQ | Spec scenarios | Satisfied by |
|-----|----------------|--------------|
| REQ-01 | 01-A, 01-B, 01-C | WA-01 (interface), WA-02 (impl + no blocking), WA-11 gate |
| REQ-02 | 02-A, 02-B, 02-C, 02-D | WA-04 (exception handler), WA-10 (ProblemDetails smoke test) |
| REQ-03 | 03-A, 03-B | WA-05 (ValidationFilter), WA-20 (validation test) |
| REQ-04 | 04-A, 04-B | WA-07 (Program.cs JWT pipeline), WA-06 (JwtBearer package), WA-10 (anon endpoint test) |
| REQ-05 | 05-A, 05-B, 05-C, 05-D, 05-E | WA-08 (DevDataSeeder), WA-10 (seeder smoke), WA-20 (full entity count via GET-all) |
| REQ-06 | 06-A, 06-B, 06-C | WA-07 (health + Swagger dev-only), WA-06 (OpenApi removed), WA-10 (health test) |
| REQ-07 | 07-A | WA-09 (Api.Tests project), WA-10 (smoke test passes) |
| REQ-08 | 08-A, 08-B | WA-12 (GetAllAsync ports + impls), WA-20 (GET-all catalogue tests) |
| REQ-09 | 09-A, 09-B, 09-C, 09-D | WA-14 (Cliente endpoints + handlers), WA-20 (tests) |
| REQ-10 | 10-A, 10-B | WA-15 (Ingrediente endpoints + handlers), WA-20 (tests) |
| REQ-11 | 11-A, 11-B | WA-16 (Plato endpoints + handlers), WA-20 (tests) |
| REQ-12 | 12-A, 12-B | WA-17 (Menu endpoints + handlers), WA-20 (tests) |
| REQ-13 | 13-A, 13-B | WA-18 (Mesa endpoints + handlers), WA-20 (tests) |
| REQ-14 | 14-A | WA-17 (Menu GetAll), WA-20 (seeded menu future-date test) |
| REQ-15 | 15-A through 15-G | WA-22 (DTOs), WA-23 (CrearPedido/AgregarLinea), WA-24 (ConfirmarPrecio), WA-25 (Transicionar/GetById), WA-29 (tests) |
| REQ-16 | 16-A through 16-F | WA-26 (Factura endpoints + handlers), WA-29 (tests) |
| REQ-17 | 17-A through 17-D | WA-27 (Stock endpoints + handlers), WA-29 (tests) |
| REQ-18 | 18-A | WA-14 through WA-18, WA-22 through WA-27 (DTO-only contracts); WA-30 gate |
| REQ-19 | 19-A | WA-14 through WA-18 (direct injection); WA-21, WA-30 gates (no IMediator check) |
| REQ-20 | 20-A | WA-20 (Slice 2 coverage), WA-29 (Slice 3 coverage), WA-30 gate (full suite passes) |

---

## Review Workload Forecast

### Estimated changed lines per slice

| Slice | Tasks | Est. new files | Est. additions | Est. deletions | Notes |
|-------|-------|----------------|----------------|----------------|-------|
| PR 1 — API Foundation | WA-01 through WA-11 | ~18 files | ~750–950L | ~30L | Interface change (~10L), EfectivoPrecioService rewrite (~50L), NotFoundException (~10L), ExceptionHandler (~80L), ValidationFilter + ext (~40L), .csproj edits (~15L), Program.cs rewrite (~100L), DevDataSeeder (~150L), ApiFactory + SmokeTests (~180L), remaining plumbing (~150L). |
| PR 2 — Catalogue | WA-12 through WA-21 | ~55 files | ~900–1,100L | ~10L | 5 port additions (~50L), 5 EF impls (~50L), 1 .csproj edit (~5L), 5×4 Contracts files (DTOs+validators+mapping+requests ~350L), 15 handlers (~300L), 5 endpoint groups (~200L), DI wiring (~50L), 14 test cases (~200L). |
| PR 3 — Transactional/Fiscal/Stock | WA-22 through WA-30 | ~45 files | ~1,000–1,300L | ~10L | Pedido DTOs/validators/mapping (~120L), 5 Pedido handlers (~200L), Factura DTOs/mapping (~150L), 2 new Factura handlers (~80L), Stock DTOs/mapping (~80L), 2 Stock handlers (~60L), 3 endpoint groups (~250L), DI wiring (~50L), 20+ test cases (~300L). |
| **Total** | **WA-01 through WA-30** | **~118 files** | **~2,650–3,350L** | **~50L** | Consistent with Phase 4 scope complexity. |

### 400-line budget analysis

| Metric | Value |
|--------|-------|
| PR 1 additions (est. midpoint) | ~850L |
| PR 2 additions (est. midpoint) | ~1,000L |
| PR 3 additions (est. midpoint) | ~1,150L |
| **Total** | **~3,000L** |
| 400-line budget risk — PR 1 | **High** (~850L >> 400L) |
| 400-line budget risk — PR 2 | **High** (~1,000L >> 400L) |
| 400-line budget risk — PR 3 | **High** (~1,150L >> 400L) |
| Chained PRs recommended | **Yes** |
| Decision needed before apply | **No — already resolved: stacked-to-main, 3 PRs** |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: High

### Sub-split assessment — PR 2 and PR 3

Each of PR 2 (~1,000L) and PR 3 (~1,150L) substantially exceeds 400 lines. However:
- **PR 2 parallel nature** (WA-14 through WA-18 are independent per-aggregate): the apply agent can implement them in sequence in one session; the diff is mostly repetitive boilerplate. A reviewer can scan by aggregate group. No sub-split recommended — the pattern is uniform and reviewable in one pass.
- **PR 3 sequential depth** (Pedido lifecycle requires ordering through WA-22→WA-29): the Pedido + Factura + Stock sub-domains are tightly coupled within the slice. A sub-split into `PR 3a (Pedido)` and `PR 3b (Factura+Stock)` is **possible if the reviewer load becomes a concern**, but not required by default. Flag to reviewer before apply.

### PR mapping

| PR | Slice | Tasks | Est. diff | Base branch | Review focus |
|----|-------|-------|-----------|-------------|--------------|
| PR 1 — API Foundation | Slice A | WA-01 → WA-11 | ~850L | `main` | W-01 async + Domain zero-dep gate; ExceptionHandler RFC 7807; ValidationFilter; Program.cs composition order; DevDataSeeder (ExentoIVA, FechaVigencia, tomorrow-date); JwtBearer + FluentValidation added; OpenApi removed; smoke tests green. |
| PR 2 — Catalogue | Slice B | WA-12 → WA-21 | ~1,000L | `main` (after PR 1) | GetAllAsync ports + EF impls; 5×4-file Contracts pattern; 15 catalogue handlers (real domain factory calls); 5 endpoint groups (AllowAnonymous, TypedResults, WithValidation); DI wiring; 14 integration tests. |
| PR 3 — Transactional | Slice C | WA-22 → WA-30 | ~1,150L | `main` (after PR 2) | Pedido lifecycle (ConfirmarPrecio exercises W-01 on HTTP stack — deadlock regression); PHASE-5 seam markers; Factura CrearFactura wire-up + RegistrarPago; Stock append-only; 20+ integration tests; full suite green. |

**Chain order:** PR 1 → PR 2 (after PR 1 merges) → PR 3 (after PR 2 merges). Each PR is independently deployable and integration-test-covered. Each is independently revertible.
