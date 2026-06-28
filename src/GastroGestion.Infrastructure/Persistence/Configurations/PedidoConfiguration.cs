using System.Text.Json;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Pedido aggregate. Full mapping (Slice B).
/// Owned graph: LineaPedido, OrdenTrabajo with JSON RecetaSnapshot, nullable DireccionEntrega, RowVersion.
/// </summary>
internal sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> b)
    {
        b.ToTable("Pedidos");

        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();

        b.Property(p => p.Tipo).HasConversion<int>();
        b.Property(p => p.Estado).HasConversion<int>();
        b.Property(p => p.MesaId);
        b.Property(p => p.ClienteId);
        b.Property(p => p.CreadoEnUtc);
        b.Property(p => p.RowVersion).IsRowVersion();

        // Nullable DireccionEntrega — flattened VO (Slice B completes this)
        b.OwnsOne(p => p.DireccionEntrega, dir =>
        {
            dir.Property(x => x.Calle).HasColumnName("Entrega_Calle");
            dir.Property(x => x.Numero).HasColumnName("Entrega_Numero");
            dir.Property(x => x.Piso).HasColumnName("Entrega_Piso");
            dir.Property(x => x.Departamento).HasColumnName("Entrega_Departamento");
            dir.Property(x => x.Ciudad).HasColumnName("Entrega_Ciudad");
            dir.Property(x => x.Provincia).HasColumnName("Entrega_Provincia");
            dir.Property(x => x.CodigoPostal).HasColumnName("Entrega_CodigoPostal");
            dir.Property(x => x.Zona).HasColumnName("Entrega_Zona");
        });
        b.Navigation(p => p.DireccionEntrega).IsRequired(false);

        // LineaPedido owned collection
        b.OwnsMany(p => p.Lineas, l =>
        {
            l.ToTable("PedidoLineas");
            l.WithOwner().HasForeignKey("PedidoId");
            l.HasKey("Id");
            l.Property<Guid>("Id").ValueGeneratedNever();
            l.Property(x => x.PlatoId);
            l.Property(x => x.Cantidad);
            l.Property(x => x.Observaciones);

            l.OwnsOne(x => x.PrecioUnitario, m =>
            {
                m.Property(d => d.Monto).HasColumnName("PrecioUnitario_Monto").HasColumnType("decimal(18,2)");
                m.Property(d => d.Moneda).HasColumnName("PrecioUnitario_Moneda").HasConversion<int>();
            });
            l.Navigation(x => x.PrecioUnitario).IsRequired(false);

            l.Property(x => x.IVA)
                .HasConversion<int?>(
                    p => p == null ? (int?)null : (int)p.Alicuota,
                    i => i == null ? null : new PorcentajeIVA((AlicuotaIVA)i))
                .HasColumnName("IVA_Alicuota");

            // Set-once flag — internal bool on LineaPedido (PE-12). Mapped by name because
            // the internal visibility is not accessible to the Infrastructure assembly at
            // compile time. EF Core reflects on non-public members by CLR name at runtime.
            l.Property<bool>("PrecioConfirmado").HasColumnName("PrecioConfirmado");

            // Computed getters must be Ignored
            l.Ignore(x => x.SubtotalLinea);
            l.Ignore(x => x.IVALinea);
            l.Ignore(x => x.TotalLinea);
        });
        b.Navigation(p => p.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field);

        // OrdenTrabajo owned collection with RecetaSnapshot stored as JSON string
        b.OwnsMany(p => p.OrdenesTrabajo, ot =>
        {
            ot.ToTable("PedidoOrdenesTrabajo");
            ot.WithOwner().HasForeignKey("PedidoId");
            ot.HasKey("Id");
            ot.Property<Guid>("Id").ValueGeneratedNever();
            ot.Property(x => x.PlatoId);
            ot.Property(x => x.LineaPedidoId);
            ot.Property(x => x.Estado).HasConversion<int>();

            ot.Property(x => x.CocineroAsignado)
                .HasConversion<Guid?>(
                    l => l == null ? (Guid?)null : l.Valor,
                    g => g == null ? null : new LegajoId(g.Value))
                .HasColumnName("CocineroAsignado");

            // LineaRecetaSnapshot is a sealed record with a nested Cantidad VO.
            // EF cannot bind record positional constructors to owned navigation properties.
            // We use a value converter to serialize the snapshot list to a JSON string column.
            var snapshotConverter = new ValueConverter<IReadOnlyList<LineaRecetaSnapshot>, string>(
                list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                json => (IReadOnlyList<LineaRecetaSnapshot>)(
                    JsonSerializer.Deserialize<List<LineaRecetaSnapshot>>(json, (JsonSerializerOptions?)null)
                    ?? new List<LineaRecetaSnapshot>()));

            ot.Property(x => x.RecetaSnapshot)
                .HasConversion(snapshotConverter)
                .HasColumnName("RecetaSnapshot")
                .HasColumnType("nvarchar(max)");
        });
        b.Navigation(p => p.OrdenesTrabajo).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
