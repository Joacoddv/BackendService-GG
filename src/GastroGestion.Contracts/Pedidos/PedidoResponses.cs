using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Pedidos;

public sealed record PedidoResponse(
    Guid Id,
    TipoPedido Tipo,
    EstadoPedido Estado,
    Guid? MesaId,
    Guid? ClienteId,
    DireccionEntregaResponse? DireccionEntrega,
    DateTime CreadoEnUtc,
    IReadOnlyList<LineaPedidoResponse> Lineas);

public sealed record DireccionEntregaResponse(
    string Calle,
    string Numero,
    string Ciudad,
    string Provincia,
    string CodigoPostal,
    string? Piso,
    string? Departamento);

public sealed record LineaPedidoResponse(
    Guid Id,
    Guid PlatoId,
    int Cantidad,
    string? Observaciones,
    decimal? PrecioUnitario,
    string? Moneda,
    decimal? IvaTasa,
    decimal? SubtotalLinea,
    decimal? TotalLinea);
