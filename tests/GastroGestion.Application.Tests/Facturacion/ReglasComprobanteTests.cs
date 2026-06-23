using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Tests.Facturacion;

/// <summary>
/// Unit tests for ReglasComprobante.EsPermitido.
/// Validates the CondicionIVA → TipoComprobanteSolicitado permission matrix.
/// </summary>
public sealed class ReglasComprobanteTests
{
    // ── ConsumidorFinal: only TicketInterno allowed ───────────────────────────

    [Fact]
    public void ConsumidorFinal_TicketInterno_IsPermitido()
        => Assert.True(ReglasComprobante.EsPermitido(CondicionIVA.ConsumidorFinal, TipoComprobanteSolicitado.TicketInterno));

    [Fact]
    public void ConsumidorFinal_FacturaConIVA_NotPermitido()
        => Assert.False(ReglasComprobante.EsPermitido(CondicionIVA.ConsumidorFinal, TipoComprobanteSolicitado.FacturaConIVA));

    [Fact]
    public void ConsumidorFinal_FacturaElectronica_NotPermitido()
        => Assert.False(ReglasComprobante.EsPermitido(CondicionIVA.ConsumidorFinal, TipoComprobanteSolicitado.FacturaElectronica));

    // ── ResponsableInscripto: all types allowed ───────────────────────────────

    [Theory]
    [InlineData(TipoComprobanteSolicitado.TicketInterno)]
    [InlineData(TipoComprobanteSolicitado.FacturaConIVA)]
    [InlineData(TipoComprobanteSolicitado.FacturaElectronica)]
    public void ResponsableInscripto_AllTypes_Permitido(TipoComprobanteSolicitado tipo)
        => Assert.True(ReglasComprobante.EsPermitido(CondicionIVA.ResponsableInscripto, tipo));

    // ── Monotributista: all types allowed ────────────────────────────────────

    [Theory]
    [InlineData(TipoComprobanteSolicitado.TicketInterno)]
    [InlineData(TipoComprobanteSolicitado.FacturaConIVA)]
    [InlineData(TipoComprobanteSolicitado.FacturaElectronica)]
    public void Monotributista_AllTypes_Permitido(TipoComprobanteSolicitado tipo)
        => Assert.True(ReglasComprobante.EsPermitido(CondicionIVA.Monotributista, tipo));

    // ── ExentoIVA: all types allowed ─────────────────────────────────────────

    [Theory]
    [InlineData(TipoComprobanteSolicitado.TicketInterno)]
    [InlineData(TipoComprobanteSolicitado.FacturaConIVA)]
    [InlineData(TipoComprobanteSolicitado.FacturaElectronica)]
    public void ExentoIVA_AllTypes_Permitido(TipoComprobanteSolicitado tipo)
        => Assert.True(ReglasComprobante.EsPermitido(CondicionIVA.ExentoIVA, tipo));
}
