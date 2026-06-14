using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Facturacion;

/// <summary>
/// An individual payment registered against a <see cref="Factura"/>.
/// Owned by the Factura aggregate; never loaded independently.
/// Multi-payment is supported: a Factura accumulates N Pago records until
/// the total paid amount reaches or exceeds the invoice total.
/// </summary>
public class Pago : Entity
{
    /// <summary>Payment amount. Must be greater than zero.</summary>
    public Dinero Monto { get; private set; }

    /// <summary>Payment method used for this transaction.</summary>
    public MetodoPago MetodoPago { get; private set; }

    /// <summary>UTC timestamp when this payment was recorded.</summary>
    public DateTime FechaPago { get; private set; }

    // EF Core parameterless ctor.
#pragma warning disable CS8618
    protected Pago() { }
#pragma warning restore CS8618

    internal Pago(Guid id, Dinero monto, MetodoPago metodoPago, DateTime fechaPago) : base(id)
    {
        if (monto is null)
            throw new DomainException("Pago.Monto cannot be null.");
        if (monto.Monto <= 0)
            throw new DomainException($"Pago.Monto must be greater than zero. Received: {monto.Monto}.");

        Monto      = monto;
        MetodoPago = metodoPago;
        FechaPago  = fechaPago;
    }
}
