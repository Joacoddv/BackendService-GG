using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Bitacora.GetBitacora;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Bitacora;

/// <summary>
/// EF Core implementation of <see cref="IBitacoraRepository"/>.
/// </summary>
internal sealed class BitacoraRepository : IBitacoraRepository
{
    private readonly GastroGestionDbContext _db;

    public BitacoraRepository(GastroGestionDbContext db) => _db = db;

    public async Task<GetBitacoraResult> GetPagedAsync(GetBitacoraQuery query, CancellationToken ct = default)
    {
        var q = _db.BitacoraEntries.AsNoTracking();

        if (query.Desde.HasValue)
            q = q.Where(e => e.FechaUtc >= query.Desde.Value);

        if (query.Hasta.HasValue)
            q = q.Where(e => e.FechaUtc <= query.Hasta.Value);

        if (query.UsuarioId.HasValue)
            q = q.Where(e => e.UsuarioId == query.UsuarioId.Value);

        var totalCount = await q.CountAsync(ct);

        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var items = await q
            .OrderByDescending(e => e.FechaUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new BitacoraEntryReadModel(
                e.Id,
                e.UsuarioId,
                e.Email,
                e.Rol,
                e.Accion,
                e.Detalle,
                e.ResultadoHttp,
                e.FechaUtc))
            .ToListAsync(ct);

        return new GetBitacoraResult(items, totalCount, page, pageSize);
    }
}
