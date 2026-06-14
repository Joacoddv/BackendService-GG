using GastroGestion.Domain.Menus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> b)
    {
        b.ToTable("Menus");

        b.HasKey(m => m.Id);
        b.Property(m => m.Id).ValueGeneratedNever();

        b.Property(m => m.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(m => m.FechaVigencia); // DateOnly → date column

        b.Property(m => m.Activo);

        // Items: owned entity collection with nullable PrecioOverride (Dinero)
        b.OwnsMany(m => m.Items, it =>
        {
            it.ToTable("MenuItems");
            it.WithOwner().HasForeignKey("MenuId");
            it.HasKey("Id");
            it.Property<Guid>("Id").ValueGeneratedNever();
            it.Property(x => x.PlatoId);

            // Nullable PrecioOverride — distinct column prefix avoids collision
            it.OwnsOne(x => x.PrecioOverride, m =>
            {
                m.Property(d => d.Monto)
                    .HasColumnName("PrecioOverride_Monto")
                    .HasColumnType("decimal(18,2)");
                m.Property(d => d.Moneda)
                    .HasColumnName("PrecioOverride_Moneda")
                    .HasConversion<int>();
            });
            it.Navigation(x => x.PrecioOverride).IsRequired(false); // nullable override
        });

        b.Navigation(m => m.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field); // backing field: _items
    }
}
