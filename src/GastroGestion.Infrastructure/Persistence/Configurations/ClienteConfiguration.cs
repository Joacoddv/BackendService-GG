using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("Clientes");

        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();

        b.Property(c => c.NumeroCliente); // Guid, get-only — EF reads via backing

        b.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(c => c.CondicionIVA)
            .HasConversion<int>();

        b.Property(c => c.Cuit)
            .HasConversion<string?>(
                cuit => cuit == null ? null : cuit.Valor,
                s    => s == null ? null : new Cuit(s))
            .HasMaxLength(11); // nullable

        b.Property(c => c.Email)
            .HasConversion<string?>(
                email => email == null ? null : email.Valor,
                s     => s == null ? null : new Email(s))
            .HasMaxLength(320); // nullable

        b.Property(c => c.Activo);

        b.Property(c => c.FechaNacimiento); // DateOnly? → nullable date column

        b.HasIndex(c => c.Cuit)
            .IsUnique()
            .HasFilter("[Cuit] IS NOT NULL");

        b.OwnsMany(c => c.Direcciones, d =>
        {
            d.ToTable("ClienteDirecciones");
            d.WithOwner().HasForeignKey("ClienteId");
            d.HasKey("Id");
            d.Property<Guid>("Id").ValueGeneratedNever();
            d.Property(x => x.Calle).IsRequired();
            d.Property(x => x.Numero).IsRequired();
            d.Property(x => x.Piso);          // nullable
            d.Property(x => x.Departamento);  // nullable
            d.Property(x => x.Ciudad).IsRequired();
            d.Property(x => x.Provincia).IsRequired();
            d.Property(x => x.CodigoPostal).IsRequired();
        });

        b.Navigation(c => c.Direcciones)
            .UsePropertyAccessMode(PropertyAccessMode.Field); // backing field: _direcciones
    }
}
