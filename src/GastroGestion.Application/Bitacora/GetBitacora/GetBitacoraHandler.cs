using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Application.Bitacora.GetBitacora;

/// <summary>
/// Retrieves a paginated, newest-first slice of audit log entries with optional filters.
/// </summary>
public sealed class GetBitacoraHandler
{
    private readonly IBitacoraRepository _repository;

    public GetBitacoraHandler(IBitacoraRepository repository) => _repository = repository;

    public Task<GetBitacoraResult> Handle(GetBitacoraQuery query, CancellationToken ct = default)
        => _repository.GetPagedAsync(query, ct);
}
