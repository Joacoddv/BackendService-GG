using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Infrastructure.Events;
using GastroGestion.Infrastructure.Persistence;
using GastroGestion.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GastroGestion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<GastroGestionDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("GastroGestion"),
                sql => sql.MigrationsAssembly(typeof(GastroGestionDbContext).Assembly.FullName)));

        // Repositories — Slice A (catalogue)
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IIngredienteRepository, IngredienteRepository>();
        services.AddScoped<IPlatoRepository, PlatoRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IMesaRepository, MesaRepository>();

        // Repositories — Slice B (transactional)
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IMovimientoStockRepository, MovimientoStockRepository>();

        // Repositories — Slice C (fiscal)
        services.AddScoped<IFacturaRepository, FacturaRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain event dispatcher
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

        return services;
    }
}
