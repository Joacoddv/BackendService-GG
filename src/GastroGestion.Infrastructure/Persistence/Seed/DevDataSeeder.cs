using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Security;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.Ingredientes;
using GastroGestion.Domain.Mesas;
using GastroGestion.Domain.Menus;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.Services;
using GastroGestion.Domain.Stock;
using GastroGestion.Domain.Usuarios;
using GastroGestion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GastroGestion.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent runtime seeder for the Development environment.
/// Called from Program.cs after auto-migrate, inside the Development check.
/// Uses domain factories exclusively — no raw SQL, no direct property sets.
/// Guard: skips entirely if Clientes table already has any rows.
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        var db             = sp.GetRequiredService<GastroGestionDbContext>();
        var config         = sp.GetRequiredService<IConfiguration>();
        var clienteRepo    = sp.GetRequiredService<IClienteRepository>();
        var ingredienteRepo= sp.GetRequiredService<IIngredienteRepository>();
        var platoRepo      = sp.GetRequiredService<IPlatoRepository>();
        var menuRepo       = sp.GetRequiredService<IMenuRepository>();
        var mesaRepo       = sp.GetRequiredService<IMesaRepository>();
        var pedidoRepo     = sp.GetRequiredService<IPedidoRepository>();
        var facturaRepo    = sp.GetRequiredService<IFacturaRepository>();
        var uow            = sp.GetRequiredService<IUnitOfWork>();
        var precioService  = sp.GetRequiredService<IEfectivoPrecioService>();

        // Admin user seed — independent of catalogue seed; guarded by its own AnyAsync (ADR-9).
        // Credentials read from configuration; fall back to documented dev constants when absent.
        // Dev fallback: admin@gastrogestion.local / Admin1234!  (documented in appsettings.Development.json)
        await SeedAdminUsuarioAsync(config, sp, ct);

        // Cocinero users — independent of the catalogue seed (own idempotency guard).
        // Populates GET /usuarios/cocineros and the asignar-cocinero picker for local dev.
        await SeedCocinerosAsync(sp, ct);

        // Mozo user — independent of the catalogue seed (own idempotency guard).
        // Lets the waiter create-order flow be exercised under a real Mozo role (not as admin).
        await SeedMozoAsync(sp, ct);

        // Stock purchases — independent, idempotent. Seeds an opening Compra per ingredient so dev
        // balances start positive. On a fresh DB ingredientes don't exist yet here (no-op); the
        // call at the end of the catalogue block covers that pass. On an already-seeded DB this is
        // where purchases get backfilled.
        await SeedComprasAsync(sp, ct);

        // Idempotency guard: skip catalogue seed if already seeded
        if (await db.Clientes.AnyAsync(ct))
            return;

        // ── Clientes (3) ─────────────────────────────────────────────────────────

        var cliente1 = Cliente.Crear(
            "Consumidor Demo",
            CondicionIVA.ConsumidorFinal,
            null,
            null);

        // 33-69345023-9 is a valid CUIT (check-digit verified)
        var cliente2 = Cliente.Crear(
            "RI Demo",
            CondicionIVA.ResponsableInscripto,
            new Cuit("33-69345023-9"),
            new Email("ri@demo.test"));

        // ExentoIVA — correct CLR name (NOT Exento)
        var cliente3 = Cliente.Crear(
            "Exento Demo",
            CondicionIVA.ExentoIVA,
            null,
            null);

        // A second ConsumidorFinal (no CUIT) so the address book has more demo data.
        var cliente4 = Cliente.Crear(
            "Consumidor Demo 2",
            CondicionIVA.ConsumidorFinal,
            null,
            new Email("consumidor2@demo.test"));

        // Saved addresses — so the "usar dirección guardada" picker on the delivery
        // order form and the cliente detail screen have demo data out of the box.
        cliente2.AgregarDireccion(new Direccion(Guid.NewGuid(), "Av. Corrientes", "1234", "CABA", "CABA", "1043"));
        cliente2.AgregarDireccion(new Direccion(Guid.NewGuid(), "Av. Santa Fe", "3456", "CABA", "CABA", "1425"));
        cliente3.AgregarDireccion(new Direccion(Guid.NewGuid(), "Calle Falsa", "123", "Rosario", "Santa Fe", "2000"));
        cliente4.AgregarDireccion(new Direccion(Guid.NewGuid(), "Belgrano", "456", "Córdoba", "Córdoba", "5000"));
        cliente4.AgregarDireccion(new Direccion(Guid.NewGuid(), "San Martín", "789", "Mendoza", "Mendoza", "5500"));

        await clienteRepo.AddAsync(cliente1, ct);
        await clienteRepo.AddAsync(cliente2, ct);
        await clienteRepo.AddAsync(cliente3, ct);
        await clienteRepo.AddAsync(cliente4, ct);

        // ── Ingredientes (5) ──────────────────────────────────────────────────────

        var harina = Ingrediente.Crear("Harina",  UnidadDeMedida.Kilogramo);
        var agua   = Ingrediente.Crear("Agua",    UnidadDeMedida.Litro);
        var sal    = Ingrediente.Crear("Sal",     UnidadDeMedida.Gramo);
        var huevo  = Ingrediente.Crear("Huevo",   UnidadDeMedida.Unidad);
        var aceite = Ingrediente.Crear("Aceite",  UnidadDeMedida.Mililitro);

        await ingredienteRepo.AddAsync(harina,  ct);
        await ingredienteRepo.AddAsync(agua,    ct);
        await ingredienteRepo.AddAsync(sal,     ct);
        await ingredienteRepo.AddAsync(huevo,   ct);
        await ingredienteRepo.AddAsync(aceite,  ct);

        // ── Platos (3) ────────────────────────────────────────────────────────────

        var milanesa = Plato.Crear("Milanesa", new Dinero(850m), AlicuotaIVA.General);
        milanesa.AgregarLineaReceta(harina.Id, new Cantidad(200m, UnidadDeMedida.Gramo));
        milanesa.AgregarLineaReceta(huevo.Id,  new Cantidad(2m,   UnidadDeMedida.Unidad));

        var ensalada = Plato.Crear("Ensalada", new Dinero(450m), AlicuotaIVA.ReducidoA);

        var tarta = Plato.Crear("Tarta", new Dinero(650m), AlicuotaIVA.General);
        tarta.AgregarLineaReceta(harina.Id, new Cantidad(300m, UnidadDeMedida.Gramo));
        tarta.AgregarLineaReceta(sal.Id,    new Cantidad(5m,   UnidadDeMedida.Gramo));

        await platoRepo.AddAsync(milanesa, ct);
        await platoRepo.AddAsync(ensalada, ct);
        await platoRepo.AddAsync(tarta,    ct);

        // ── Menu (1) — FechaVigencia = tomorrow (computed at runtime, never hardcoded) ──

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var menu = Menu.Crear("Menú del Día", tomorrow); // param: FechaVigencia (NOT fechaMenu)
        menu.AgregarItem(milanesa.Id, null);              // no override → uses PrecioBase at order time
        menu.AgregarItem(tarta.Id, new Dinero(600m));     // one PrecioOverride

        await menuRepo.AddAsync(menu, ct);

        // ── Mesas (4) ─────────────────────────────────────────────────────────────

        var mesa1 = Mesa.Crear(1, 2);
        var mesa2 = Mesa.Crear(2, 4);
        var mesa3 = Mesa.Crear(3, 4);
        var mesa4 = Mesa.Crear(4, 6);

        await mesaRepo.AddAsync(mesa1, ct);
        await mesaRepo.AddAsync(mesa2, ct);
        await mesaRepo.AddAsync(mesa3, ct);
        await mesaRepo.AddAsync(mesa4, ct);

        // First SaveChanges so Platos/Mesas/Menus are persisted before the price service
        // needs to load them via the repo (EF tracks all in this scope, so they are available).
        await uow.SaveChangesAsync(ct);

        // ── Pedido Salon (1) ──────────────────────────────────────────────────────

        var pedidoSalon = Pedido.Crear(
            TipoPedido.Salon,
            mesa1.Id,
            cliente1.Id,
            null,
            DateTime.UtcNow);

        mesa1.AsignarPedido(pedidoSalon.Id);

        var lineaSalon = pedidoSalon.AgregarLinea(milanesa.Id, 2);
        var (precioSalon, ivaSalon) = await precioService.ResolverPrecioEfectivoAsync(
            milanesa.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct);
        lineaSalon.ConfirmarPrecio(precioSalon, ivaSalon);

        await pedidoRepo.AddAsync(pedidoSalon, ct);

        // ── Ordenes de Trabajo for the Salon pedido (kitchen board demo data) ───────
        // Milanesa carries recipe lines, so OTs can be generated. This leaves a Creada
        // OT on the Cocina board so asignar-cocinero is testable without manual setup.
        var recetaSnapshots = new Dictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>>
        {
            [milanesa.Id] = milanesa.LineasReceta
                .Select(lr => new LineaRecetaSnapshot(lr.IngredienteId, lr.Cantidad))
                .ToList()
                .AsReadOnly()
        };
        pedidoSalon.GenerarOrdenesTrabajo(recetaSnapshots);

        // ── Pedido TakeAway (1) ────────────────────────────────────────────────────

        var pedidoTakeAway = Pedido.Crear(
            TipoPedido.TakeAway,
            null,
            cliente1.Id,
            null,
            DateTime.UtcNow);

        var lineaTakeAway = pedidoTakeAway.AgregarLinea(tarta.Id, 1);
        var (precioTakeAway, ivaTakeAway) = await precioService.ResolverPrecioEfectivoAsync(
            tarta.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct);
        lineaTakeAway.ConfirmarPrecio(precioTakeAway, ivaTakeAway);

        await pedidoRepo.AddAsync(pedidoTakeAway, ct);

        await uow.SaveChangesAsync(ct);

        // ── Factura (1) — TicketInterno from TakeAway ──────────────────────────────

        var lineasFactura = new List<FacturaLinea>();
        foreach (var linea in pedidoTakeAway.Lineas)
        {
            if (linea.PrecioUnitario is null || linea.IVA is null)
                continue;

            lineasFactura.Add(new FacturaLinea(
                Guid.NewGuid(),
                linea.Id,
                linea.PrecioUnitario,
                linea.IVA,
                linea.Cantidad));
        }

        var factura = Factura.CrearTicket(
            cliente1.Id,
            [pedidoTakeAway.Id],
            lineasFactura);

        await facturaRepo.AddAsync(factura, ct);
        await uow.SaveChangesAsync(ct);

        // Fresh-DB pass: ingredientes now exist, so seed their opening purchases.
        await SeedComprasAsync(sp, ct);
    }

    /// <summary>
    /// Seeds an opening Compra (10000 units) for every ingredient so dev stock balances start
    /// positive. Idempotent: skips when no ingredientes exist yet, or when any Compra already exists.
    /// </summary>
    private static async Task SeedComprasAsync(IServiceProvider sp, CancellationToken ct)
    {
        var db              = sp.GetRequiredService<GastroGestionDbContext>();
        var ingredienteRepo = sp.GetRequiredService<IIngredienteRepository>();
        var stockRepo       = sp.GetRequiredService<IMovimientoStockRepository>();
        var uow             = sp.GetRequiredService<IUnitOfWork>();

        var ingredientes = await ingredienteRepo.GetAllAsync(ct);
        if (ingredientes.Count == 0)
            return; // ingredientes not seeded yet (fresh DB, first pass)

        if (await db.MovimientosStock.AnyAsync(m => m.Tipo == TipoMovimientoStock.Compra, ct))
            return; // opening purchases already seeded

        foreach (var ingrediente in ingredientes)
            await stockRepo.AddAsync(MovimientoStock.RegistrarCompra(ingrediente.Id, 10000m), ct);

        await uow.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Seeds the initial admin user if no Usuario rows exist (AUTH-08).
    /// Credentials are read from Seed:AdminEmail / Seed:AdminPassword in configuration.
    /// Dev fallback (documented): admin@gastrogestion.local / Admin1234!
    /// </summary>
    private static async Task SeedAdminUsuarioAsync(
        IConfiguration config,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var usuarioRepo   = sp.GetRequiredService<IUsuarioRepository>();
        var hasher        = sp.GetRequiredService<IPasswordHasher>();
        var seguridadUow  = sp.GetRequiredService<ISeguridadUnitOfWork>();

        // Idempotency guard: return early if any user already exists (AUTH-08.3)
        if (await usuarioRepo.AnyAsync(ct))
            return;

        // Credentials from configuration; documented dev fallback when keys are absent (ADR-9)
        var adminEmail    = config["Seed:AdminEmail"]    ?? "admin@gastrogestion.local";
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin1234!";

        var usuario = Usuario.Crear(adminEmail, "Admin", RolUsuario.Administrador, "placeholder");
        var hash    = hasher.Hash(usuario, adminPassword);

        // Re-create with the real hash — factory validates all fields including hash (non-empty)
        var adminUsuario = Usuario.Crear(adminEmail, "Admin", RolUsuario.Administrador, hash);
        await usuarioRepo.AddAsync(adminUsuario, ct);
        await seguridadUow.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Seeds two Cocinero users for the Development environment so
    /// GET /usuarios/cocineros and the asignar-cocinero picker are populated.
    /// Idempotency guard: skips entirely if any Cocinero already exists.
    /// Dev credentials (documented): pedro.cocina@gastrogestion.local /
    /// ana.cocina@gastrogestion.local, password Cocina1234!
    /// </summary>
    private static async Task SeedCocinerosAsync(IServiceProvider sp, CancellationToken ct)
    {
        var usuarioRepo  = sp.GetRequiredService<IUsuarioRepository>();
        var hasher       = sp.GetRequiredService<IPasswordHasher>();
        var seguridadUow = sp.GetRequiredService<ISeguridadUnitOfWork>();

        // Idempotency guard: return early if any Cocinero already exists
        if ((await usuarioRepo.GetByRolAsync(RolUsuario.Cocinero, ct)).Any())
            return;

        await AddCocineroAsync(usuarioRepo, hasher, "pedro.cocina@gastrogestion.local", "Pedro Parrillero", ct);
        await AddCocineroAsync(usuarioRepo, hasher, "ana.cocina@gastrogestion.local",   "Ana Salsera",      ct);
        await seguridadUow.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Seeds one Mozo (waiter) user for the Development environment so the waiter create-order
    /// flow can be tested under a real Mozo role instead of as the admin.
    /// Idempotency guard: skips entirely if any Mozo already exists.
    /// Dev credentials (documented): mozo@gastrogestion.local / Mozo1234!
    /// </summary>
    private static async Task SeedMozoAsync(IServiceProvider sp, CancellationToken ct)
    {
        var usuarioRepo  = sp.GetRequiredService<IUsuarioRepository>();
        var hasher       = sp.GetRequiredService<IPasswordHasher>();
        var seguridadUow = sp.GetRequiredService<ISeguridadUnitOfWork>();

        // Idempotency guard: return early if any Mozo already exists
        if ((await usuarioRepo.GetByRolAsync(RolUsuario.Mozo, ct)).Any())
            return;

        // Two-step create mirrors the other seeds: the factory validates the hash is non-empty,
        // so build once with a placeholder to obtain a Usuario for the hasher, then re-create
        // with the real hash. Password is a documented dev constant.
        const string email = "mozo@gastrogestion.local";
        var placeholder = Usuario.Crear(email, "Mateo Mozo", RolUsuario.Mozo, "placeholder");
        var hash        = hasher.Hash(placeholder, "Mozo1234!");

        var mozo = Usuario.Crear(email, "Mateo Mozo", RolUsuario.Mozo, hash);
        await usuarioRepo.AddAsync(mozo, ct);
        await seguridadUow.SaveChangesAsync(ct);
    }

    private static async Task AddCocineroAsync(
        IUsuarioRepository usuarioRepo,
        IPasswordHasher    hasher,
        string             email,
        string             nombreCompleto,
        CancellationToken  ct)
    {
        // Two-step create mirrors the admin seed: factory validates the hash is non-empty,
        // so build once with a placeholder to obtain a Usuario for the hasher, then re-create
        // with the real hash. Password is a documented dev constant (not used to log in as the cook).
        var placeholder = Usuario.Crear(email, nombreCompleto, RolUsuario.Cocinero, "placeholder");
        var hash        = hasher.Hash(placeholder, "Cocina1234!");

        var cocinero = Usuario.Crear(email, nombreCompleto, RolUsuario.Cocinero, hash);
        await usuarioRepo.AddAsync(cocinero, ct);
    }
}
