using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Clientes.QuitarDireccion;

public sealed record QuitarDireccionCommand(Guid ClienteId, Guid DireccionId);

public sealed class QuitarDireccionHandler
{
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork        _uow;

    public QuitarDireccionHandler(IClienteRepository clientes, IUnitOfWork uow)
    {
        _clientes = clientes;
        _uow      = uow;
    }

    public async Task Handle(QuitarDireccionCommand cmd, CancellationToken ct = default)
    {
        var cliente = await _clientes.GetByIdAsync(cmd.ClienteId, ct)
            ?? throw new NotFoundException($"Cliente '{cmd.ClienteId}' was not found.");

        cliente.EliminarDireccion(cmd.DireccionId); // idempotent — no-op if not found
        await _uow.SaveChangesAsync(ct);
    }
}
