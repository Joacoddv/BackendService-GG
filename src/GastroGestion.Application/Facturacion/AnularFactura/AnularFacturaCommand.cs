namespace GastroGestion.Application.Facturacion.AnularFactura;

public sealed record AnularFacturaCommand(Guid FacturaId, string Motivo);
