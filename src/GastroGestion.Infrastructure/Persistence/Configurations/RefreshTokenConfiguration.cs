using GastroGestion.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the RefreshToken aggregate. Picked up automatically by
/// ApplyConfigurationsFromAssembly in GastroGestionDbContext.
/// </summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");

        b.HasKey(t => t.Id);
        b.Property(t => t.Id).ValueGeneratedNever();

        b.Property(t => t.UsuarioId).IsRequired();

        // SHA-256 hex of the raw token (64 chars). Looked up on every refresh → unique index.
        b.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);
        b.HasIndex(t => t.TokenHash).IsUnique();

        b.Property(t => t.ExpiresAtUtc).IsRequired();
        b.Property(t => t.CreadoEnUtc).IsRequired();
        b.Property(t => t.RevocadoEnUtc);
    }
}
