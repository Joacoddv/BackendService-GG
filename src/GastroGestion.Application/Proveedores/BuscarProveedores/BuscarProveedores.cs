using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Proveedores;

namespace GastroGestion.Application.Proveedores.BuscarProveedores;

public sealed record BuscarProveedoresQuery(string? Nombre, bool IncluirInactivos);

public sealed class BuscarProveedoresHandler
{
    private readonly IProveedorRepository _proveedores;

    public BuscarProveedoresHandler(IProveedorRepository proveedores) => _proveedores = proveedores;

    public Task<IReadOnlyList<Proveedor>> Handle(BuscarProveedoresQuery query, CancellationToken ct = default)
        => _proveedores.SearchAsync(query.Nombre, query.IncluirInactivos, ct);
}
