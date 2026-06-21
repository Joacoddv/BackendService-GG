using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Proveedores;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Proveedores.CrearProveedor;

public sealed record CrearProveedorCommand(string Nombre, string? Cuit, string? Email, string? Telefono);

public sealed class CrearProveedorHandler
{
    private readonly IProveedorRepository _proveedores;
    private readonly IUnitOfWork          _uow;

    public CrearProveedorHandler(IProveedorRepository proveedores, IUnitOfWork uow)
    {
        _proveedores = proveedores;
        _uow         = uow;
    }

    public async Task<Guid> Handle(CrearProveedorCommand cmd, CancellationToken ct = default)
    {
        // Build the value object first: it validates + normalizes the CUIT (strips formatting), so
        // the uniqueness check compares the stored normalized value, not the raw input.
        var cuit  = cmd.Cuit  is null ? null : new Cuit(cmd.Cuit);
        var email = cmd.Email is null ? null : new Email(cmd.Email);

        if (cuit is not null && await _proveedores.CuitExistsForOtherAsync(cuit.Valor, Guid.Empty, ct))
            throw new ConflictException($"CUIT '{cmd.Cuit}' is already assigned to another proveedor.");

        var proveedor = Proveedor.Crear(cmd.Nombre, cuit, email, cmd.Telefono);

        await _proveedores.AddAsync(proveedor, ct);
        await _uow.SaveChangesAsync(ct);

        return proveedor.Id;
    }
}
