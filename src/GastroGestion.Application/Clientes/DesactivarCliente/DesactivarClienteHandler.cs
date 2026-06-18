using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Clientes.DesactivarCliente;

public sealed class DesactivarClienteHandler
{
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork        _uow;

    public DesactivarClienteHandler(IClienteRepository clientes, IUnitOfWork uow)
    {
        _clientes = clientes;
        _uow      = uow;
    }

    public async Task Handle(DesactivarClienteCommand cmd, CancellationToken ct = default)
    {
        var cliente = await _clientes.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Cliente '{cmd.Id}' was not found.");

        // Desactivar() is idempotent — calling on an already-inactive client is a no-op.
        cliente.Desactivar();

        await _uow.SaveChangesAsync(ct);
    }
}
