using GastroGestion.Domain.Usuarios;
using GastroGestion.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the security/identity database (separate physical database from the
/// domain context). Owns the Usuario and RefreshToken aggregates. Neither raises domain
/// events, so this is a plain context (no dispatcher).
/// </summary>
public sealed class SeguridadDbContext : DbContext
{
    public SeguridadDbContext(DbContextOptions<SeguridadDbContext> options) : base(options) { }

    public DbSet<Usuario>      Usuarios      => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        // Apply ONLY the security configurations from the shared assembly.
        => modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SeguridadDbContext).Assembly,
            t => typeof(ISecurityEntityTypeConfiguration).IsAssignableFrom(t));
}
