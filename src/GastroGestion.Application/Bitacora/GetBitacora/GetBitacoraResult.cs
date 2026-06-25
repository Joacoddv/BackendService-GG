namespace GastroGestion.Application.Bitacora.GetBitacora;

/// <summary>
/// Paged result returned by <see cref="GetBitacoraHandler"/>.
/// </summary>
public sealed record GetBitacoraResult(
    IReadOnlyList<BitacoraEntryReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);
