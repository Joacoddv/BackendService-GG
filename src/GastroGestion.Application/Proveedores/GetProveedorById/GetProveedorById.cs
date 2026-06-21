using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Proveedores;

namespace GastroGestion.Application.Proveedores.GetProveedorById;

public sealed record GetProveedorByIdQuery(Guid Id);

public sealed class GetProveedorByIdHandler
{
    private readonly IProveedorRepository _proveedores;

    public GetProveedorByIdHandler(IProveedorRepository proveedores) => _proveedores = proveedores;

    public Task<Proveedor?> Handle(GetProveedorByIdQuery query, CancellationToken ct = default)
        => _proveedores.GetByIdAsync(query.Id, ct);
}
