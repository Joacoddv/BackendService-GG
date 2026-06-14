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
    private DbContextOptions<GastroGestionDbContext> _options = null!;

    public LocalDbFixture()
    {
        // Unique per class instance to avoid cross-test-class bleed
        _databaseName = $"GastroGestion_Test_{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        var connectionString =
            $"Server={LocalDbServer};Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

        _options = new DbContextOptionsBuilder<GastroGestionDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
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
}
