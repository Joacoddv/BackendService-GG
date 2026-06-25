namespace GastroGestion.Application.Stock.GetProducibles;

/// <summary>Query that returns the maximum producible quantity for every active dish.</summary>
public sealed record GetProduciblesQuery;

/// <summary>Per-dish result row from <see cref="GetProduciblesQuery"/>.</summary>
public sealed record PlatoProducibleResult(
    Guid PlatoId,
    string Nombre,
    int MaxProducible);
