using GastroGestion.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GastroGestion.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can construct the context without DI.
/// Reads the connection string from appsettings.Development.json or the
/// <c>ConnectionStrings__GastroGestion</c> environment variable.
/// Injects a NullDomainEventDispatcher (design-time never dispatches events).
/// </summary>
public sealed class GastroGestionDbContextFactory
    : IDesignTimeDbContextFactory<GastroGestionDbContext>
{
    private const string DefaultConnectionString =
        @"Server=(localdb)\mssqllocaldb;Database=GastroGestion;Trusted_Connection=True;TrustServerCertificate=True";

    public GastroGestionDbContext CreateDbContext(string[] args)
    {
        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = cfg.GetConnectionString("GastroGestion") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<GastroGestionDbContext>()
            .UseSqlServer(cs, sql =>
                sql.MigrationsAssembly(typeof(GastroGestionDbContext).Assembly.FullName))
            .Options;

        return new GastroGestionDbContext(options, new NullDomainEventDispatcher());
    }
}
