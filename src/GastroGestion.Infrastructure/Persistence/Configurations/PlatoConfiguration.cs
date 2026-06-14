using GastroGestion.Domain.Platos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

internal sealed class PlatoConfiguration : IEntityTypeConfiguration<Plato>
{
    public void Configure(EntityTypeBuilder<Plato> b)
    {
        b.ToTable("Platos");

        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();

        b.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(p => p.AlicuotaIVA)
            .HasConversion<int>();

        b.Property(p => p.Activo);

        // PrecioBase: owned Dinero — distinct column names to avoid collision
        b.OwnsOne(p => p.PrecioBase, m =>
        {
            m.Property(d => d.Monto)
                .HasColumnName("PrecioBase_Monto")
                .HasColumnType("decimal(18,2)");
            m.Property(d => d.Moneda)
                .HasColumnName("PrecioBase_Moneda")
                .HasConversion<int>();
        });
        b.Navigation(p => p.PrecioBase).IsRequired();

        // LineasReceta: owned entity collection with nested Cantidad VO
        b.OwnsMany(p => p.LineasReceta, r =>
        {
            r.ToTable("PlatoLineasReceta");
            r.WithOwner().HasForeignKey("PlatoId");
            r.HasKey("Id");
            r.Property<Guid>("Id").ValueGeneratedNever();
            r.Property(x => x.IngredienteId);
            r.Property(x => x.PlatoReferenciadoId); // nullable sub-recipe seam
            r.OwnsOne(x => x.Cantidad, c =>
            {
                c.Property(q => q.Valor)
                    .HasColumnName("Cantidad_Valor")
                    .HasColumnType("decimal(18,3)");
                c.Property(q => q.Unidad)
                    .HasColumnName("Cantidad_Unidad")
                    .HasConversion<int>();
            });
            r.Navigation(x => x.Cantidad).IsRequired();
        });

        b.Navigation(p => p.LineasReceta)
            .UsePropertyAccessMode(PropertyAccessMode.Field); // backing field: _lineasReceta
    }
}
