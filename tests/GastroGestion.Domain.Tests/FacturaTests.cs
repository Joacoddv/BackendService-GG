using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.Facturacion.Events;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Tests;

/// <summary>
/// Covers spec REQ-13 (Scenarios 13-A through 13-K) and REQ-15 (Scenario 15-F):
/// Factura TPH creation invariants, CAE guard, multi-payment, Pagada threshold,
/// computed totals, cancel guard, and same-client grouping seam.
/// </summary>
public class FacturaTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly Guid ClienteId    = Guid.NewGuid();
    private static readonly Guid OtroClienteId = Guid.NewGuid();

    private static List<Guid> UnPedido() => [Guid.NewGuid()];

    private static List<FacturaLinea> LineasBasicas() =>
    [
        new FacturaLinea(Guid.NewGuid(), Guid.NewGuid(), new Dinero(100m), PorcentajeIVA.Cero, 2)
    ];

    private static List<FacturaLinea> LineasConIVA() =>
    [
        new FacturaLinea(Guid.NewGuid(), Guid.NewGuid(), new Dinero(100m), new PorcentajeIVA(AlicuotaIVA.General), 2),
        new FacturaLinea(Guid.NewGuid(), Guid.NewGuid(), new Dinero(200m), new PorcentajeIVA(AlicuotaIVA.ReducidoA), 1)
    ];

    // ── CrearTicket ───────────────────────────────────────────────────────────

    [Fact]
    public void CrearTicket_SetsTicketInternoTipo()
    {
        var factura = Factura.CrearTicket(ClienteId, UnPedido(), LineasBasicas());

        factura.TipoComprobante.Should().Be(TipoComprobante.TicketInterno);
        factura.Estado.Should().Be(EstadoFactura.Creada);
        factura.CAE.Should().BeNull();
    }

    [Fact]
    public void CrearTicket_ForcesIVACero_OnAllLines()
    {
        // REQ-13: TicketInterno forces IVA = Cero on all lines regardless of input
        var lineasConIVA = LineasConIVA();
        var factura = Factura.CrearTicket(ClienteId, UnPedido(), lineasConIVA);

        factura.Lineas.Should().AllSatisfy(l =>
            l.IVA.Alicuota.Should().Be(AlicuotaIVA.Exento));
    }

    // ── CrearFacturaElectronica ───────────────────────────────────────────────

    [Fact]
    public void CrearFacturaElectronica_RaisesFacturaNecesitaCAEEvent()
    {
        // REQ-15 Scenario 15-F / REQ-13 Scenario 13-B
        var factura = Factura.CrearFacturaElectronica(ClienteId, UnPedido(), LineasConIVA());

        var evento = factura.DomainEvents
            .OfType<FacturaNecesitaCAE>()
            .SingleOrDefault();

        evento.Should().NotBeNull();
        evento!.FacturaId.Should().Be(factura.Id);
        evento.ClienteId.Should().Be(ClienteId);
    }

    // ── AsignarCae ────────────────────────────────────────────────────────────

    [Fact]
    public void AsignarCae_OnTicket_ThrowsDomainException()
    {
        // REQ-13 Scenario 13-A
        var factura = Factura.CrearTicket(ClienteId, UnPedido(), LineasBasicas());

        var act = () => factura.AsignarCae("12345678901234", new DateOnly(2027, 1, 1));

        act.Should().Throw<DomainException>()
           .WithMessage("*TicketInterno*");
    }

    [Fact]
    public void AsignarCae_SetOnce_ThrowsOnSecondCall()
    {
        var factura = Factura.CrearFacturaElectronica(ClienteId, UnPedido(), LineasConIVA());
        factura.AsignarCae("12345678901234", new DateOnly(2027, 1, 1));

        var act = () => factura.AsignarCae("99999999999999", new DateOnly(2027, 6, 1));

        act.Should().Throw<DomainException>()
           .WithMessage("*already assigned*");
    }

    [Fact]
    public void AsignarCae_OnElectronica_SetsProperties()
    {
        var factura      = Factura.CrearFacturaElectronica(ClienteId, UnPedido(), LineasConIVA());
        var cae          = "12345678901234";
        var vencimiento  = new DateOnly(2027, 1, 1);

        factura.AsignarCae(cae, vencimiento);

        factura.CAE.Should().Be(cae);
        factura.VencimientoCAE.Should().Be(vencimiento);
    }

    // ── RegistrarPago + EstaPagada ────────────────────────────────────────────

    [Fact]
    public void EstaPagada_WhenTotalPagadoGteTotal_ReturnsTrue()
    {
        // REQ-13 Scenario 13-C: two payments covering Total → Pagada
        var lineas  = new List<FacturaLinea>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), new Dinero(500m), PorcentajeIVA.Cero, 1),
            new(Guid.NewGuid(), Guid.NewGuid(), new Dinero(500m), PorcentajeIVA.Cero, 1)
        };
        var factura = Factura.CrearTicket(ClienteId, UnPedido(), lineas);
        // Total = 1000m (no IVA on ticket)

        factura.RegistrarPago(new Dinero(600m), MetodoPago.Efectivo, DateTime.UtcNow);
        factura.RegistrarPago(new Dinero(400m), MetodoPago.TarjetaCredito, DateTime.UtcNow);

        factura.EstaPagada.Should().BeTrue();
        factura.Estado.Should().Be(EstadoFactura.Pagada);
    }

    [Fact]
    public void RegistrarPago_WhenCancelada_ThrowsDomainException()
    {
        // REQ-13: payment on cancelled invoice is rejected
        var factura = Factura.CrearTicket(ClienteId, UnPedido(), LineasBasicas());
        factura.Cancelar();

        var act = () => factura.RegistrarPago(new Dinero(100m), MetodoPago.Efectivo, DateTime.UtcNow);

        act.Should().Throw<DomainException>();
    }

    // ── Cancelar ─────────────────────────────────────────────────────────────

    [Fact]
    public void Cancelar_WhenPagada_ThrowsDomainException()
    {
        // REQ-13 Scenario 13-E
        var lineas  = new List<FacturaLinea>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), new Dinero(100m), PorcentajeIVA.Cero, 1)
        };
        var factura = Factura.CrearTicket(ClienteId, UnPedido(), lineas);
        factura.RegistrarPago(new Dinero(100m), MetodoPago.Efectivo, DateTime.UtcNow);

        var act = () => factura.Cancelar();

        act.Should().Throw<DomainException>()
           .WithMessage("*paid*");
    }

    // ── Computed totals ───────────────────────────────────────────────────────

    [Fact]
    public void Totales_ComputedCorrectly_FromLineSnapshots()
    {
        // REQ-13 Scenario 13-F:
        // Line 1: 100 ARS × 2 qty = 200 net; IVA 21% = 42m
        // Line 2: 200 ARS × 1 qty = 200 net; IVA 10.5% = 21m
        // SubTotal = 400m, TotalIVA = 63m, Total = 463m
        var lineas = LineasConIVA();
        var factura = Factura.CrearFacturaConIVA(ClienteId, UnPedido(), lineas);

        factura.SubTotal.Monto.Should().Be(400m);
        factura.TotalIVA.Monto.Should().Be(63m);
        factura.Total.Monto.Should().Be(463m);
    }

    // ── PuedeCombinarse ───────────────────────────────────────────────────────

    [Fact]
    public void PuedeCombinarse_SameClientAndTipo_ReturnsTrue()
    {
        var f1 = Factura.CrearTicket(ClienteId, UnPedido(), LineasBasicas());
        var f2 = Factura.CrearTicket(ClienteId, UnPedido(), LineasBasicas());

        f1.PuedeCombinarseConFactura(f2).Should().BeTrue();
    }

    [Fact]
    public void PuedeCombinarse_DifferentCliente_ReturnsFalse()
    {
        var f1 = Factura.CrearTicket(ClienteId,     UnPedido(), LineasBasicas());
        var f2 = Factura.CrearTicket(OtroClienteId, UnPedido(), LineasBasicas());

        f1.PuedeCombinarseConFactura(f2).Should().BeFalse();
    }

    [Fact]
    public void PuedeCombinarse_DifferentTipo_ReturnsFalse()
    {
        var f1 = Factura.CrearTicket(ClienteId, UnPedido(), LineasBasicas());
        var f2 = Factura.CrearFacturaConIVA(ClienteId, UnPedido(), LineasConIVA());

        f1.PuedeCombinarseConFactura(f2).Should().BeFalse();
    }
}
