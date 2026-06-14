using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.Ingredientes;
using GastroGestion.Domain.Mesas;
using GastroGestion.Domain.Menus;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.Services;
using GastroGestion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
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
        var clienteRepo    = sp.GetRequiredService<IClienteRepository>();
        var ingredienteRepo= sp.GetRequiredService<IIngredienteRepository>();
        var platoRepo      = sp.GetRequiredService<IPlatoRepository>();
        var menuRepo       = sp.GetRequiredService<IMenuRepository>();
        var mesaRepo       = sp.GetRequiredService<IMesaRepository>();
        var pedidoRepo     = sp.GetRequiredService<IPedidoRepository>();
        var facturaRepo    = sp.GetRequiredService<IFacturaRepository>();
        var uow            = sp.GetRequiredService<IUnitOfWork>();
        var precioService  = sp.GetRequiredService<IEfectivoPrecioService>();

        // Idempotency guard: skip if already seeded
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

        await clienteRepo.AddAsync(cliente1, ct);
        await clienteRepo.AddAsync(cliente2, ct);
        await clienteRepo.AddAsync(cliente3, ct);

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
    }
}
