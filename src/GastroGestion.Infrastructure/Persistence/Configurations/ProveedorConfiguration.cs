using GastroGestion.Domain.Proveedores;
using GastroGestion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

internal sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> b)
    {
        b.ToTable("Proveedores");

        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();

        b.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(p => p.Cuit)
            .HasConversion<string?>(
                cuit => cuit == null ? null : cuit.Valor,
                s    => s == null ? null : new Cuit(s))
            .HasMaxLength(11);

        b.Property(p => p.Email)
            .HasConversion<string?>(
                email => email == null ? null : email.Valor,
                s     => s == null ? null : new Email(s))
            .HasMaxLength(320);

        b.Property(p => p.Telefono)
            .HasMaxLength(40);

        b.Property(p => p.Activo);

        b.HasIndex(p => p.Cuit)
            .IsUnique()
            .HasFilter("[Cuit] IS NOT NULL");
    }
}
