using GastroGestion.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Usuario aggregate. Applied by SeguridadDbContext
/// (marked ISecurityEntityTypeConfiguration so the main context skips it).
/// </summary>
internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>, ISecurityEntityTypeConfiguration
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios");

        b.HasKey(u => u.Id);
        b.Property(u => u.Id).ValueGeneratedNever();

        b.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320);

        b.Property(u => u.NombreCompleto)
            .IsRequired()
            .HasMaxLength(200);

        // Stored as the enum's int backing (Administrador=0...Cocinero=3)
        // Mirrors how Cliente.CondicionIVA is mapped (ClienteConfiguration.cs:24-25)
        b.Property(u => u.Rol)
            .HasConversion<int>();

        // Opaque PBKDF2-SHA256 string from PasswordHasher<Usuario> (ADR-2)
        b.Property(u => u.PasswordHash)
            .IsRequired();

        b.Property(u => u.Activo);

        // Email uniqueness is an Infrastructure invariant, not a domain rule (mirrors Cliente CUIT)
        b.HasIndex(u => u.Email).IsUnique();
    }
}
