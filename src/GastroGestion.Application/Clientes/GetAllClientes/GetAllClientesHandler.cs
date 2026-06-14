using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Clientes;

namespace GastroGestion.Application.Clientes.GetAllClientes;

public sealed class GetAllClientesHandler
{
    private readonly IClienteRepository _clientes;

    public GetAllClientesHandler(IClienteRepository clientes) => _clientes = clientes;

    public Task<IReadOnlyList<Cliente>> Handle(GetAllClientesQuery query, CancellationToken ct = default)
        => _clientes.GetAllAsync(ct);
}
