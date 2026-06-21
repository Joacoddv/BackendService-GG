using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Infrastructure.Events;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Common;

/// <summary>
/// Per-test-class LocalDB fixture. Creates an isolated database for each test class,
/// applies all EF Core migrations before tests run, and drops the database on dispose.
/// </summary>
public sealed class LocalDbFixture : IAsyncLifetime
{
    private const string LocalDbServer = @"(localdb)\mssqllocaldb";

    private readonly string _databaseName;
    private readonly string _seguridadDatabaseName;
    private DbContextOptions<GastroGestionDbContext> _options = null!;
    private DbContextOptions<SeguridadDbContext> _seguridadOptions = null!;

    public LocalDbFixture()
    {
        // Unique per class instance to avoid cross-test-class bleed
        _databaseName          = $"GastroGestion_Test_{Guid.NewGuid():N}";
        _seguridadDatabaseName = $"GastroGestionSeguridad_Test_{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        var connectionString =
            $"Server={LocalDbServer};Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

        _options = new DbContextOptionsBuilder<GastroGestionDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var seguridadConnectionString =
            $"Server={LocalDbServer};Database={_seguridadDatabaseName};Trusted_Connection=True;TrustServerCertificate=True";

        _seguridadOptions = new DbContextOptionsBuilder<SeguridadDbContext>()
            .UseSqlServer(seguridadConnectionString)
            .Options;

        await using var db = CreateContext();
        await db.Database.MigrateAsync();

        await using var seguridadDb = CreateSeguridadContext();
        await seguridadDb.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();

        await using var seguridadDb = CreateSeguridadContext();
        await seguridadDb.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Creates a fresh DbContext instance sharing the same database connection options.
    /// Each call returns a new context to simulate separate scopes / requests.
    /// </summary>
    public GastroGestionDbContext CreateContext()
        => new(_options, new NullDomainEventDispatcher());

    /// <summary>
    /// Creates a fresh DbContext with a capturing dispatcher for event-assertion tests.
    /// </summary>
    public GastroGestionDbContext CreateContext(IDomainEventDispatcher dispatcher)
        => new(_options, dispatcher);

    /// <summary>Creates a fresh SeguridadDbContext (Usuario, RefreshToken) for the isolated security test DB.</summary>
    public SeguridadDbContext CreateSeguridadContext()
        => new(_seguridadOptions);
}
