using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Clientes;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Clientes.CrearCliente;

public sealed class CrearClienteHandler
{
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork        _uow;

    public CrearClienteHandler(IClienteRepository clientes, IUnitOfWork uow)
    {
        _clientes = clientes;
        _uow      = uow;
    }

    public async Task<Guid> Handle(CrearClienteCommand cmd, CancellationToken ct = default)
    {
        var cuit  = cmd.Cuit  is null ? null : new Cuit(cmd.Cuit);
        var email = cmd.Email is null ? null : new Email(cmd.Email);

        var cliente = Cliente.Crear(cmd.Nombre, cmd.CondicionIVA, cuit, email);

        await _clientes.AddAsync(cliente, ct);
        await _uow.SaveChangesAsync(ct);

        return cliente.Id;
    }
}
