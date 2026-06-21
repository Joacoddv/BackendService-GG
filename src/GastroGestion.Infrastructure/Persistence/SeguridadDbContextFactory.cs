using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GastroGestion.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can construct the SeguridadDbContext without DI.
/// Reads the connection string from appsettings.Development.json or the
/// <c>ConnectionStrings__Seguridad</c> environment variable.
/// </summary>
public sealed class SeguridadDbContextFactory
    : IDesignTimeDbContextFactory<SeguridadDbContext>
{
    private const string DefaultConnectionString =
        @"Server=(localdb)\mssqllocaldb;Database=GastroGestionSeguridad;Trusted_Connection=True;TrustServerCertificate=True";

    public SeguridadDbContext CreateDbContext(string[] args)
    {
        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = cfg.GetConnectionString("Seguridad") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<SeguridadDbContext>()
            .UseSqlServer(cs, sql =>
                sql.MigrationsAssembly(typeof(SeguridadDbContext).Assembly.FullName))
            .Options;

        return new SeguridadDbContext(options);
    }
}
