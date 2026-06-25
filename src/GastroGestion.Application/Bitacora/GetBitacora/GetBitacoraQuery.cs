namespace GastroGestion.Application.Bitacora.GetBitacora;

/// <summary>
/// Query parameters for fetching paginated audit log entries.
/// </summary>
public sealed record GetBitacoraQuery(
    DateTime? Desde    = null,
    DateTime? Hasta    = null,
    Guid?     UsuarioId = null,
    int       Page     = 1,
    int       PageSize = 50);
