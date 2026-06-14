using GastroGestion.Domain.Mesas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

internal sealed class MesaConfiguration : IEntityTypeConfiguration<Mesa>
{
    public void Configure(EntityTypeBuilder<Mesa> b)
    {
        b.ToTable("Mesas");

        b.HasKey(m => m.Id);
        b.Property(m => m.Id).ValueGeneratedNever();

        b.Property(m => m.Numero);
        b.Property(m => m.Capacidad);

        b.Property(m => m.Estado)
            .HasConversion<int>(); // EstadoMesa enum

        b.Property(m => m.Activa);

        b.Property(m => m.PedidoActivoId); // nullable Guid

        // SQL Server rowversion — store-generated; domain default [] is ignored on insert
        b.Property(m => m.RowVersion).IsRowVersion();

        b.HasIndex(m => m.Numero).IsUnique();
    }
}
