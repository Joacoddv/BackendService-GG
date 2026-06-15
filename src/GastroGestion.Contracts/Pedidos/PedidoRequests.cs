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
/// PHASE-5 seam: Rol is supplied from the request body.
/// In Phase 5, this will be replaced by a JWT claim.
/// </summary>
public sealed record TransicionarEstadoRequest(
    EstadoPedido EstadoNuevo,
    RolUsuario Rol);
