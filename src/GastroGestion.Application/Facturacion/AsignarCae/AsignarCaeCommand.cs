namespace GastroGestion.Application.Facturacion.AsignarCae;

public sealed record AsignarCaeCommand(
    Guid     FacturaId,
    string   Cae,
    DateOnly Vencimiento);
