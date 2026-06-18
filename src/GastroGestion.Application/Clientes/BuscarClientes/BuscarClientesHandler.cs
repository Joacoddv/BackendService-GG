using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Clientes;

namespace GastroGestion.Application.Clientes.BuscarClientes;

public sealed class BuscarClientesHandler
{
    private readonly IClienteRepository _clientes;

    public BuscarClientesHandler(IClienteRepository clientes)
        => _clientes = clientes;

    public Task<IReadOnlyList<Cliente>> Handle(BuscarClientesQuery query, CancellationToken ct = default)
        => _clientes.SearchAsync(query.Nombre, query.IncluirInactivos, ct);
}
