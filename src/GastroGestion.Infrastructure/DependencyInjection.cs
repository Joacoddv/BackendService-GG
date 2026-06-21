using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Security;
using GastroGestion.Infrastructure.Events;
using GastroGestion.Infrastructure.Persistence;
using GastroGestion.Infrastructure.Persistence.Repositories;
using GastroGestion.Infrastructure.Security;
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

        // Separate security/identity database (Usuario, RefreshToken).
        services.AddDbContext<SeguridadDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Seguridad"),
                sql => sql.MigrationsAssembly(typeof(SeguridadDbContext).Assembly.FullName)));

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
        services.AddScoped<IProveedorRepository, ProveedorRepository>();

        // Repositories — Auth (Phase 5)
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISeguridadUnitOfWork, SeguridadUnitOfWork>();

        // Domain event dispatcher
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

        // Security — Auth (Phase 5)
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();

        return services;
    }
}
