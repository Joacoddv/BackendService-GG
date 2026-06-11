using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Tests;

public class ValueObjectTests
{
    // ─── Dinero ──────────────────────────────────────────────────────────────

    [Fact]
    public void Dinero_WithNegativeMonto_ThrowsDomainException()
    {
        var act = () => new Dinero(-1m);
        act.Should().Throw<DomainException>()
           .WithMessage("*negative*");
    }

    [Fact]
    public void Dinero_WithZeroMonto_IsAllowed()
    {
        var dinero = new Dinero(0m);
        dinero.Monto.Should().Be(0m);
    }

    [Fact]
    public void Dinero_Sumar_SameMoneda_ReturnsSum()
    {
        // Happy-path: two ARS amounts sum correctly.
        // Note: the cross-currency guard (mixed Moneda → DomainException) exists in
        // Dinero.GuardarMoneda but cannot be exercised until a second Moneda value is added.
        var a = new Dinero(100m, Moneda.ARS);
        var b = new Dinero(50m, Moneda.ARS);
        var result = a.Sumar(b);
        result.Monto.Should().Be(150m);
    }

    [Fact]
    public void Dinero_ConIVA_AppliesCorrectly()
    {
        // 100 ARS + 21% IVA = 121 ARS
        var precioBase = new Dinero(100m);
        var iva = new PorcentajeIVA(AlicuotaIVA.General);

        var conIva = precioBase.ConIVA(iva);

        conIva.Monto.Should().Be(121m);
        conIva.Moneda.Should().Be(Moneda.ARS);
    }

    [Fact]
    public void Dinero_AplicarIVA_ReturnsOnlyIVAAmount()
    {
        var precio = new Dinero(200m);
        var iva = new PorcentajeIVA(AlicuotaIVA.ReducidoA); // 10.5%

        var soloIva = precio.AplicarIVA(iva);

        soloIva.Monto.Should().Be(21m); // 200 * 0.105
    }

    [Fact]
    public void Dinero_Restar_BelowZero_ThrowsDomainException()
    {
        var a = new Dinero(50m);
        var b = new Dinero(100m);
        var act = () => a.Restar(b);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Dinero_Multiplicar_NegativeFactor_ThrowsDomainException()
    {
        var dinero = new Dinero(100m);
        var act = () => dinero.Multiplicar(-2m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Dinero_Equality_SameMontoAndMoneda_AreEqual()
    {
        var a = new Dinero(100m, Moneda.ARS);
        var b = new Dinero(100m, Moneda.ARS);
        a.Should().Be(b);
    }

    // ─── Cuit ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Cuit_InvalidFormat_ThrowsDomainException()
    {
        var act = () => new Cuit("12345");
        act.Should().Throw<DomainException>()
           .WithMessage("*11 digits*");
    }

    [Fact]
    public void Cuit_ValidCuit_FormatsCorrectly()
    {
        // 20-12345678-6 has a valid AFIP check digit (sum=148, rem=5, expected=6)
        var cuit = new Cuit("20123456786");
        // Format: ##-########-#
        cuit.ToString().Should().Be("20-12345678-6");
    }

    [Fact]
    public void Cuit_InvalidCheckDigit_ThrowsDomainException()
    {
        // 20-12345678-0 has an invalid check digit (valid is 6)
        var act = () => new Cuit("20123456780");
        act.Should().Throw<DomainException>()
           .WithMessage("*check digit*");
    }

    [Fact]
    public void Cuit_WithHyphens_IsAccepted()
    {
        // Should strip hyphens and validate — 20-12345678-6 is valid
        var cuit = new Cuit("20-12345678-6");
        cuit.Valor.Should().Be("20123456786");
    }

    [Fact]
    public void Cuit_WhereExpectedCheckDigitIsTen_IsRejected()
    {
        // AFIP rule: when the algorithm produces expected == 10, no valid check digit
        // exists and the CUIT is structurally invalid. Fixture "30693450230":
        // weights=[5,4,3,2,7,6,5,4,3,2], sum=133, 133%11=1, expected=10 → rejected.
        var act = () => new Cuit("30693450230");
        act.Should().Throw<DomainException>()
           .WithMessage("*check digit*");
    }

    // ─── Email ────────────────────────────────────────────────────────────────

    [Fact]
    public void Email_InvalidFormat_NoAtSign_ThrowsDomainException()
    {
        var act = () => new Email("notanemail");
        act.Should().Throw<DomainException>()
           .WithMessage("*invalid*");
    }

    [Fact]
    public void Email_ValidFormat_NormalizesToLowercase()
    {
        var email = new Email("User@EXAMPLE.COM");
        email.Valor.Should().Be("user@example.com");
    }

    [Fact]
    public void Email_EmptyString_ThrowsDomainException()
    {
        var act = () => new Email("");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Email_MissingDomain_ThrowsDomainException()
    {
        var act = () => new Email("user@");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Email_DoubleAtSign_ThrowsDomainException()
    {
        // "a@b@c.com" must be rejected — exactly one '@' is required.
        var act = () => new Email("a@b@c.com");
        act.Should().Throw<DomainException>()
           .WithMessage("*invalid*");
    }

    // ─── Cantidad ─────────────────────────────────────────────────────────────

    [Fact]
    public void Cantidad_ZeroValor_ThrowsDomainException()
    {
        var act = () => new Cantidad(0m, UnidadDeMedida.Gramo);
        act.Should().Throw<DomainException>()
           .WithMessage("*greater than zero*");
    }

    [Fact]
    public void Cantidad_NegativeValor_ThrowsDomainException()
    {
        var act = () => new Cantidad(-1m, UnidadDeMedida.Kilogramo);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cantidad_Sumar_DifferentUnits_ThrowsDomainException()
    {
        var gramos = new Cantidad(100m, UnidadDeMedida.Gramo);
        var kilos  = new Cantidad(1m,   UnidadDeMedida.Kilogramo);
        var act = () => gramos.Sumar(kilos);
        act.Should().Throw<DomainException>()
           .WithMessage("*mix units*");
    }

    [Fact]
    public void Cantidad_Sumar_SameUnits_ReturnsSum()
    {
        var a = new Cantidad(100m, UnidadDeMedida.Gramo);
        var b = new Cantidad(200m, UnidadDeMedida.Gramo);
        var result = a.Sumar(b);
        result.Valor.Should().Be(300m);
        result.Unidad.Should().Be(UnidadDeMedida.Gramo);
    }

    [Fact]
    public void Cantidad_Restar_EqualAmounts_ThrowsDomainException()
    {
        // Cantidad.Restar uses `result <= 0`, so subtracting equal values (result = 0)
        // throws — result must be strictly positive (spec: Cantidad.Valor > 0 invariant).
        var a = new Cantidad(5m, UnidadDeMedida.Gramo);
        var b = new Cantidad(5m, UnidadDeMedida.Gramo);
        var act = () => a.Restar(b);
        act.Should().Throw<DomainException>();
    }

    // ─── PorcentajeIVA ────────────────────────────────────────────────────────

    [Fact]
    public void PorcentajeIVA_Cero_ReturnsZeroTasa()
    {
        var pct = PorcentajeIVA.Cero;
        pct.Tasa.Should().Be(0m);
        pct.Alicuota.Should().Be(AlicuotaIVA.Exento);
    }

    [Fact]
    public void PorcentajeIVA_General_Returns21Percent()
    {
        var pct = new PorcentajeIVA(AlicuotaIVA.General);
        pct.Tasa.Should().Be(0.21m);
    }

    [Fact]
    public void PorcentajeIVA_ReducidoA_Returns10Point5Percent()
    {
        var pct = new PorcentajeIVA(AlicuotaIVA.ReducidoA);
        pct.Tasa.Should().Be(0.105m);
    }

    [Fact]
    public void PorcentajeIVA_Diferencial_Returns27Percent()
    {
        var pct = new PorcentajeIVA(AlicuotaIVA.Diferencial);
        pct.Tasa.Should().Be(0.27m);
    }

    [Fact]
    public void PorcentajeIVA_Equality_SameAlicuota_AreEqual()
    {
        var a = new PorcentajeIVA(AlicuotaIVA.General);
        var b = new PorcentajeIVA(AlicuotaIVA.General);
        a.Should().Be(b);
    }
}
