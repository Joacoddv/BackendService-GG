namespace GastroGestion.Contracts.Pedidos;

public sealed record AsignarCocineroRequest(Guid CocineroLegajoId);
// GenerarOrdenesTrabajo and MarcarLista take no body — route params only.
