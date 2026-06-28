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
    string? Departamento,
    string? Zona = null);

public sealed record AgregarLineaRequest(
    Guid PlatoId,
    int Cantidad,
    string? Observaciones);

/// <summary>Request body for PUT /pedidos/{id}/lineas/{lineaId} — edit quantity/notes.</summary>
public sealed record ActualizarLineaRequest(
    int Cantidad,
    string? Observaciones);

/// <summary>
/// PHASE-5: Rol is sourced from the JWT ClaimTypes.Role claim, not the request body.
/// </summary>
public sealed record TransicionarEstadoRequest(EstadoPedido EstadoNuevo);
