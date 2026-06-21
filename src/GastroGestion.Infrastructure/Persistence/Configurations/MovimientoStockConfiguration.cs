using GastroGestion.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for MovimientoStock append-only aggregate.
/// Note: SaveChanges guard rejects Modified/Deleted states for this entity.
/// </summary>
internal sealed class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
{
    public void Configure(EntityTypeBuilder<MovimientoStock> b)
    {
        b.ToTable("MovimientosStock");

        b.HasKey(m => m.Id);
        b.Property(m => m.Id).ValueGeneratedNever();

        b.Property(m => m.IngredienteId);
        b.Property(m => m.Cantidad).HasColumnType("decimal(18,3)"); // signed decimal — NOT a VO
        b.Property(m => m.Tipo).HasConversion<int>();               // TipoMovimientoStock
        b.Property(m => m.FechaMovimiento);
        b.Property(m => m.OrdenTrabajoId);   // nullable Guid
        b.Property(m => m.LineaPedidoId);    // nullable Guid
        b.Property(m => m.Lote);             // nullable string
        b.Property(m => m.FechaVencimiento); // DateOnly? → nullable date column
        b.Property(m => m.ProveedorId);      // nullable Guid — supplier on Compra movements

        b.HasIndex(m => m.IngredienteId); // SUM query performance
    }
}
