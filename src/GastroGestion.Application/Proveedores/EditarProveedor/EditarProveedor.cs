using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Proveedores;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Proveedores.EditarProveedor;

public sealed record EditarProveedorCommand(Guid Id, string Nombre, string? Cuit, string? Email, string? Telefono);

public sealed class EditarProveedorHandler
{
    private readonly IProveedorRepository _proveedores;
    private readonly IUnitOfWork          _uow;

    public EditarProveedorHandler(IProveedorRepository proveedores, IUnitOfWork uow)
    {
        _proveedores = proveedores;
        _uow         = uow;
    }

    public async Task<Proveedor> Handle(EditarProveedorCommand cmd, CancellationToken ct = default)
    {
        var proveedor = await _proveedores.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Proveedor '{cmd.Id}' was not found.");

        var cuit  = cmd.Cuit  is null ? null : new Cuit(cmd.Cuit);
        var email = cmd.Email is null ? null : new Email(cmd.Email);

        // Compare the normalized CUIT value (not the raw input) against stored rows.
        if (cuit is not null && await _proveedores.CuitExistsForOtherAsync(cuit.Valor, cmd.Id, ct))
            throw new ConflictException($"CUIT '{cmd.Cuit}' is already assigned to another proveedor.");

        proveedor.ActualizarDatos(cmd.Nombre, cuit, email, cmd.Telefono);

        await _uow.SaveChangesAsync(ct);

        return proveedor;
    }
}
