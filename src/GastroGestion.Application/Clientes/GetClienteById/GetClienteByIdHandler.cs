using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Clientes;

namespace GastroGestion.Application.Clientes.GetClienteById;

public sealed class GetClienteByIdHandler
{
    private readonly IClienteRepository _clientes;

    public GetClienteByIdHandler(IClienteRepository clientes) => _clientes = clientes;

    public Task<Cliente?> Handle(GetClienteByIdQuery query, CancellationToken ct = default)
        => _clientes.GetByIdAsync(query.Id, ct);
}
