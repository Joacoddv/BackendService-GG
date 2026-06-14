using GastroGestion.Domain.Ingredientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

internal sealed class IngredienteConfiguration : IEntityTypeConfiguration<Ingrediente>
{
    public void Configure(EntityTypeBuilder<Ingrediente> b)
    {
        b.ToTable("Ingredientes");

        b.HasKey(i => i.Id);
        b.Property(i => i.Id).ValueGeneratedNever();

        b.Property(i => i.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(i => i.UnidadBase)
            .HasConversion<int>();

        b.Property(i => i.Activo);

        b.HasIndex(i => i.Nombre).IsUnique();
    }
}
