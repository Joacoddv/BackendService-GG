using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Facturacion.Events;

/// <summary>
/// Raised when a <see cref="GastroGestion.Domain.Facturacion.Factura"/> of type
/// <c>FacturaElectronica</c> is created, signalling that the AFIP/ARCA web service
/// must be called to obtain a CAE before the invoice can be delivered to the client.
/// <para>
/// This is the AFIP integration seam (design §5e, REQ-15 Scenario 15-F).
/// The Application layer subscribes to this event and orchestrates the AFIP call,
/// then calls <c>Factura.AsignarCae</c> with the result.
/// </para>
/// </summary>
public sealed record FacturaNecesitaCAE(
    Guid FacturaId,
    Guid ClienteId,
    Dinero Total,
    DateTime OccurredOnUtc) : IDomainEvent;
