using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Proveedores.DesactivarProveedor;

public sealed record DesactivarProveedorCommand(Guid Id);

public sealed class DesactivarProveedorHandler
{
    private readonly IProveedorRepository _proveedores;
    private readonly IUnitOfWork          _uow;

    public DesactivarProveedorHandler(IProveedorRepository proveedores, IUnitOfWork uow)
    {
        _proveedores = proveedores;
        _uow         = uow;
    }

    public async Task Handle(DesactivarProveedorCommand cmd, CancellationToken ct = default)
    {
        var proveedor = await _proveedores.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Proveedor '{cmd.Id}' was not found.");

        proveedor.Desactivar();

        await _uow.SaveChangesAsync(ct);
    }
}
