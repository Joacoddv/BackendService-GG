using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Stock;

namespace GastroGestion.Domain.Tests;

/// <summary>
/// Covers spec REQ-12 (Scenarios 12-A through 12-F): MovimientoStock construction,
/// sign convention, immutability, lot/expiry fields, and Balance projection.
/// </summary>
public class MovimientoStockTests
{
    private static readonly Guid IngredienteId = Guid.NewGuid();

    // ── RegistrarCompra ───────────────────────────────────────────────────────

    [Fact]
    public void RegistrarCompra_CreatesPositiveMovimiento()
    {
        // REQ-12 Scenario 12-A
        var lote        = "L2024-01";
        var vencimiento = new DateOnly(2027, 1, 1);

        var m = MovimientoStock.RegistrarCompra(IngredienteId, 10m, lote, vencimiento);

        m.Cantidad.Should().Be(10m);
        m.Tipo.Should().Be(TipoMovimientoStock.Compra);
        m.Lote.Should().Be(lote);
        m.FechaVencimiento.Should().Be(vencimiento);
        m.IngredienteId.Should().Be(IngredienteId);
        m.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void RegistrarMovimiento_ZeroCantidad_ThrowsDomainException()
    {
        // REQ-12 Scenario 12-C
        var act = () => MovimientoStock.RegistrarMovimiento(IngredienteId, TipoMovimientoStock.Compra, 0m);

        act.Should().Throw<DomainException>()
           .WithMessage("*zero*");
    }

    [Fact]
    public void RegistrarMovimiento_Reserva_StoresNegativeCantidad()
    {
        // REQ-12 Scenario 12-B: Reserva stores negative quantity
        var m = MovimientoStock.RegistrarMovimiento(IngredienteId, TipoMovimientoStock.Reserva, 5m);

        m.Cantidad.Should().Be(-5m);
        m.Tipo.Should().Be(TipoMovimientoStock.Reserva);
    }

    [Fact]
    public void RegistrarMovimiento_Consumo_StoresNegativeCantidad()
    {
        var m = MovimientoStock.RegistrarMovimiento(IngredienteId, TipoMovimientoStock.Consumo, 3m);

        m.Cantidad.Should().Be(-3m);
        m.Tipo.Should().Be(TipoMovimientoStock.Consumo);
    }

    [Fact]
    public void RegistrarMovimiento_LiberacionReserva_StoresPositiveCantidad()
    {
        var m = MovimientoStock.RegistrarMovimiento(IngredienteId, TipoMovimientoStock.LiberacionReserva, 5m);

        m.Cantidad.Should().Be(5m);
    }

    // ── CalcularDisponible ────────────────────────────────────────────────────

    [Fact]
    public void CalcularDisponible_SignedSum_ReturnsCorrectBalance()
    {
        // REQ-12 Scenario 12-D: 20 - 5 - 5 + 5 = 15
        var movimientos = new List<MovimientoStock>
        {
            MovimientoStock.RegistrarCompra(IngredienteId, 20m),
            MovimientoStock.RegistrarMovimiento(IngredienteId, TipoMovimientoStock.Reserva, 5m),
            MovimientoStock.RegistrarMovimiento(IngredienteId, TipoMovimientoStock.Consumo, 5m),
            MovimientoStock.RegistrarMovimiento(IngredienteId, TipoMovimientoStock.LiberacionReserva, 5m)
        };

        var balance = MovimientoStock.CalcularDisponible(movimientos);

        balance.Should().Be(15m);
    }

    [Fact]
    public void CalcularDisponible_Negative_ThrowsDomainException()
    {
        // REQ-12: domain invariant — balance < 0 signals infra violation
        var movimientos = new List<MovimientoStock>
        {
            MovimientoStock.RegistrarCompra(IngredienteId, 5m),
            MovimientoStock.RegistrarMovimiento(IngredienteId, TipoMovimientoStock.Consumo, 10m)
        };

        var act = () => MovimientoStock.CalcularDisponible(movimientos);

        act.Should().Throw<DomainException>();
    }

    // ── Immutability ──────────────────────────────────────────────────────────

    [Fact]
    public void MovimientoStock_IsImmutable_NoPublicSetters()
    {
        // REQ-12 Scenario 12-E: verify no public settable properties via reflection
        var type    = typeof(MovimientoStock);
        var setters = type.GetProperties()
            .Where(p => p.SetMethod is not null && p.SetMethod.IsPublic)
            .Select(p => p.Name)
            .ToList();

        setters.Should().BeEmpty(
            because: "MovimientoStock is append-only and must have no public property setters");
    }

    // ── Lot / expiry seam ─────────────────────────────────────────────────────

    [Fact]
    public void FechaVencimiento_IsNullableOnCompra()
    {
        // REQ-12: seam test — compra without expiry must succeed
        var m = MovimientoStock.RegistrarCompra(IngredienteId, 5m, lote: null, fechaVencimiento: null);

        m.FechaVencimiento.Should().BeNull();
        m.Lote.Should().BeNull();
    }
}
