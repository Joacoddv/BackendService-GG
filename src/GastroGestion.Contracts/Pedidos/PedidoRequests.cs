using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Pedidos;

public sealed record CrearPedidoRequest(
    TipoPedido Tipo,
    Guid? MesaId,
    Guid? ClienteId,
    DireccionEntregaRequest? DireccionEntrega);

public sealed record DireccionEntregaRequest(
    string Calle,
    string Numero,
    string Ciudad,
    string Provincia,
    string CodigoPostal,
    string? Piso,
    string? Departamento);

public sealed record AgregarLineaRequest(
    Guid PlatoId,
    int Cantidad,
    string? Observaciones);

/// <summary>
/// PHASE-5: Rol is sourced from the JWT ClaimTypes.Role claim, not the request body.
/// </summary>
public sealed record TransicionarEstadoRequest(EstadoPedido EstadoNuevo);
