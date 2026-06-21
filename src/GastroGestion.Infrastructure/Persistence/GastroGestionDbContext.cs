using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.Ingredientes;
using GastroGestion.Domain.Mesas;
using GastroGestion.Domain.Menus;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.Stock;
using GastroGestion.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence;

/// <summary>
/// Primary EF Core context for GastroGestion. Contains DbSets for all 8 aggregate roots.
/// Owned types (LineaPedido, OrdenTrabajo, FacturaLinea, Pago, Direccion, LineaReceta, MenuItem)
/// are reached only through their owner — no DbSet exposed for them.
/// </summary>
public sealed class GastroGestionDbContext : DbContext
{
    private readonly IDomainEventDispatcher _dispatcher;

    public GastroGestionDbContext(
        DbContextOptions<GastroGestionDbContext> options,
        IDomainEventDispatcher dispatcher) : base(options)
        => _dispatcher = dispatcher;

    public DbSet<Cliente>         Clientes         => Set<Cliente>();
    public DbSet<Ingrediente>     Ingredientes     => Set<Ingrediente>();
    public DbSet<Plato>           Platos           => Set<Plato>();
    public DbSet<Menu>            Menus            => Set<Menu>();
    public DbSet<Mesa>            Mesas            => Set<Mesa>();
    public DbSet<Pedido>          Pedidos          => Set<Pedido>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<Factura>         Facturas         => Set<Factura>();
    public DbSet<Usuario>         Usuarios         => Set<Usuario>();
    public DbSet<RefreshToken>    RefreshTokens    => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(GastroGestionDbContext).Assembly);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        GuardAppendOnlyLedger();                                   // (a) reject ledger mutation/delete
        var roots = CollectAggregatesWithEvents();                 // (b) snapshot event-bearing roots
        var result = await base.SaveChangesAsync(cancellationToken); // (c) commit
        await DispatchAndClear(roots, cancellationToken);           // (d) post-commit dispatch
        return result;
    }

    private void GuardAppendOnlyLedger()
    {
        foreach (var entry in ChangeTracker.Entries<MovimientoStock>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException(
                    "MovimientoStock is append-only; only inserts are permitted.");
        }
    }

    private List<AggregateRoot> CollectAggregatesWithEvents()
        => ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

    private async Task DispatchAndClear(List<AggregateRoot> roots, CancellationToken ct)
    {
        foreach (var root in roots)
        {
            await _dispatcher.DispatchAsync(root.DomainEvents, ct);
            root.ClearDomainEvents();
        }
    }
}
