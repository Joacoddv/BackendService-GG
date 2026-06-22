using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Facturacion.GetFacturas;

public sealed record GetFacturasQuery(EstadoFactura? Estado, Guid? ClienteId);
