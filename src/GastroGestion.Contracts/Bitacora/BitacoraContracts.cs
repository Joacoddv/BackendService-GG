using GastroGestion.Application.Bitacora.GetBitacora;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Bitacora;

/// <summary>Response record for a single audit log entry.</summary>
public sealed record BitacoraEntryResponse(
    Guid       Id,
    Guid        UsuarioId,
    string      Email,
    RolUsuario? Rol,
    string      Accion,
    string?    Detalle,
    int        ResultadoHttp,
    DateTime   FechaUtc);

/// <summary>Paged response for GET /bitacora.</summary>
public sealed record BitacoraPageResponse(
    IReadOnlyList<BitacoraEntryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public static class BitacoraMapping
{
    public static BitacoraEntryResponse ToResponse(this BitacoraEntryReadModel m)
        => new(m.Id, m.UsuarioId, m.Email, m.Rol, m.Accion, m.Detalle, m.ResultadoHttp, m.FechaUtc);

    public static BitacoraPageResponse ToPageResponse(this GetBitacoraResult result)
        => new(
            result.Items.Select(i => i.ToResponse()).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
}
