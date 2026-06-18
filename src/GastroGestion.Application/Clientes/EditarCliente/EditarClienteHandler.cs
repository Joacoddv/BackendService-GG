using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Clientes.EditarCliente;

public sealed class EditarClienteHandler
{
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork        _uow;

    public EditarClienteHandler(IClienteRepository clientes, IUnitOfWork uow)
    {
        _clientes = clientes;
        _uow      = uow;
    }

    public async Task<Cliente> Handle(EditarClienteCommand cmd, CancellationToken ct = default)
    {
        var cliente = await _clientes.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Cliente '{cmd.Id}' was not found.");

        // Pre-check CUIT uniqueness before mutating the aggregate (ADR-CCC-1).
        if (cmd.Cuit is not null)
        {
            var conflict = await _clientes.CuitExistsForOtherAsync(cmd.Cuit, cmd.Id, ct);
            if (conflict)
                throw new ConflictException($"CUIT '{cmd.Cuit}' is already assigned to another cliente.");
        }

        var cuit  = cmd.Cuit  is null ? null : new Cuit(cmd.Cuit);
        var email = cmd.Email is null ? null : new Email(cmd.Email);

        // Domain method re-validates RI-requires-CUIT and updates mutable fields.
        // DomainException bubbles up → 422 via GastroGestionExceptionHandler.
        cliente.ActualizarDatos(cmd.Nombre, cmd.CondicionIVA, cuit, email);

        await _uow.SaveChangesAsync(ct);

        return cliente;
    }
}
