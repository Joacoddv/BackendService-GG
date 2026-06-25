using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Bitacora.GetBitacora;

/// <summary>
/// Flat read model projected from a <see cref="Domain.Bitacora.BitacoraEntry"/>.
/// </summary>
public sealed record BitacoraEntryReadModel(
    Guid         Id,
    Guid         UsuarioId,
    string       Email,
    RolUsuario?  Rol,
    string       Accion,
    string?      Detalle,
    int          ResultadoHttp,
    DateTime     FechaUtc);
