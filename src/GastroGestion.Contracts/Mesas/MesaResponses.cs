using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Mesas;

public sealed record MesaResponse(
    Guid Id,
    int Numero,
    int Capacidad,
    EstadoMesa Estado,
    bool Activa,
    Guid? PedidoActivoId);
