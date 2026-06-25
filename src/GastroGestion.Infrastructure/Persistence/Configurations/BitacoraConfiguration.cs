using GastroGestion.Domain.Bitacora;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="BitacoraEntry"/> append-only audit table.
/// NOT marked as <see cref="ISecurityEntityTypeConfiguration"/> — belongs to the main context.
/// </summary>
internal sealed class BitacoraConfiguration : IEntityTypeConfiguration<BitacoraEntry>
{
    public void Configure(EntityTypeBuilder<BitacoraEntry> b)
    {
        b.ToTable("Bitacora");

        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();

        b.Property(e => e.UsuarioId).IsRequired();

        b.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256);

        // Nullable: anonymous (unauthenticated) entries have no role.
        b.Property(e => e.Rol)
            .HasConversion<int?>();

        b.Property(e => e.Accion)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(e => e.Detalle)
            .HasMaxLength(1000); // nullable

        b.Property(e => e.ResultadoHttp).IsRequired();

        b.Property(e => e.FechaUtc).IsRequired();

        b.HasIndex(e => e.FechaUtc);
        b.HasIndex(e => e.UsuarioId);
    }
}
