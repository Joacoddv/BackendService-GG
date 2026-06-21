using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Application.Clientes.Cumpleaneros;

/// <summary>Query for the active clientes whose birthday falls in the given month (1-12).</summary>
public sealed record GetCumpleanerosQuery(int Mes);

public sealed record CumpleaneroResult(Guid Id, string Nombre, string? Email, DateOnly FechaNacimiento);

public sealed class GetCumpleanerosHandler
{
    private readonly IClienteRepository _clientes;

    public GetCumpleanerosHandler(IClienteRepository clientes) => _clientes = clientes;

    public async Task<IReadOnlyList<CumpleaneroResult>> Handle(GetCumpleanerosQuery query, CancellationToken ct = default)
    {
        var clientes = await _clientes.SearchAsync(null, incluirInactivos: false, ct);

        return clientes
            .Where(c => c.FechaNacimiento is { } f && f.Month == query.Mes)
            .Select(c => new CumpleaneroResult(c.Id, c.Nombre, c.Email?.Valor, c.FechaNacimiento!.Value))
            .OrderBy(c => c.FechaNacimiento.Day)
            .ToList();
    }
}
