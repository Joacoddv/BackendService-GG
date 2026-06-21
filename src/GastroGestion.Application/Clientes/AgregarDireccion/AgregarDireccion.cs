using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Clientes;

namespace GastroGestion.Application.Clientes.AgregarDireccion;

public sealed record AgregarDireccionCommand(
    Guid ClienteId,
    string Calle,
    string Numero,
    string Ciudad,
    string Provincia,
    string CodigoPostal,
    string? Piso,
    string? Departamento);

public sealed class AgregarDireccionHandler
{
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork        _uow;

    public AgregarDireccionHandler(IClienteRepository clientes, IUnitOfWork uow)
    {
        _clientes = clientes;
        _uow      = uow;
    }

    public async Task<Guid> Handle(AgregarDireccionCommand cmd, CancellationToken ct = default)
    {
        var cliente = await _clientes.GetByIdAsync(cmd.ClienteId, ct)
            ?? throw new NotFoundException($"Cliente '{cmd.ClienteId}' was not found.");

        var direccion = new Direccion(
            Guid.NewGuid(),
            cmd.Calle, cmd.Numero, cmd.Ciudad, cmd.Provincia, cmd.CodigoPostal,
            cmd.Piso, cmd.Departamento);

        cliente.AgregarDireccion(direccion);
        await _uow.SaveChangesAsync(ct);

        return direccion.Id;
    }
}
