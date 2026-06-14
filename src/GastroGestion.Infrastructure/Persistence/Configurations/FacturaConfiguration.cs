using System.Text.Json;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GastroGestion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Flat single-table mapping for Factura.
/// TipoComprobante is a plain discriminator column (int) — NOT EF inheritance.
/// PedidosFacturados is a JSON primitive collection.
/// Full Slice C mapping; included here so the EF model builds across all slices.
/// </summary>
internal sealed class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> b)
    {
        b.ToTable("Facturas");

        b.HasKey(f => f.Id);
        b.Property(f => f.Id).ValueGeneratedNever();

        b.Property(f => f.TipoComprobante).HasConversion<int>(); // discriminator COLUMN — NOT EF inheritance
        b.Property(f => f.Estado).HasConversion<int>();           // EstadoFactura enum
        b.Property(f => f.ClienteId);
        b.Property(f => f.FechaAlta);
        b.Property(f => f.CAE).HasMaxLength(14);   // nullable — FacturaElectronica only
        b.Property(f => f.VencimientoCAE);          // DateOnly? → nullable date

        // Computed totals — MUST be Ignored
        b.Ignore(f => f.SubTotal);
        b.Ignore(f => f.TotalIVA);
        b.Ignore(f => f.Total);
        b.Ignore(f => f.TotalPagado);
        b.Ignore(f => f.EstaPagada);

        // PedidosFacturados: backing field _pedidosFacturados → JSON nvarchar(max) column
        // via value converter. EF 8 PrimitiveCollection + ToJson() requires OwnedNavigation API;
        // for a List<Guid> we use a ValueConverter to store a JSON array string.
        b.Property(f => f.PedidosFacturados)
            .HasField("_pedidosFacturados")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                json => (IReadOnlyList<Guid>)(JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null) ?? new List<Guid>()))
            .HasColumnName("PedidosFacturados")
            .HasColumnType("nvarchar(max)");

        // FacturaLinea owned collection
        b.OwnsMany(f => f.Lineas, l =>
        {
            l.ToTable("FacturaLineas");
            l.WithOwner().HasForeignKey("FacturaId");
            l.HasKey("Id");
            l.Property<Guid>("Id").ValueGeneratedNever();
            l.Property(x => x.LineaPedidoId);
            l.Property(x => x.Cantidad);
            l.OwnsOne(x => x.PrecioUnitario, m =>
            {
                m.Property(d => d.Monto).HasColumnName("PrecioUnitario_Monto").HasColumnType("decimal(18,2)");
                m.Property(d => d.Moneda).HasColumnName("PrecioUnitario_Moneda").HasConversion<int>();
            });
            l.Navigation(x => x.PrecioUnitario).IsRequired();
            l.Property(x => x.IVA)
                .HasConversion(PorcentajeIvaConverter.Instance)
                .HasColumnName("IVA_Alicuota");
            l.Ignore(x => x.Subtotal);
            l.Ignore(x => x.SubtotalConIVA);
        });
        b.Navigation(f => f.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Pago owned collection
        b.OwnsMany(f => f.Pagos, p =>
        {
            p.ToTable("FacturaPagos");
            p.WithOwner().HasForeignKey("FacturaId");
            p.HasKey("Id");
            p.Property<Guid>("Id").ValueGeneratedNever();
            p.Property(x => x.MetodoPago).HasConversion<int>(); // MetodoPago enum
            p.Property(x => x.FechaPago);
            p.OwnsOne(x => x.Monto, m =>
            {
                m.Property(d => d.Monto).HasColumnName("Monto_Monto").HasColumnType("decimal(18,2)");
                m.Property(d => d.Moneda).HasColumnName("Monto_Moneda").HasConversion<int>();
            });
            p.Navigation(x => x.Monto).IsRequired();
        });
        b.Navigation(f => f.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
