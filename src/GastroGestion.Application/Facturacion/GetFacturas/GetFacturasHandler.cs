using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Facturacion;

namespace GastroGestion.Application.Facturacion.GetFacturas;

public sealed class GetFacturasHandler
{
    private readonly IFacturaRepository _facturas;

    public GetFacturasHandler(IFacturaRepository facturas) => _facturas = facturas;

    public async Task<IReadOnlyList<Factura>> Handle(GetFacturasQuery query, CancellationToken ct = default)
        => await _facturas.ListAsync(query.Estado, query.ClienteId, ct);
}
