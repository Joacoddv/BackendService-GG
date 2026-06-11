using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Tests;

/// <summary>
/// Covers REQ-07 (DireccionEntrega frozen VO snapshot) structural equality and
/// guard clauses.
/// </summary>
public class DireccionEntregaTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var dir = new DireccionEntrega("Corrientes", "1234", "CABA", "Buenos Aires", "C1043");

        dir.Calle.Should().Be("Corrientes");
        dir.Numero.Should().Be("1234");
        dir.Ciudad.Should().Be("CABA");
        dir.Provincia.Should().Be("Buenos Aires");
        dir.CodigoPostal.Should().Be("C1043");
        dir.Piso.Should().BeNull();
        dir.Departamento.Should().BeNull();
    }

    [Fact]
    public void Create_WithOptionalFields_Succeeds()
    {
        var dir = new DireccionEntrega("Lavalle", "500", "CABA", "Buenos Aires", "C1047", "3", "A");

        dir.Piso.Should().Be("3");
        dir.Departamento.Should().Be("A");
    }

    [Theory]
    [InlineData("", "1", "Ciudad", "Prov", "1234", "*Calle*")]
    [InlineData("Calle", "", "Ciudad", "Prov", "1234", "*Numero*")]
    [InlineData("Calle", "1", "", "Prov", "1234", "*Ciudad*")]
    [InlineData("Calle", "1", "Ciudad", "", "1234", "*Provincia*")]
    [InlineData("Calle", "1", "Ciudad", "Prov", "", "*CodigoPostal*")]
    public void Create_WithEmptyRequiredField_ThrowsDomainException(
        string calle, string numero, string ciudad, string provincia, string cp, string errorMatch)
    {
        var act = () => new DireccionEntrega(calle, numero, ciudad, provincia, cp);
        act.Should().Throw<DomainException>().WithMessage(errorMatch);
    }

    [Fact]
    public void TwoIdenticalInstances_AreEqual()
    {
        var a = new DireccionEntrega("Florida", "200", "CABA", "Buenos Aires", "C1005");
        var b = new DireccionEntrega("Florida", "200", "CABA", "Buenos Aires", "C1005");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void DifferentAddresses_AreNotEqual()
    {
        var a = new DireccionEntrega("Lavalle", "100", "CABA", "Buenos Aires", "C1047");
        var b = new DireccionEntrega("Florida", "100", "CABA", "Buenos Aires", "C1005");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ToString_IncludesMainFields()
    {
        var dir = new DireccionEntrega("Corrientes", "1234", "CABA", "Buenos Aires", "C1043");

        var str = dir.ToString();

        str.Should().Contain("Corrientes");
        str.Should().Contain("CABA");
    }
}
